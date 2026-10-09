using System.Linq.Expressions;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Seguridad;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.Application.Tests.Seguridad;

/// <summary>
/// Reglas de fila (L2-T006, R-02) con un documento de prueba que tiene planta (su almacén de origen):
/// un usuario de PIM no ve los de SC. Su primer uso real es la OF (RF-1, F2).
/// </summary>
public class ReglasDeFilaTests(SqlServerFixture sql)
{
    /// <summary>Como RF-1: solo los documentos de las plantas donde el usuario tiene el permiso de leer.</summary>
    private sealed class SoloSuPlanta : IReglaDeFila<DocumentoDePrueba>
    {
        public Expression<Func<DocumentoDePrueba, bool>> Filtro(ContextoDeReglas contexto)
        {
            var plantas = contexto.Plantas(Permisos.PedidoLeer);
            return d => plantas.Contains(d.Origen);
        }
    }

    private sealed class PermisosFalsos(params AsignacionEfectiva[] asignaciones) : IPermisosDelUsuario
    {
        public Task<IReadOnlyList<AsignacionEfectiva>> AsignacionesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AsignacionEfectiva>>(asignaciones);
    }

    private static AsignacionEfectiva Planner(string planta) =>
        new(1, "PLANNER", "Planner", planta == "PIM" ? 1 : 2, planta, false, new HashSet<string> { Permisos.PedidoLeer });

    private async Task<(Entorno Entorno, long Pim, long Sc)> PrepararAsync()
    {
        var entorno = await Entorno.CrearAsync(sql);
        await using var db = entorno.Contexto();
        var pim = new DocumentoDePrueba("PRB-PIM", "PEBD-001", 1, "KG", origen: "PIM");
        var sc = new DocumentoDePrueba("PRB-SC", "PEBD-001", 1, "KG", origen: "SC");
        db.Documentos.AddRange(pim, sc);
        await db.SaveChangesAsync();
        return (entorno, pim.Id, sc.Id);
    }

    private static ServiceProvider Servicios(Entorno entorno, ICurrentUser usuario, params AsignacionEfectiva[] asignaciones) =>
        ServiciosDePrueba.Crear(entorno, configurar: s =>
        {
            s.AddSingleton(usuario);
            s.AddSingleton<IPermisosDelUsuario>(new PermisosFalsos(asignaciones));
            s.AddSingleton<IReglaDeFila<DocumentoDePrueba>, SoloSuPlanta>();
        });

    [Fact]
    public async Task Un_usuario_de_PIM_no_ve_los_documentos_de_SC()
    {
        var (entorno, pim, sc) = await PrepararAsync();
        await using var sp = Servicios(entorno, new UsuarioFijo("roosvelt", id: 3), Planner("PIM"));
        await using var scope = sp.CreateAsyncScope();
        var almacen = scope.ServiceProvider.GetRequiredService<IAlmacen<DocumentoDePrueba>>();

        (await almacen.ListarAsync()).Select(d => d.Id).Should().Equal(pim);
        (await almacen.PorIdAsync(sc)).Should().BeNull();
        // Un filtro no amplía lo que la regla oculta (02 §7).
        (await almacen.ListarAsync(d => d.Origen == "SC")).Should().BeEmpty();
    }

    [Fact]
    public async Task Con_asignaciones_en_las_dos_plantas_ve_ambos_y_el_sistema_ve_todo()
    {
        var (entorno, pim, sc) = await PrepararAsync();
        await using (var sp = Servicios(entorno, new UsuarioFijo("diana", id: 4), Planner("PIM"), Planner("SC")))
        await using (var scope = sp.CreateAsyncScope())
            (await scope.ServiceProvider.GetRequiredService<IAlmacen<DocumentoDePrueba>>().ListarAsync())
                .Select(d => d.Id).Should().BeEquivalentTo([pim, sc]);

        await using var spSistema = Servicios(entorno, new UsuarioFijo("sistema"));
        await using var scopeSistema = spSistema.CreateAsyncScope();
        (await scopeSistema.ServiceProvider.GetRequiredService<IAlmacen<DocumentoDePrueba>>().ContarAsync(_ => true)).Should().Be(2);
    }
}
