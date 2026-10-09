using System.Net;
using System.Text;
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

namespace PolyConecta.IntegrationTests.Plataforma;

/// <summary>
/// Endpoint del callback (FR-020; contracts/callback-api.md de la spec 002): firma, idempotencia y
/// efecto en el documento.
/// </summary>
public class BridgeCallbackTests(SqlServerFixture sql)
{
    private const string Ruta = "/api/v1/plataforma/bridge/callbacks";

    private static async Task<(Entorno Entorno, ApiDePrueba Api, long Id, string Llave)> PrepararAsync(SqlServerFixture sql)
    {
        var entorno = await Entorno.CrearAsync(sql);
        var api = new ApiDePrueba(entorno);
        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PruebasDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await uow.BeginTransactionAsync();
        var doc = new DocumentoDePrueba("PRB-1", "PEBD-001", 10, "KG");
        db.Documentos.Add(doc);
        doc.Confirmar();
        await scope.ServiceProvider.GetRequiredService<IBridgeSyncService>().EncolarAsync(doc, "confirmar");
        await uow.SaveChangesAsync();
        await uow.CommitAsync();
        return (entorno, api, doc.Id, $"DocumentoDePrueba:{doc.Id}:confirmar");
    }

    private static async Task<HttpResponseMessage> EnviarAsync(ApiDePrueba api, Entorno entorno, JsonObject cuerpo,
        string secreto = ApiDePrueba.Secreto, DateTimeOffset? cuando = null)
    {
        var texto = cuerpo.ToJsonString();
        using var peticion = new HttpRequestMessage(HttpMethod.Post, Ruta) { Content = new StringContent(texto, Encoding.UTF8, "application/json") };
        peticion.Headers.Add(BridgeSignature.Cabecera, BridgeSignature.Firmar(texto, secreto, cuando ?? entorno.Reloj.Now));
        return await api.CreateClient().SendAsync(peticion);
    }

    private static JsonObject Callback(string llave, string status, string? folio = "1042", string? codigo = null) => new()
    {
        ["event_type"] = "transaction.status_changed",
        ["contract_version"] = "1.0",
        ["transaction_id"] = Guid.NewGuid().ToString(),
        ["correlation_id"] = "x",
        ["idempotency_key"] = llave,
        ["command_type"] = "TRASPASO",
        ["status"] = status,
        ["result"] = status == "CONFIRMED" ? new JsonObject { ["folio"] = folio, ["id_erp"] = 55120 } : null,
        ["error"] = codigo is null ? null : new JsonObject { ["code"] = codigo, ["retryable"] = false, ["message"] = "fallo" },
        ["timestamp"] = "2026-10-12T00:00:00Z",
    };

    private static async Task<DocumentoDePrueba> DocumentoAsync(Entorno entorno, long id)
    {
        await using var db = entorno.Contexto();
        return await db.Documentos.SingleAsync(d => d.Id == id);
    }

    [Fact]
    public async Task Con_firma_valida_confirma_el_documento_y_guarda_folio_e_id_ERP()
    {
        var (entorno, api, id, llave) = await PrepararAsync(sql);
        await using var _ = api;

        (await EnviarAsync(api, entorno, Callback(llave, "CONFIRMED"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await DocumentoAsync(entorno, id);
        doc.Sync.Status.Should().Be(SyncStatus.Confirmado);
        doc.Sync.ErpFolio.Should().Be("1042");
        doc.Sync.ErpId.Should().Be("55120");
    }

    [Fact]
    public async Task Una_firma_invalida_o_vencida_se_rechaza_con_401_sin_cambios()
    {
        var (entorno, api, id, llave) = await PrepararAsync(sql);
        await using var _ = api;

        (await EnviarAsync(api, entorno, Callback(llave, "CONFIRMED"), secreto: "otro")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await EnviarAsync(api, entorno, Callback(llave, "CONFIRMED"), cuando: entorno.Reloj.Now.AddMinutes(-6)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var sinFirma = new HttpRequestMessage(HttpMethod.Post, Ruta) { Content = new StringContent(Callback(llave, "CONFIRMED").ToJsonString(), Encoding.UTF8, "application/json") };
        (await api.CreateClient().SendAsync(sinFirma)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await DocumentoAsync(entorno, id)).Sync.Status.Should().Be(SyncStatus.Pendiente);
    }

    [Fact]
    public async Task Un_callback_repetido_no_cambia_un_documento_confirmado()
    {
        var (entorno, api, id, llave) = await PrepararAsync(sql);
        await using var _ = api;
        await EnviarAsync(api, entorno, Callback(llave, "CONFIRMED", "1042"));

        (await EnviarAsync(api, entorno, Callback(llave, "CONFIRMED", "9999"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await EnviarAsync(api, entorno, Callback(llave, "FAILED", null, "SDK_ERROR"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await DocumentoAsync(entorno, id);
        doc.Sync.Status.Should().Be(SyncStatus.Confirmado);
        doc.Sync.ErpFolio.Should().Be("1042");
    }

    [Fact]
    public async Task Un_FAILED_deja_el_documento_y_el_mensaje_en_Error()
    {
        var (entorno, api, id, llave) = await PrepararAsync(sql);
        await using var _ = api;

        (await EnviarAsync(api, entorno, Callback(llave, "FAILED", null, "EXISTENCIA_INSUFICIENTE"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await DocumentoAsync(entorno, id);
        doc.Sync.Status.Should().Be(SyncStatus.Error);
        doc.Sync.LastErrorCode.Should().Be("EXISTENCIA_INSUFICIENTE");
        await using var db = entorno.Contexto();
        (await db.OutboxMessages.SingleAsync()).Status.Should().Be(OutboxStatus.Error);
    }

    [Fact]
    public async Task Una_llave_desconocida_da_404_y_un_cuerpo_invalido_400()
    {
        var (entorno, api, _, _) = await PrepararAsync(sql);
        await using var __ = api;

        (await EnviarAsync(api, entorno, Callback("Otro:1:x", "CONFIRMED"))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await EnviarAsync(api, entorno, new JsonObject { ["hola"] = 1 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reintentar_un_comando_que_no_esta_en_Error_responde_400()
    {
        var (entorno, api, _, _) = await PrepararAsync(sql);
        await using var _ = api;
        await using var db = entorno.Contexto();
        var mensaje = await db.OutboxMessages.SingleAsync();

        // El reintento es una ruta con sesión (FR-009); el callback no (se autentica con su firma).
        var cliente = await api.ClienteAsync();
        var r = await cliente.PostAsync($"/api/v1/plataforma/outbox/{mensaje.Id}/reintentar", null);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
