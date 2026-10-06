using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Application.Plataforma.Folios;
using PolyConecta.Tests.Compartido;
using PolyConecta.Domain.Plataforma;
using PolyConecta.Infrastructure.Plataforma;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.Application.Tests.Plataforma;

/// <summary>US-4, escenario 6 de la spec 002.</summary>
public class FoliosTests(SqlServerFixture sql)
{
    private static async Task<Entorno> ConSecuenciasAsync(SqlServerFixture sql)
    {
        var entorno = await Entorno.CrearAsync(sql);
        await using var db = entorno.Contexto();
        db.ReferenceSequences.AddRange(
            new ReferenceSequence("PEDIDO", "IV", 4, ResetRule.Anual),
            new ReferenceSequence("OF_BOLSEO", "BOL-{yyyy}-", 4, ResetRule.Nunca));
        await db.SaveChangesAsync();
        return entorno;
    }

    [Fact]
    public async Task Cincuenta_peticiones_en_paralelo_dan_cincuenta_folios_distintos_y_consecutivos()
    {
        var entorno = await ConSecuenciasAsync(sql);

        var folios = await Task.WhenAll(Enumerable.Range(0, 50).Select(async _ =>
        {
            await using var db = entorno.Contexto();
            await using var tx = await db.Database.BeginTransactionAsync();
            var folio = await new ReferenceSequenceService(db, entorno.Reloj).NextAsync("PEDIDO");
            await tx.CommitAsync();
            return folio;
        }));

        folios.Should().OnlyHaveUniqueItems();
        folios.Order().Should().Equal(Enumerable.Range(1, 50).Select(n => $"IV{n:D4}"));
    }

    [Fact]
    public async Task Cada_tipo_usa_su_propia_secuencia()
    {
        var entorno = await ConSecuenciasAsync(sql);
        await using var db = entorno.Contexto();
        var servicio = new ReferenceSequenceService(db, entorno.Reloj);
        var ct = TestContext.Current.CancellationToken;

        (await servicio.NextAsync("PEDIDO", cancellationToken: ct)).Should().Be("IV0001");
        (await servicio.NextAsync("OF_BOLSEO", cancellationToken: ct)).Should().Be("BOL-2026-0001");
        (await servicio.NextAsync("PEDIDO", cancellationToken: ct)).Should().Be("IV0002");
    }

    [Fact]
    public async Task Un_folio_de_una_transaccion_revertida_se_vuelve_a_entregar()
    {
        var entorno = await ConSecuenciasAsync(sql);
        var ct = TestContext.Current.CancellationToken;
        await using (var db = entorno.Contexto())
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            (await new ReferenceSequenceService(db, entorno.Reloj).NextAsync("PEDIDO", cancellationToken: ct)).Should().Be("IV0001");
            await tx.RollbackAsync(ct);
        }

        await using (var db = entorno.Contexto())
            (await new ReferenceSequenceService(db, entorno.Reloj).NextAsync("PEDIDO", cancellationToken: ct)).Should().Be("IV0001");
    }

    [Fact]
    public async Task Un_tipo_sin_secuencia_es_un_error()
    {
        var entorno = await ConSecuenciasAsync(sql);
        await using var db = entorno.Contexto();
        var accion = () => new ReferenceSequenceService(db, entorno.Reloj).NextAsync("NO_EXISTE", cancellationToken: TestContext.Current.CancellationToken);
        await accion.Should().ThrowAsync<SecuenciaNoConfiguradaException>();
    }

    [Fact]
    public async Task La_migracion_trae_la_secuencia_de_rollos_de_extrusion()
    {
        var entorno = await Entorno.CrearAsync(sql);
        await using var db = entorno.Contexto();
        var folio = await new ReferenceSequenceService(db, entorno.Reloj)
            .NextAsync("ROLLO_EXTRUSION", new Dictionary<string, string> { ["linea"] = "01" }, TestContext.Current.CancellationToken);
        folio.Should().Be("EX-01-26000001");
    }
}
