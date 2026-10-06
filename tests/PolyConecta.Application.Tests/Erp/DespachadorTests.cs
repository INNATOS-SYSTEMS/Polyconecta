using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Plataforma;
using PolyConecta.Infrastructure.Erp;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.Application.Tests.Erp;

/// <summary>US-4, escenarios 4 y 5 de la spec 002; research R-04.</summary>
public class DespachadorTests(SqlServerFixture sql)
{
    private sealed class Escenario : IAsyncDisposable
    {
        public required Entorno Entorno { get; init; }
        public required ServiceProvider Servicios { get; init; }
        public required BridgeFalso Bridge { get; init; }

        public static async Task<Escenario> CrearAsync(SqlServerFixture sql, Action<ErpOptions>? erp = null)
        {
            var entorno = await Entorno.CrearAsync(sql);
            var bridge = new BridgeFalso();
            return new Escenario { Entorno = entorno, Bridge = bridge, Servicios = ServiciosDePrueba.Crear(entorno, bridge, erp) };
        }

        public async Task<long> DocumentoAsync(string folio, string producto, string origen = "MP-PIM", string destino = "WIP-PIM")
        {
            await using var scope = Servicios.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PruebasDbContext>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await uow.BeginTransactionAsync();
            var doc = new DocumentoDePrueba(folio, producto, 10, "KG", origen, destino);
            db.Documentos.Add(doc);
            doc.Confirmar();
            await scope.ServiceProvider.GetRequiredService<IBridgeSyncService>().EncolarAsync(doc, "confirmar");
            await uow.SaveChangesAsync();
            await uow.CommitAsync();
            return doc.Id;
        }

        public async Task CicloAsync()
        {
            await using var scope = Servicios.CreateAsyncScope();
            await Servicios.GetRequiredService<BridgeDispatcher>().CicloAsync(scope.ServiceProvider, CancellationToken.None);
        }

        public async Task<EfectoCallback> CallbackAsync(JsonObject cuerpo)
        {
            await using var scope = Servicios.CreateAsyncScope();
            using var json = System.Text.Json.JsonDocument.Parse(cuerpo.ToJsonString());
            return await scope.ServiceProvider.GetRequiredService<IUseCase<ResultadoBridge, EfectoCallback>>()
                .ExecuteAsync(ConfirmarSincronizacion.Leer(json.RootElement));
        }

        public async Task ReintentarAsync(Guid outboxId)
        {
            await using var scope = Servicios.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IUseCase<ReintentarSincronizacionRequest, Unit>>()
                .ExecuteAsync(new ReintentarSincronizacionRequest(outboxId));
        }

        public async Task<List<OutboxMessage>> MensajesAsync()
        {
            await using var db = Entorno.Contexto();
            return await db.OutboxMessages.OrderBy(m => m.Sequence).ToListAsync();
        }

        public async Task<DocumentoDePrueba> DocumentoPorIdAsync(long id)
        {
            await using var db = Entorno.Contexto();
            return await db.Documentos.SingleAsync(d => d.Id == id);
        }

        public ValueTask DisposeAsync() => Servicios.DisposeAsync();
    }

    [Fact]
    public async Task Envia_en_orden_de_registro_y_uno_a_la_vez_entre_comandos_que_comparten_llave()
    {
        await using var e = await Escenario.CrearAsync(sql);
        var a = await e.DocumentoAsync("PRB-A", "PEBD-001");
        var b = await e.DocumentoAsync("PRB-B", "PEBD-001");
        var c = await e.DocumentoAsync("PRB-C", "PEBD-001");

        await e.CicloAsync();
        e.Bridge.Llaves.Should().Equal($"DocumentoDePrueba:{a}:confirmar");

        await e.CicloAsync();
        e.Bridge.Llaves.Should().HaveCount(1, "B espera a que A termine porque comparten producto y almacén");

        await e.CallbackAsync(BridgeFalso.Confirmado($"DocumentoDePrueba:{a}:confirmar"));
        await e.CicloAsync();
        await e.CallbackAsync(BridgeFalso.Confirmado($"DocumentoDePrueba:{b}:confirmar"));
        await e.CicloAsync();

        e.Bridge.Llaves.Should().Equal(
            $"DocumentoDePrueba:{a}:confirmar", $"DocumentoDePrueba:{b}:confirmar", $"DocumentoDePrueba:{c}:confirmar");
        e.Bridge.Recibidos[0]["correlation_id"]!.GetValue<string>().Should().Be("corr-prueba");
        e.Bridge.Recibidos[0]["callback_url"]!.GetValue<string>().Should().EndWith("/api/v1/plataforma/bridge/callbacks");
    }

    [Fact]
    public async Task Comandos_sin_llaves_en_comun_no_se_esperan()
    {
        await using var e = await Escenario.CrearAsync(sql);
        await e.DocumentoAsync("PRB-A", "PEBD-001", "MP-PIM", "WIP-PIM");
        await e.DocumentoAsync("PRB-B", "R-IV310", "PT-SC", "TRANS-PIM-SC");

        await e.CicloAsync();

        e.Bridge.Llaves.Should().HaveCount(2);
    }

    [Fact]
    public async Task El_callback_guarda_folio_e_id_ERP_y_confirma_el_documento()
    {
        await using var e = await Escenario.CrearAsync(sql);
        var id = await e.DocumentoAsync("PRB-A", "PEBD-001");
        await e.CicloAsync();
        (await e.DocumentoPorIdAsync(id)).Sync.Status.Should().Be(SyncStatus.Enviado);

        (await e.CallbackAsync(BridgeFalso.Confirmado($"DocumentoDePrueba:{id}:confirmar", "1042", 55120))).Should().Be(EfectoCallback.Aplicado);

        var doc = await e.DocumentoPorIdAsync(id);
        doc.Sync.Status.Should().Be(SyncStatus.Confirmado);
        doc.Sync.ErpFolio.Should().Be("1042");
        doc.Sync.ErpId.Should().Be("55120");
        doc.Sync.ErpDocuments.Should().Contain("\"rol\":\"entrada\"");
        (await e.MensajesAsync()).Single().Status.Should().Be(OutboxStatus.Confirmado);

        (await e.CallbackAsync(BridgeFalso.Confirmado($"DocumentoDePrueba:{id}:confirmar", "OTRO"))).Should().Be(EfectoCallback.Ignorado);
        (await e.DocumentoPorIdAsync(id)).Sync.ErpFolio.Should().Be("1042");
    }

    [Fact]
    public async Task Un_error_bloquea_solo_a_los_posteriores_que_comparten_llave_y_el_reintento_los_libera()
    {
        await using var e = await Escenario.CrearAsync(sql);
        var a = await e.DocumentoAsync("PRB-A", "PEBD-001", "MP-PIM", "WIP-PIM");
        var b = await e.DocumentoAsync("PRB-B", "PEBD-001", "MP-PIM", "WIP-PIM");
        var c = await e.DocumentoAsync("PRB-C", "R-IV310", "PT-SC", "TRANS-PIM-SC");
        await e.CicloAsync();
        await e.CallbackAsync(BridgeFalso.Fallido($"DocumentoDePrueba:{a}:confirmar", "EXISTENCIA_INSUFICIENTE"));

        await e.CicloAsync();

        var mensajes = await e.MensajesAsync();
        mensajes.Select(m => m.Status).Should().Equal(OutboxStatus.Error, OutboxStatus.Bloqueado, OutboxStatus.Enviado);
        (await e.DocumentoPorIdAsync(a)).Sync.LastErrorCode.Should().Be("EXISTENCIA_INSUFICIENTE");

        await e.ReintentarAsync(mensajes[0].Id);
        await e.CicloAsync();

        mensajes = await e.MensajesAsync();
        mensajes[0].Status.Should().Be(OutboxStatus.Enviado, "se reenvía con la misma llave");
        mensajes[1].Status.Should().Be(OutboxStatus.Pendiente, "ya no está bloqueado; espera a que A termine");
        e.Bridge.Llaves.Count(l => l == $"DocumentoDePrueba:{a}:confirmar").Should().Be(2);
        (await e.DocumentoPorIdAsync(a)).Sync.Status.Should().Be(SyncStatus.Enviado);
    }

    [Fact]
    public async Task Las_fallas_de_red_se_reintentan_con_espera_y_al_agotarse_dejan_Error()
    {
        await using var e = await Escenario.CrearAsync(sql, o => o.MaxIntentos = 2);
        e.Bridge.AlEnviar = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        var id = await e.DocumentoAsync("PRB-A", "PEBD-001");

        await e.CicloAsync();
        var m = (await e.MensajesAsync()).Single();
        m.Status.Should().Be(OutboxStatus.Pendiente);
        m.NextAttemptAt.Should().Be(e.Entorno.Reloj.Now.AddSeconds(2));

        await e.CicloAsync();
        e.Bridge.Recibidos.Should().HaveCount(1, "no se reintenta antes de la espera");

        e.Entorno.Reloj.Now = e.Entorno.Reloj.Now.AddSeconds(3);
        await e.CicloAsync();

        (await e.MensajesAsync()).Single().Status.Should().Be(OutboxStatus.Error);
        var doc = await e.DocumentoPorIdAsync(id);
        doc.Sync.Status.Should().Be(SyncStatus.Error);
        doc.Sync.LastErrorCode.Should().Be("ENVIO_FALLIDO");
    }

    [Fact]
    public async Task Un_rechazo_del_contrato_deja_Error_sin_reintentar()
    {
        await using var e = await Escenario.CrearAsync(sql);
        e.Bridge.AlEnviar = _ => BridgeFalso.Json(HttpStatusCode.BadRequest,
            new JsonObject { ["code"] = "CARGA_INVALIDA", ["retryable"] = false, ["message"] = "fecha: Es obligatorio." });
        var id = await e.DocumentoAsync("PRB-A", "PEBD-001");

        await e.CicloAsync();
        await e.CicloAsync();

        e.Bridge.Recibidos.Should().HaveCount(1);
        (await e.DocumentoPorIdAsync(id)).Sync.LastErrorCode.Should().Be("CARGA_INVALIDA");
    }

    [Fact]
    public async Task Si_el_callback_no_llega_a_tiempo_consulta_la_transaccion()
    {
        await using var e = await Escenario.CrearAsync(sql, o => o.CallbackTimeoutSegundos = 60);
        var id = await e.DocumentoAsync("PRB-A", "PEBD-001");
        e.Bridge.AlEnviar = _ => BridgeFalso.Aceptar("tx-1");
        e.Bridge.AlConsultar = tx => tx == "tx-1"
            ? BridgeFalso.Json(HttpStatusCode.OK, BridgeFalso.Confirmado($"DocumentoDePrueba:{id}:confirmar", "2001"))
            : new HttpResponseMessage(HttpStatusCode.NotFound);
        await e.CicloAsync();

        await e.CicloAsync();
        (await e.DocumentoPorIdAsync(id)).Sync.Status.Should().Be(SyncStatus.Enviado, "todavía no vence el plazo");

        e.Entorno.Reloj.Now = e.Entorno.Reloj.Now.AddSeconds(61);
        await e.CicloAsync();

        var doc = await e.DocumentoPorIdAsync(id);
        doc.Sync.Status.Should().Be(SyncStatus.Confirmado);
        doc.Sync.ErpFolio.Should().Be("2001");
    }

    [Fact]
    public async Task Solo_envia_los_comandos_habilitados()
    {
        await using var e = await Escenario.CrearAsync(sql, o => o.ComandosHabilitados = ["ALTA_PEDIDO"]);
        await e.DocumentoAsync("PRB-A", "PEBD-001");

        await e.CicloAsync();

        e.Bridge.Recibidos.Should().BeEmpty();
        (await e.MensajesAsync()).Single().Status.Should().Be(OutboxStatus.Pendiente);
    }
}
