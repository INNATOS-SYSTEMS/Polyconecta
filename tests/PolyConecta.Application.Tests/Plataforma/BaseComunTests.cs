using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Tests.Compartido;
using PolyConecta.Domain.Common;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.Application.Tests.Plataforma;

/// <summary>US-4, escenarios 1 y 2 de la spec 002.</summary>
public class BaseComunTests(SqlServerFixture sql)
{
    [Fact]
    public async Task Alta_y_modificacion_registran_quien_y_cuando()
    {
        var entorno = await Entorno.CrearAsync(sql);
        var ct = TestContext.Current.CancellationToken;
        long id;
        await using (var db = entorno.Contexto())
        {
            var doc = new DocumentoDePrueba("PRB-0001", "MP-001", 10, "KG");
            db.Documentos.Add(doc);
            await db.SaveChangesAsync(ct);
            id = doc.Id;
        }

        entorno.Reloj.Now = entorno.Reloj.Now.AddHours(1);
        await using (var db = entorno.Contexto())
        {
            var doc = await db.Documentos.SingleAsync(d => d.Id == id, ct);
            doc.CreatedBy.Should().Be("luis.alvarado");
            doc.CreatedAt.Should().Be(new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));
            doc.ModifiedAt.Should().BeNull();

            doc.Confirmar();
            await db.SaveChangesAsync(ct);
        }

        await using (var db = entorno.Contexto())
        {
            var doc = await db.Documentos.SingleAsync(d => d.Id == id, ct);
            doc.ModifiedAt.Should().Be(new DateTimeOffset(2026, 10, 5, 13, 0, 0, TimeSpan.Zero));
            doc.ModifiedBy.Should().Be("luis.alvarado");
        }
    }

    [Fact]
    public async Task Archivar_oculta_sin_borrar()
    {
        var entorno = await Entorno.CrearAsync(sql);
        var ct = TestContext.Current.CancellationToken;
        await using (var db = entorno.Contexto())
        {
            var doc = new DocumentoDePrueba("PRB-0002", "MP-001", 1, "KG");
            db.Documentos.Add(doc);
            doc.Archive();
            await db.SaveChangesAsync(ct);
        }

        await using (var db = entorno.Contexto())
        {
            (await db.Documentos.CountAsync(ct)).Should().Be(0);
            (await db.Documentos.IgnoreQueryFilters().CountAsync(ct)).Should().Be(1);
        }
    }

    [Fact]
    public async Task Cada_transicion_queda_en_la_bitacora_con_usuario_rol_fecha_y_nota()
    {
        var entorno = await Entorno.CrearAsync(sql);
        var ct = TestContext.Current.CancellationToken;
        long id;
        await using (var db = entorno.Contexto())
        {
            var doc = new DocumentoDePrueba("PRB-0003", "MP-001", 5, "KG");
            doc.Confirmar();
            db.Documentos.Add(doc);
            await db.SaveChangesAsync(ct);
            id = doc.Id;

            doc.Cancelar();
            await db.SaveChangesAsync(ct);
        }

        await using (var db = entorno.Contexto())
        {
            var bitacora = await db.StateTransitionLogs.Where(l => l.EntityId == id).OrderBy(l => l.Id).ToListAsync(ct);
            bitacora.Select(l => (l.FromState, l.ToState)).Should().Equal(("Borrador", "Confirmado"), ("Confirmado", "Cancelado"));
            bitacora[0].EntityType.Should().Be(nameof(DocumentoDePrueba));
            bitacora[0].UserName.Should().Be("luis.alvarado");
            bitacora[0].Role.Should().Be("Planner");
            bitacora[0].Note.Should().Be("Confirmado en prueba");
            bitacora[0].CorrelationId.Should().Be("corr-prueba");
        }
    }

    [Fact]
    public async Task Una_transicion_invalida_no_toca_la_base()
    {
        var entorno = await Entorno.CrearAsync(sql);
        var ct = TestContext.Current.CancellationToken;
        await using var db = entorno.Contexto();
        var doc = new DocumentoDePrueba("PRB-0004", "MP-001", 5, "KG");
        doc.Cancelar();
        db.Documentos.Add(doc);
        await db.SaveChangesAsync(ct);

        var accion = doc.Confirmar;
        accion.Should().Throw<TransicionInvalidaException>();
        (await db.StateTransitionLogs.CountAsync(l => l.EntityId == doc.Id, ct)).Should().Be(1);
    }
}
