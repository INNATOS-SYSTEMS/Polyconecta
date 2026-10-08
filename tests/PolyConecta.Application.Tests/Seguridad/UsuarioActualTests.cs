using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using PolyConecta.Infrastructure.Plataforma.Identidad;
using Xunit;

namespace PolyConecta.Application.Tests.Seguridad;

/// <summary>ICurrentUser desde la cookie (L2-T004, FR-012).</summary>
public class UsuarioActualTests
{
    private static CurrentUserDesdeCookie Con(HttpContext? contexto) => new(new HttpContextAccessor { HttpContext = contexto });

    [Fact]
    public void Fuera_de_una_peticion_es_sistema()
    {
        var usuario = Con(null);

        usuario.UserName.Should().Be("sistema");
        usuario.UserId.Should().BeNull();
        usuario.GrupoEjercido.Should().BeNull();
    }

    [Fact]
    public void Con_sesion_lee_el_usuario_de_los_claims_y_guarda_el_grupo_ejercido()
    {
        var identidad = new ClaimsIdentity(
            [
                new Claim(ClaimsDeSesion.UserId, "7"),
                new Claim(ClaimsDeSesion.UserName, "cvillarreal"),
                new Claim(ClaimsDeSesion.NombreVisible, "Celia Villarreal"),
            ], "Cookies");
        var usuario = Con(new DefaultHttpContext { User = new ClaimsPrincipal(identidad) });

        usuario.EjercerGrupo("Cobranza", esSuplente: true);

        usuario.UserName.Should().Be("cvillarreal");
        usuario.UserId.Should().Be(7);
        usuario.NombreVisible.Should().Be("Celia Villarreal");
        usuario.GrupoEjercido.Should().Be("Cobranza");
        usuario.EsSuplente.Should().BeTrue();
        ((PolyConecta.Application.Common.ICurrentUser)usuario).Role.Should().Be("Cobranza");
    }

    [Fact]
    public void Sin_autenticar_sigue_siendo_sistema()
    {
        Con(new DefaultHttpContext()).UserName.Should().Be("sistema");
    }
}
