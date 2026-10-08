using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Plataforma;

/// <summary>Sesión con cookie (L2-T008, FR-009, US2 escenarios 1, 2 y 5).</summary>
public class SesionTests(SqlServerFixture sql)
{
    private const string Ruta = "/api/v1/plataforma/sesion";

    private static async Task<(Entorno Entorno, ApiDePrueba Api)> LevantarAsync(SqlServerFixture sql)
    {
        var entorno = await Entorno.CrearAsync(sql);
        var api = new ApiDePrueba(entorno);
        await api.CrearUsuarioAsync("cvillarreal", "Celia Villarreal", (GruposIniciales.AtencionClientes, "PIM", false));
        return (entorno, api);
    }

    private static HttpClient SinSesion(ApiDePrueba api)
    {
        var c = api.CreateClient();
        c.DefaultRequestHeaders.Add("X-Requested-With", "PolyConecta");
        return c;
    }

    private static Task<HttpResponseMessage> EntrarAsync(HttpClient c, string usuario, string contrasena) =>
        c.PostAsJsonAsync(Ruta, new { usuario, contrasena });

    private static async Task<string?> CodigoAsync(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString();

    [Fact]
    public async Task Entrar_da_la_sesion_con_usuario_grupos_y_permisos_y_salir_la_termina()
    {
        var (_, api) = await LevantarAsync(sql);
        await using var _ = api;
        var c = SinSesion(api);

        var r = await EntrarAsync(c, "cvillarreal", ApiDePrueba.Contrasena);
        r.StatusCode.Should().Be(HttpStatusCode.OK);

        var sesion = JsonDocument.Parse(await (await c.GetAsync(Ruta)).Content.ReadAsStringAsync()).RootElement;
        sesion.GetProperty("usuario").GetProperty("nombre").GetString().Should().Be("Celia Villarreal");
        sesion.GetProperty("asignaciones")[0].GetProperty("grupo").GetString().Should().Be(GruposIniciales.AtencionClientes);
        sesion.GetProperty("asignaciones")[0].GetProperty("planta").GetString().Should().Be("PIM");
        sesion.GetProperty("permisos").EnumerateArray().Select(p => p.GetString()).Should().Contain(Permisos.PedidoConfirmar);

        (await c.DeleteAsync(Ruta)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await c.GetAsync(Ruta)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Sin_sesion_toda_ruta_responde_401_y_el_callback_del_bridge_no_la_pide()
    {
        var (_, api) = await LevantarAsync(sql);
        await using var _ = api;
        var c = SinSesion(api);

        var r = await c.GetAsync(Ruta);
        r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await CodigoAsync(r)).Should().Be("SIN_SESION");
        (await c.PostAsync($"/api/v1/plataforma/outbox/{Guid.NewGuid()}/reintentar", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // El callback responde por su firma (401 sin firma válida), nunca redirige a iniciar sesión.
        (await c.PostAsJsonAsync("/api/v1/plataforma/bridge/callbacks", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Credenciales_malas_dan_401_y_cinco_intentos_bloquean_al_usuario()
    {
        var (_, api) = await LevantarAsync(sql);
        await using var _ = api;
        var c = SinSesion(api);

        var mala = await EntrarAsync(c, "cvillarreal", "Equivocada1");
        mala.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await CodigoAsync(mala)).Should().Be("CREDENCIALES_INVALIDAS");
        (await CodigoAsync(await EntrarAsync(c, "nadie", "Equivocada1"))).Should().Be("CREDENCIALES_INVALIDAS");

        for (var i = 0; i < 4; i++) await EntrarAsync(c, "cvillarreal", "Equivocada1");
        var bloqueada = await EntrarAsync(c, "cvillarreal", ApiDePrueba.Contrasena);
        bloqueada.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await CodigoAsync(bloqueada)).Should().Be("USUARIO_BLOQUEADO");
    }

    [Fact]
    public async Task Un_usuario_archivado_no_entra_y_su_sesion_abierta_deja_de_valer()
    {
        var (entorno, api) = await LevantarAsync(sql);
        await using var _ = api;
        var abierta = await api.ClienteAsync("cvillarreal");

        await using (var db = entorno.Contexto())
        {
            var u = await db.Usuarios.SingleAsync(x => x.UserName == "cvillarreal");
            u.Archivar();
            await db.SaveChangesAsync();
        }

        (await abierta.GetAsync(Ruta)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await CodigoAsync(await EntrarAsync(SinSesion(api), "cvillarreal", ApiDePrueba.Contrasena))).Should().Be("CREDENCIALES_INVALIDAS");
    }

    [Fact]
    public async Task Una_peticion_que_escribe_sin_X_Requested_With_se_rechaza()
    {
        var (_, api) = await LevantarAsync(sql);
        await using var _ = api;
        var c = api.CreateClient();

        var r = await EntrarAsync(c, "cvillarreal", ApiDePrueba.Contrasena);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodigoAsync(r)).Should().Be("FALTA_X_REQUESTED_WITH");
    }
}
