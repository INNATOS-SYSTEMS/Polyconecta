using AwesomeAssertions;
using PolyConecta.Application.Common;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.Application.Tests.Seguridad;

/// <summary>Decorador de autorización (L2-T005, R-02): permiso, planta, suplente y grupo ejercido.</summary>
public class AutorizacionTests
{
    private const long Pim = 1, Sc = 2;

    private sealed class PermisosFalsos(params AsignacionEfectiva[] asignaciones) : IPermisosDelUsuario
    {
        public Task<IReadOnlyList<AsignacionEfectiva>> AsignacionesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AsignacionEfectiva>>(asignaciones);
    }

    private sealed record Peticion(string Permiso, long? PlantaId = null) : IRequierePermiso, IConPlanta;

    private sealed record Firma(IReadOnlyList<string> Permisos) : IRequiereAlgunPermiso;

    private sealed class Registro<T> : IUseCase<T, string>
    {
        public int Llamadas { get; private set; }

        public Task<string> ExecuteAsync(T request, CancellationToken cancellationToken = default)
        {
            Llamadas++;
            return Task.FromResult("hecho");
        }
    }

    private static AsignacionEfectiva Asignacion(long grupo, string nombre, long planta, bool suplente, params string[] permisos) =>
        new(grupo, nombre.ToUpperInvariant(), nombre, planta, planta == Pim ? "PIM" : "SC", suplente, permisos.ToHashSet());

    private static (AuthorizationDecorator<T, string> Decorador, Registro<T> Interno, UsuarioFijo Usuario) Armar<T>(params AsignacionEfectiva[] asignaciones)
    {
        var usuario = new UsuarioFijo("ana", id: 7);
        var interno = new Registro<T>();
        return (new AuthorizationDecorator<T, string>(interno, new Autorizacion(new PermisosFalsos(asignaciones)), usuario), interno, usuario);
    }

    [Fact]
    public async Task Con_el_permiso_corre_y_deja_el_grupo_ejercido()
    {
        var (decorador, interno, usuario) = Armar<Peticion>(Asignacion(3, "Atención a Clientes", Pim, false, Permisos.PedidoConfirmar));

        (await decorador.ExecuteAsync(new Peticion(Permisos.PedidoConfirmar))).Should().Be("hecho");

        interno.Llamadas.Should().Be(1);
        usuario.GrupoEjercido.Should().Be("Atención a Clientes");
        usuario.EsSuplente.Should().BeFalse();
    }

    [Fact]
    public async Task Sin_el_permiso_lanza_403_con_su_razon_y_no_corre_el_caso_de_uso()
    {
        var (decorador, interno, _) = Armar<Peticion>(Asignacion(4, "Planner", Pim, false, Permisos.PedidoLeer));

        var accion = () => decorador.ExecuteAsync(new Peticion(Permisos.PedidoConfirmar));

        var error = (await accion.Should().ThrowAsync<PermisoDenegadoException>()).Which;
        error.Razon.Should().Be("Tu grupo no tiene el permiso Ventas › Pedido › Confirmar.");
        interno.Llamadas.Should().Be(0);
    }

    [Fact]
    public async Task Con_planta_solo_cuentan_las_asignaciones_de_esa_planta()
    {
        var (decorador, _, usuario) = Armar<Peticion>(Asignacion(4, "Planner", Sc, false, Permisos.PedidoLeer));

        var enPim = () => decorador.ExecuteAsync(new Peticion(Permisos.PedidoLeer, Pim));
        await enPim.Should().ThrowAsync<PermisoDenegadoException>();

        await decorador.ExecuteAsync(new Peticion(Permisos.PedidoLeer, Sc));
        usuario.GrupoEjercido.Should().Be("Planner");
    }

    [Fact]
    public async Task El_titular_va_antes_que_el_suplente_y_el_suplente_queda_marcado()
    {
        var (decorador, _, usuario) = Armar<Peticion>(
            Asignacion(5, "Cobranza", Pim, true, Permisos.PedidoFirmarCobranza),
            Asignacion(6, "Cobranza titular", Sc, false, Permisos.PedidoFirmarCobranza));

        await decorador.ExecuteAsync(new Peticion(Permisos.PedidoFirmarCobranza));
        usuario.GrupoEjercido.Should().Be("Cobranza titular");
        usuario.EsSuplente.Should().BeFalse();

        await decorador.ExecuteAsync(new Peticion(Permisos.PedidoFirmarCobranza, Pim));
        usuario.GrupoEjercido.Should().Be("Cobranza");
        usuario.EsSuplente.Should().BeTrue();
    }

    [Fact]
    public async Task Con_varios_permisos_basta_cualquiera()
    {
        var (decorador, interno, usuario) = Armar<Firma>(Asignacion(7, "Comercial", Pim, false, Permisos.PedidoFirmarComercial));

        await decorador.ExecuteAsync(new Firma([Permisos.PedidoFirmarCobranza, Permisos.PedidoFirmarComercial]));

        interno.Llamadas.Should().Be(1);
        usuario.GrupoEjercido.Should().Be("Comercial");
    }

    [Fact]
    public async Task Lo_que_corre_como_sistema_no_pasa_por_permisos()
    {
        var interno = new Registro<Peticion>();
        var decorador = new AuthorizationDecorator<Peticion, string>(interno, new Autorizacion(new PermisosFalsos()), new UsuarioFijo("sistema"));

        await decorador.ExecuteAsync(new Peticion(Permisos.SincronizacionEjecutar));

        interno.Llamadas.Should().Be(1);
    }
}
