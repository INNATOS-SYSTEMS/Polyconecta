using AwesomeAssertions;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Plataforma.Seguridad;
using Xunit;

namespace PolyConecta.Domain.Tests.Plataforma;

/// <summary>Usuarios, grupos y el catálogo de permisos (FR-010, D-148).</summary>
public class SeguridadTests
{
    private static readonly AsignacionSolicitada ComercialPim = new(1, 10, false);

    [Fact]
    public void Un_usuario_activo_tiene_al_menos_un_grupo()
    {
        var crear = () => new User("ana", "Ana Treviño", null, []);
        crear.Should().Throw<ReglaDeNegocioException>().Which.Codigo.Should().Be("USUARIO_SIN_GRUPO");

        var usuario = new User("ana", "Ana Treviño", null, [ComercialPim]);
        var quitarTodo = () => usuario.AsignarGrupos([]);
        quitarTodo.Should().Throw<ReglaDeNegocioException>();
        usuario.Assignments.Should().ContainSingle();
    }

    [Fact]
    public void El_usuario_no_lleva_espacios()
    {
        var crear = () => new User("ana t", "Ana", null, [ComercialPim]);
        crear.Should().Throw<ReglaDeNegocioException>().Which.Codigo.Should().Be("USUARIO_CON_ESPACIOS");
    }

    [Fact]
    public void Asignar_grupos_deja_exactamente_los_pedidos_y_actualiza_la_marca_de_suplente()
    {
        var usuario = new User("doble", "Persona Doble", null, [ComercialPim, new(2, 10, false)]);

        usuario.AsignarGrupos([new(1, 10, true), new(3, 20, false)]);

        usuario.Assignments.Select(a => (a.GroupId, a.PlantId, a.IsSubstitute))
            .Should().BeEquivalentTo([(1L, 10L, true), (3L, 20L, false)]);
    }

    [Fact]
    public void Archivar_conserva_las_asignaciones_y_restaurar_exige_un_grupo()
    {
        var usuario = new User("ana", "Ana", null, [ComercialPim]);
        usuario.Archivar();

        usuario.IsActive.Should().BeFalse();
        usuario.Assignments.Should().ContainSingle();
        usuario.Restaurar();
        usuario.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Un_grupo_con_miembros_activos_no_se_archiva()
    {
        var grupo = new Group("COMERCIAL", "Comercial");

        var archivar = () => grupo.Archivar(miembrosActivos: 2);
        archivar.Should().Throw<ReglaDeNegocioException>().Which.Codigo.Should().Be("GRUPO_CON_MIEMBROS");
        grupo.IsActive.Should().BeTrue();

        grupo.Archivar(0);
        grupo.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Copiar_un_grupo_lleva_sus_permisos_y_se_ajusta_sin_tocar_el_original()
    {
        var origen = new Group("SUPERVISOR_TURNO", "Supervisor de turno");
        origen.AsignarPermisos([1, 2, 3]);

        var copia = Group.CopiarDe(origen, "PRODUCCION_SUPERVISOR", "Producción · Supervisor");
        copia.AsignarPermisos([1, 2, 4]);

        copia.Permissions.Select(p => p.PermissionId).Should().BeEquivalentTo([1L, 2L, 4L]);
        origen.Permissions.Select(p => p.PermissionId).Should().BeEquivalentTo([1L, 2L, 3L]);
        copia.Code.Should().Be("PRODUCCION_SUPERVISOR");
    }

    [Fact]
    public void Asignar_permisos_deja_la_lista_final_sin_duplicados()
    {
        var grupo = new Group("X", "X");
        grupo.AsignarPermisos([5, 5, 6]);
        grupo.AsignarPermisos([6, 7]);

        grupo.Permissions.Select(p => p.PermissionId).Should().BeEquivalentTo([6L, 7L]);
    }

    [Fact]
    public void El_catalogo_de_permisos_tiene_claves_unicas_de_tres_niveles()
    {
        Permisos.Catalogo.Select(p => p.Clave).Should().OnlyHaveUniqueItems();
        Permisos.Catalogo.Should().AllSatisfy(p =>
        {
            p.Clave.Split('.').Should().HaveCount(3);
            p.Clave.Should().Be($"{p.Modulo}.{p.Objeto}.{p.Accion}");
        });
    }

    [Fact]
    public void Los_grupos_iniciales_son_los_diez_roles_y_solo_usan_permisos_del_catalogo()
    {
        GruposIniciales.Catalogo.Should().HaveCount(10);
        GruposIniciales.Catalogo.SelectMany(g => g.Permisos)
            .Should().AllSatisfy(clave => Permisos.Buscar(clave).Should().NotBeNull());
    }

    [Fact]
    public void D148_el_Administrador_lee_confirma_cancela_y_revoca_pero_no_crea_ni_edita_ni_firma()
    {
        var admin = GruposIniciales.Catalogo.Single(g => g.Codigo == GruposIniciales.Administrador).Permisos;
        admin.Should().Contain([Permisos.PedidoLeer, Permisos.PedidoConfirmar, Permisos.PedidoCancelar, Permisos.PedidoRevocar]);
        admin.Should().NotContain([Permisos.PedidoCrear, Permisos.PedidoEditar, Permisos.PedidoFirmarComercial, Permisos.PedidoFirmarCobranza]);

        var comercial = GruposIniciales.Catalogo.Single(g => g.Codigo == GruposIniciales.Comercial).Permisos;
        comercial.Should().Contain([Permisos.PedidoLeer, Permisos.PedidoFirmarComercial, Permisos.PedidoRevocar]);
        comercial.Should().NotContain(Permisos.PedidoFirmarCobranza);
        GruposIniciales.Catalogo.Single(g => g.Codigo == GruposIniciales.Cobranza).Permisos.Should().Contain(Permisos.PedidoLeer);
    }
}
