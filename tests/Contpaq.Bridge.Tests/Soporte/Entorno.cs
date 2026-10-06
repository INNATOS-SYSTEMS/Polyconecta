using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contpaq.Bridge.Core.Configuration;
using Contpaq.Bridge.Core.Contract;
using Contpaq.Bridge.Core.Models;
using Contpaq.Bridge.Core.Validation;
using Contpaq.Bridge.Infrastructure.Outbox;
using Contpaq.Bridge.Infrastructure.Persistence;
using Contpaq.Bridge.Infrastructure.Sdk;
using Contpaq.Bridge.Infrastructure.Webhooks;
using Contpaq.Bridge.Simulated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Contpaq.Bridge.Tests.Soporte
{
    /// <summary>Callbacks capturados en memoria, sin HTTP.</summary>
    public sealed class CallbacksCapturados : IWebhookDispatcher
    {
        public List<BridgeTransaction> Enviados { get; } = new();

        public Task DeliverAsync(BridgeTransaction transaction, CancellationToken cancellationToken = default)
        {
            lock (Enviados) Enviados.Add(transaction);
            return Task.CompletedTask;
        }
    }

    /// <summary>Bridge en modo simulado sin servidor: outbox, validador, gateway y worker reales.</summary>
    public sealed class Entorno
    {
        public static string Raiz { get; } = BuscarRaiz();

        public string Conexion { get; } = $"Data Source={Path.Combine(Path.GetTempPath(), $"bridge_{Guid.NewGuid():N}.db")}";
        public IConfiguration Config { get; }
        public OutboxRepository Outbox { get; }
        public SimulatedStore Store { get; }
        public SimulatedReadRepository Lecturas { get; }
        public FaultStore Fallos { get; }
        public ValidadorComandos Validador { get; }
        public CallbacksCapturados Callbacks { get; } = new();
        public OutboxWorker Worker { get; }

        public Entorno(int maxRetries = 3)
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Raiz, "PolyConecta.Contpaq", "appsettings.json"))
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["BridgeConfig:Mode"] = "Simulated",
                    ["BridgeConfig:MaxRetries"] = maxRetries.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["BridgeConfig:Simulated:DelayMs"] = "0",
                })
                .Build();
            new DbInitializer(Conexion).Initialize();
            Outbox = new OutboxRepository(Conexion);
            Store = new SimulatedStore(Conexion);
            Store.Inicializar();
            var catalogo = SimulatedCatalog.Cargar(Path.Combine(Raiz, "PolyConecta.Contpaq", "Simulated", "seed.json"));
            Lecturas = new SimulatedReadRepository(catalogo, Store);
            var conceptos = new ConfiguracionConceptos(Config);
            Fallos = new FaultStore(Config);
            Validador = new ValidadorComandos(Lecturas, conceptos);
            var gateway = new SimulatedSdkGateway(Store, conceptos, Fallos, Lecturas, Config);
            Worker = new OutboxWorker(Outbox, gateway, Validador, Callbacks, BridgeOptions.Leer(Config),
                new CircuitBreakerPolicy(100, 1), NullLogger<OutboxWorker>.Instance);
        }

        public static string Ejemplo(string nombre) => File.ReadAllText(Path.Combine(Raiz, "docs", "contratos", "ejemplos", nombre));

        public static ComandoRequest Comando(string nombre) =>
            System.Text.Json.JsonSerializer.Deserialize<ComandoRequest>(Ejemplo(nombre))!;

        /// <summary>Encola el ejemplo como lo haría TransactionsController y devuelve su id.</summary>
        public async Task<string> EncolarAsync(string ejemplo, string? idempotencyKey = null, Func<string, string>? cambiarCarga = null)
        {
            var r = Comando(ejemplo);
            if (cambiarCarga is not null)
                r.Payload = System.Text.Json.JsonDocument.Parse(cambiarCarga(r.Payload.GetRawText())).RootElement;
            var ahora = DateTime.UtcNow.ToString("o");
            var tx = new BridgeTransaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                CorrelationId = r.CorrelationId!,
                ClientAppId = r.ClientAppId!,
                IdempotencyKey = idempotencyKey ?? r.IdempotencyKey,
                CommandType = r.CommandType!,
                ContractVersion = r.ContractVersion!,
                Variant = r.Variant,
                PayloadJson = r.Payload.GetRawText(),
                CallbackUrl = r.CallbackUrl,
                NextAttemptAt = ahora,
                CreatedAt = ahora,
                UpdatedAt = ahora,
            };
            await Outbox.AddTransactionAsync(tx);
            return tx.TransactionId;
        }

        /// <summary>Procesa lo pendiente como el ciclo del worker, sin esperar el reintento programado.</summary>
        public async Task<BridgeTransaction> ProcesarAsync(string transactionId)
        {
            var tx = (await Outbox.GetByIdAsync(transactionId))!;
            Worker.Procesar(tx);
            await Task.Delay(50);
            return (await Outbox.GetByIdAsync(transactionId))!;
        }

        private static string BuscarRaiz()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !dir.GetFiles("Polyconecta.slnx").Any()) dir = dir.Parent;
            return dir?.FullName ?? throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
        }
    }
}
