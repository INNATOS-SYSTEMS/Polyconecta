using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Plataforma;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.Application.Tests.Erp;

/// <summary>Caso de uso de prueba: crea el documento, lo confirma y encola su comando.</summary>
public sealed record CrearYConfirmar(string Folio, string Producto, decimal Cantidad, bool FallarAlFinal = false);

public sealed class CrearYConfirmarDocumento(PruebasDbContext db, IBridgeSyncService bridge) : IUseCase<CrearYConfirmar, long>
{
    public async Task<long> ExecuteAsync(CrearYConfirmar request, CancellationToken cancellationToken = default)
    {
        var doc = new DocumentoDePrueba(request.Folio, request.Producto, request.Cantidad, "KG");
        db.Documentos.Add(doc);
        doc.Confirmar();
        await bridge.EncolarAsync(doc, "confirmar", cancellationToken);
        if (request.FallarAlFinal) throw new InvalidOperationException("Falla de negocio después de encolar.");
        return doc.Id;
    }
}

/// <summary>US-4, escenario 3: el outbox se escribe en la misma transacción que el negocio (CT-20).</summary>
public class EncolarTests(SqlServerFixture sql)
{
    [Fact]
    public async Task Encola_el_comando_con_su_llave_su_carga_y_sus_llaves_de_bloqueo()
    {
        var entorno = await Entorno.CrearAsync(sql);
        await using var sp = ConCasoDeUso(entorno);
        await using var scope = sp.CreateAsyncScope();
        var ct = TestContext.Current.CancellationToken;

        var id = await scope.ServiceProvider.GetRequiredService<IUseCase<CrearYConfirmar, long>>()
            .ExecuteAsync(new CrearYConfirmar("PRB-1", "PEBD-001", 100), ct);

        await using var db = entorno.Contexto();
        var mensaje = await db.OutboxMessages.SingleAsync(ct);
        mensaje.IdempotencyKey.Should().Be($"DocumentoDePrueba:{id}:confirmar");
        mensaje.CommandType.Should().Be("TRASPASO");
        mensaje.Variant.Should().Be("RECOLECCION");
        mensaje.CorrelationId.Should().Be("corr-prueba");
        mensaje.LockKeys.Should().BeEquivalentTo("producto:PEBD-001", "almacen:MP-PIM", "almacen:WIP-PIM");
        mensaje.Status.Should().Be(OutboxStatus.Pendiente);
        using var carga = JsonDocument.Parse(mensaje.Payload);
        carga.RootElement.GetProperty("referencia_negocio").GetString().Should().Be("PRB-1");

        var doc = await db.Documentos.SingleAsync(ct);
        doc.Sync.Status.Should().Be(SyncStatus.Pendiente);
        doc.State.Should().Be(EstadoPrueba.Confirmado);
    }

    [Fact]
    public async Task Si_el_negocio_falla_no_queda_ni_el_documento_ni_el_mensaje()
    {
        var entorno = await Entorno.CrearAsync(sql);
        await using var sp = ConCasoDeUso(entorno);
        await using var scope = sp.CreateAsyncScope();
        var ct = TestContext.Current.CancellationToken;

        var accion = () => scope.ServiceProvider.GetRequiredService<IUseCase<CrearYConfirmar, long>>()
            .ExecuteAsync(new CrearYConfirmar("PRB-2", "PEBD-001", 100, FallarAlFinal: true), ct);
        await accion.Should().ThrowAsync<InvalidOperationException>();

        await using var db = entorno.Contexto();
        (await db.OutboxMessages.CountAsync(ct)).Should().Be(0);
        (await db.Documentos.IgnoreQueryFilters().CountAsync(ct)).Should().Be(0);
        (await db.StateTransitionLogs.CountAsync(ct)).Should().Be(0);
    }

    internal static ServiceProvider ConCasoDeUso(Entorno entorno, HttpMessageHandler? bridge = null) =>
        ServiciosDePrueba.Crear(entorno, bridge, configurar: s => s.AddUseCase<CrearYConfirmar, long, CrearYConfirmarDocumento>());
}
