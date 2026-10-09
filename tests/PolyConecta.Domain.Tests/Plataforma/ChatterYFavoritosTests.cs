using AwesomeAssertions;
using PolyConecta.Domain.Plataforma.Chatter;
using PolyConecta.Domain.Plataforma.Listas;
using Xunit;

namespace PolyConecta.Domain.Tests.Plataforma;

public class ChatterYFavoritosTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Un_mensaje_guarda_autor_grupo_y_texto_sin_espacios_sobrantes()
    {
        var m = ChatterMessage.Publicar("ventas.pedido", 15, TipoMensaje.Nota, "  Revisar precio  ", 7, "Celia Villarreal", "ATENCION_CLIENTES", Ahora);

        m.Body.Should().Be("Revisar precio");
        m.Kind.Should().Be(TipoMensaje.Nota);
        m.AuthorUserId.Should().Be(7);
        m.GroupExercised.Should().Be("ATENCION_CLIENTES");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_mensaje_vacio_no_se_publica(string texto)
    {
        var publicar = () => ChatterMessage.Publicar("ventas.pedido", 15, TipoMensaje.Mensaje, texto, 7, "Celia", null, Ahora);

        publicar.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Un_mensaje_de_mas_de_4000_caracteres_no_se_publica()
    {
        var publicar = () => ChatterMessage.Publicar("ventas.pedido", 15, TipoMensaje.Mensaje, new string('x', 4001), 7, "Celia", null, Ahora);

        publicar.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void R04_Un_usuario_no_publica_un_cambio()
    {
        var publicar = () => ChatterMessage.Publicar("ventas.pedido", 15, TipoMensaje.Cambio, "Borrador → Confirmado", 7, "Celia", null, Ahora);

        publicar.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void R04_El_cambio_dice_la_transicion_y_su_nota()
    {
        var cambio = ChatterMessage.RegistrarCambio("ventas.pedido", 15, "Autorizado", "Confirmado", "Revocada por edición", "Celia", "ATENCION_CLIENTES", Ahora);

        cambio.Kind.Should().Be(TipoMensaje.Cambio);
        cambio.Body.Should().Be("Autorizado → Confirmado: Revocada por edición");
        cambio.AuthorUserId.Should().BeNull();
    }

    [Fact]
    public void Un_cambio_sin_cambio_de_estado_muestra_solo_el_estado_y_la_nota()
    {
        var cambio = ChatterMessage.RegistrarCambio("ventas.pedido", 15, "Confirmado", "Confirmado", "Firma de Comercial", "Rosa", null, Ahora);

        cambio.Body.Should().Be("Confirmado: Firma de Comercial");
    }

    [Fact]
    public void Un_favorito_exige_nombre_y_definicion()
    {
        var sinNombre = () => new SavedSearch(1, "ventas.pedidos", " ", "{}", false, Ahora);
        var sinDefinicion = () => new SavedSearch(1, "ventas.pedidos", "Por autorizar", "", false, Ahora);

        sinNombre.Should().Throw<ArgumentException>();
        sinDefinicion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Un_favorito_se_renombra_y_se_marca_por_omision()
    {
        var favorito = new SavedSearch(1, "ventas.pedidos", "Por autorizar", "{\"filtros\":[\"Confirmado\"]}", false, Ahora);

        favorito.Renombrar("  Pendientes de firma ");
        favorito.MarcarPorOmision(true);

        favorito.Name.Should().Be("Pendientes de firma");
        favorito.IsDefault.Should().BeTrue();
    }
}
