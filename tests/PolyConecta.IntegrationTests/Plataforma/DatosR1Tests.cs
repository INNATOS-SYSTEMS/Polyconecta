using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Infrastructure.Persistence.Sembradores;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Plataforma;

/// <summary>Usuarios de ejemplo de R1 (L2-T033, quickstart, tabla de usuarios de R1).</summary>
public class DatosR1Tests(SqlServerFixture sql)
{
    private const string ContrasenaR1 = "R1Prueba2026x";

    [Fact]
    public async Task Con_su_contrasena_siembra_los_usuarios_de_R1_una_sola_vez_y_entran_con_su_grupo()
    {
        var entorno = await Entorno.CrearAsync(sql);
        var ajustes = new Dictionary<string, string?> { ["Seguridad:DatosR1:Contrasena"] = ContrasenaR1 };
        await using (var api = new ApiDePrueba(entorno, ajustes))
        {
            var suplente = await api.ClienteAsync("cobranza-suplente", ContrasenaR1);
            var sesion = JsonNode.Parse(await (await suplente.GetAsync("/api/v1/plataforma/sesion")).Content.ReadAsStringAsync())!;
            sesion["asignaciones"]![0]!["suplente"]!.GetValue<bool>().Should().BeTrue();
            sesion["asignaciones"]![0]!["planta"]!.GetValue<string>().Should().Be(DatosR1.Planta);

            var doble = await api.ClienteAsync("doble", ContrasenaR1);
            var deDoble = JsonNode.Parse(await (await doble.GetAsync("/api/v1/plataforma/sesion")).Content.ReadAsStringAsync())!;
            deDoble["asignaciones"]!.AsArray().Should().HaveCount(2);
        }

        // Al volver a arrancar no duplica ni cambia a nadie.
        await using (var otraVez = new ApiDePrueba(entorno, ajustes))
            await otraVez.ClienteAsync("ac1", ContrasenaR1);
        await using var db = entorno.Contexto();
        (await db.Usuarios.CountAsync(u => u.UserName != "admin")).Should().Be(7);
    }

    [Fact]
    public async Task Sin_contrasena_no_siembra_a_nadie()
    {
        var entorno = await Entorno.CrearAsync(sql);
        await using var api = new ApiDePrueba(entorno);
        await api.ClienteAsync();

        await using var db = entorno.Contexto();
        (await db.Usuarios.CountAsync(u => u.UserName != "admin")).Should().Be(0);
    }
}
