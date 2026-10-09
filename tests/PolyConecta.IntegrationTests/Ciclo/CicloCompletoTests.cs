using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Application.Plataforma.Folios;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Plataforma;
using PolyConecta.IntegrationTests.Plataforma;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Ciclo;

/// <summary>
/// SC-004 de la spec 002: un documento recorre alta con folio → transición registrada → outbox →
/// despachador → bridge simulado → callback firmado → folio e id ERP en sus columnas erp_*. Con
/// un fallo forzado termina en Error y se recupera reintentando. Necesita el bridge simulado en
/// BRIDGE_URL (en CI, el trabajo `contrato`); sin él se omite.
/// </summary>
public class CicloCompletoTests(SqlServerFixture sql)
{
    private static readonly string? BridgeUrl = Environment.GetEnvironmentVariable("BRIDGE_URL");
    private static readonly string Secreto = Environment.GetEnvironmentVariable("BRIDGE_CALLBACK_SECRET") ?? "secreto-de-pruebas";

    public static bool HayBridge => !string.IsNullOrWhiteSpace(BridgeUrl);

    private sealed record Sistema(Entorno Entorno, ApiDePrueba Api) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Api.DisposeAsync();
    }

    private async Task<Sistema> LevantarAsync(string prefijo = "PRB-{yyyy}-")
    {
        var entorno = await Entorno.CrearAsync(sql);
        await using (var db = entorno.Contexto())
        {
            db.ReferenceSequences.Add(new ReferenceSequence("PRUEBA", prefijo, 4, ResetRule.Nunca));
            await db.SaveChangesAsync();
            // La idempotency_key lleva el id del documento (CT-19) y cada base nueva arranca en 1. El
            // simulador conserva sus transacciones entre corridas: con el mismo id, el comando llegaría
            // como reenvío y devolvería el resultado anterior. Un id inicial al azar evita el choque.
            var inicio = Random.Shared.NextInt64(1_000_000, 1_000_000_000_000);
            // DBCC no admite parámetros en todas sus formas; el valor es un número generado aquí, no una entrada.
            var resiembra = string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"DBCC CHECKIDENT ('prueba.documento_de_prueba', RESEED, {inicio}) WITH NO_INFOMSGS;");
            await db.Database.ExecuteSqlRawAsync(resiembra);
        }
        // El reloj del documento es real: el bridge firma el callback con su propia hora.
        entorno.Reloj.Now = DateTimeOffset.UtcNow;

        var puerto = PuertoLibre();
        var api = new ApiDePrueba(entorno, new Dictionary<string, string?>
        {
            ["Erp:BridgeUrl"] = BridgeUrl,
            ["Erp:CallbackBaseUrl"] = $"http://{Environment.GetEnvironmentVariable("CALLBACK_HOST") ?? "localhost"}:{puerto}",
            ["Erp:CallbackSecret"] = Secreto,
            ["Erp:IntervaloMs"] = "200",
            ["Erp:CallbackTimeoutSegundos"] = "120",
        });
        api.UseKestrel(puerto);
        api.StartServer();
        return new Sistema(entorno, api);
    }

    private static async Task<(long Id, string Folio)> CrearDocumentoAsync(Sistema s)
    {
        await using var scope = s.Api.Services.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await uow.BeginTransactionAsync();
        var folio = await scope.ServiceProvider.GetRequiredService<IReferenceSequenceService>().NextAsync("PRUEBA");
        var doc = new DocumentoDePrueba(folio, "PEBD-001", 10, "KG", "MP-PIM", "WIP-PIM");
        scope.ServiceProvider.GetRequiredService<PruebasDbContext>().Documentos.Add(doc);
        doc.Confirmar();
        await scope.ServiceProvider.GetRequiredService<IBridgeSyncService>().EncolarAsync(doc, "confirmar");
        await uow.SaveChangesAsync();
        await uow.CommitAsync();
        return (doc.Id, folio);
    }

    private static async Task<DocumentoDePrueba> EsperarAsync(Sistema s, long id, SyncStatus estado)
    {
        var limite = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            await using var db = s.Entorno.Contexto();
            var doc = await db.Documentos.SingleAsync(d => d.Id == id);
            if (doc.Sync.Status == estado || DateTime.UtcNow > limite) return doc;
            await Task.Delay(200);
        }
    }

    [Fact(SkipUnless = nameof(HayBridge), Skip = "Ciclo completo: define BRIDGE_URL con el bridge simulado.")]
    public async Task Un_documento_recorre_el_ciclo_hasta_el_callback()
    {
        await using var s = await LevantarAsync();

        var (id, folio) = await CrearDocumentoAsync(s);
        folio.Should().MatchRegex(@"^PRB-\d{4}-0001$");

        var doc = await EsperarAsync(s, id, SyncStatus.Confirmado);
        doc.Sync.Status.Should().Be(SyncStatus.Confirmado);
        doc.Sync.ErpFolio.Should().NotBeNullOrEmpty();
        doc.Sync.ErpId.Should().NotBeNullOrEmpty();
        doc.Sync.ErpDocuments.Should().Contain("\"rol\":\"salida\"").And.Contain("\"rol\":\"entrada\"");

        await using var db = s.Entorno.Contexto();
        var mensaje = await db.OutboxMessages.SingleAsync();
        mensaje.Status.Should().Be(OutboxStatus.Confirmado);
        mensaje.BridgeTransactionId.Should().NotBeNull();
        (await db.StateTransitionLogs.Where(l => l.EntityId == id).Select(l => l.ToState).ToListAsync())
            .Should().Equal("Confirmado");
    }

    [Fact(SkipUnless = nameof(HayBridge), Skip = "Ciclo completo: define BRIDGE_URL con el bridge simulado.")]
    public async Task Con_un_fallo_forzado_termina_en_Error_y_se_recupera_reintentando()
    {
        // Folio único por corrida, para que la regla de fallo no toque otras pruebas.
        var prefijo = $"F{Guid.NewGuid():N}"[..10] + "-";
        var folioEsperado = prefijo + "0001";
        await using var s = await LevantarAsync(prefijo);
        using var bridge = new HttpClient { BaseAddress = new Uri(BridgeUrl!) };
        (await bridge.PutAsJsonAsync("/admin/simulated/faults", new[]
        {
            new { command_type = "TRASPASO", referencia_negocio = folioEsperado, error_code = "EXISTENCIA_INSUFICIENTE", veces = 1 },
        })).EnsureSuccessStatusCode();

        try
        {
            var (id, _) = await CrearDocumentoAsync(s);
            var doc = await EsperarAsync(s, id, SyncStatus.Error);
            doc.Sync.Status.Should().Be(SyncStatus.Error);
            doc.Sync.LastErrorCode.Should().Be("EXISTENCIA_INSUFICIENTE");

            Guid mensajeId;
            await using (var db = s.Entorno.Contexto())
                mensajeId = (await db.OutboxMessages.SingleAsync()).Id;
            // El reintento es una ruta con sesión (FR-009); el callback del bridge no.
            (await (await s.Api.ClienteAsync()).PostAsync($"/api/v1/plataforma/outbox/{mensajeId}/reintentar", null))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);

            doc = await EsperarAsync(s, id, SyncStatus.Confirmado);
            doc.Sync.Status.Should().Be(SyncStatus.Confirmado);
            doc.Sync.ErpFolio.Should().NotBeNullOrEmpty();
        }
        finally
        {
            await bridge.PutAsJsonAsync("/admin/simulated/faults", Array.Empty<object>());
        }
    }

    private static int PuertoLibre()
    {
        using var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        return ((IPEndPoint)l.LocalEndpoint).Port;
    }
}
