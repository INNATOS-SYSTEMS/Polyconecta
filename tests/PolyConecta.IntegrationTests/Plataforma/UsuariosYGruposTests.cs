using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Plataforma;

/// <summary>Usuarios y grupos por la API (L2-T009, US2, FR-013).</summary>
public class UsuariosYGruposTests(SqlServerFixture sql)
{
    private sealed record Ids(long Comercial, long Cobranza, long Supervisor, long Pim, long Sc);

    private static async Task<(Entorno Entorno, ApiDePrueba Api, HttpClient Admin, Ids Ids)> LevantarAsync(SqlServerFixture sql)
    {
        var entorno = await Entorno.CrearAsync(sql);
        var api = new ApiDePrueba(entorno);
        var admin = await api.ClienteAsync();
        await using var db = entorno.Contexto();
        var g = await db.Groups.ToDictionaryAsync(x => x.Code, x => x.Id);
        var p = await db.Plants.ToDictionaryAsync(x => x.Code, x => x.Id);
        return (entorno, api, admin, new Ids(g[GruposIniciales.Comercial], g[GruposIniciales.Cobranza], g[GruposIniciales.Supervisor], p["PIM"], p["SC"]));
    }

    private static async Task<JsonNode> JsonAsync(HttpResponseMessage r)
    {
        var texto = await r.Content.ReadAsStringAsync();
        r.IsSuccessStatusCode.Should().BeTrue(texto);
        return JsonNode.Parse(texto)!;
    }

    private static async Task<string[]> PermisosDeSesionAsync(HttpClient c) =>
        (await JsonAsync(await c.GetAsync("/api/v1/plataforma/sesion")))["permisos"]!.AsArray().Select(x => x!.GetValue<string>()).ToArray();

    [Fact]
    public async Task El_Administrador_crea_un_suplente_de_Comercial_en_PIM_que_entra_con_sus_permisos()
    {
        var (_, api, admin, ids) = await LevantarAsync(sql);
        await using var _ = api;

        var r = await admin.PostAsJsonAsync("/api/v1/plataforma/usuarios", new
        {
            usuario = "atrevino", nombre = "Ana Treviño", contrasena = "Comercial2026",
            asignaciones = new[] { new { grupoId = ids.Comercial, plantaId = ids.Pim, suplente = true } },
        });
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var creado = await JsonAsync(r);
        creado["asignaciones"]![0]!["grupo"]!.GetValue<string>().Should().Be("Comercial");
        creado["asignaciones"]![0]!["suplente"]!.GetValue<bool>().Should().BeTrue();

        var ana = await api.ClienteAsync("atrevino", "Comercial2026");
        (await PermisosDeSesionAsync(ana)).Should().Contain(Permisos.PedidoFirmarComercial);
    }

    [Fact]
    public async Task Quitar_un_permiso_a_un_grupo_lo_quita_a_sus_miembros_sin_tocar_codigo()
    {
        var (_, api, admin, ids) = await LevantarAsync(sql);
        await using var _ = api;
        await api.CrearUsuarioAsync("comercial1", "Comercial Uno", (GruposIniciales.Comercial, "PIM", false));
        var comercial1 = await api.ClienteAsync("comercial1");
        (await PermisosDeSesionAsync(comercial1)).Should().Contain(Permisos.PedidoFirmarComercial);

        var grupo = await JsonAsync(await admin.GetAsync($"/api/v1/plataforma/grupos/{ids.Comercial}"));
        var permisos = grupo["permisos"]!.AsArray().Select(x => x!.GetValue<string>()).Where(p => p != Permisos.PedidoFirmarComercial).ToArray();
        var editado = await JsonAsync(await admin.PutAsJsonAsync($"/api/v1/plataforma/grupos/{ids.Comercial}", new
        {
            rowVersion = grupo["rowVersion"]!.GetValue<string>(), nombre = "Comercial", descripcion = (string?)null, permisos,
        }));
        editado["permisos"]!.AsArray().Select(x => x!.GetValue<string>()).Should().NotContain(Permisos.PedidoFirmarComercial);

        (await PermisosDeSesionAsync(comercial1)).Should().NotContain(Permisos.PedidoFirmarComercial);
    }

    [Fact]
    public async Task El_arbol_de_permisos_va_por_modulo_objeto_y_accion()
    {
        var (_, api, admin, _) = await LevantarAsync(sql);
        await using var _ = api;

        var arbol = (await JsonAsync(await admin.GetAsync("/api/v1/plataforma/permisos"))).AsArray();

        var ventas = arbol.Single(m => m!["modulo"]!.GetValue<string>() == "ventas")!;
        var pedido = ventas["objetos"]!.AsArray().Single(o => o!["objeto"]!.GetValue<string>() == "pedido")!;
        pedido["tipo"]!.GetValue<string>().Should().Be("Documento");
        pedido["acciones"]!.AsArray().Select(a => a!["clave"]!.GetValue<string>()).Should().Contain(Permisos.PedidoConfirmar);
        arbol.SelectMany(m => m!["objetos"]!.AsArray()).SelectMany(o => o!["acciones"]!.AsArray()).Should().HaveCount(Permisos.Catalogo.Count);
    }

    [Fact]
    public async Task Copiar_un_grupo_lleva_sus_permisos_y_no_se_archiva_uno_con_miembros()
    {
        var (_, api, admin, ids) = await LevantarAsync(sql);
        await using var _ = api;

        var copia = await JsonAsync(await admin.PostAsJsonAsync("/api/v1/plataforma/grupos", new
        {
            codigo = "PRODUCCION_SUPERVISOR", nombre = "Producción · Supervisor", copiarDe = ids.Supervisor,
        }));
        var original = await JsonAsync(await admin.GetAsync($"/api/v1/plataforma/grupos/{ids.Supervisor}"));
        copia["permisos"]!.ToJsonString().Should().Be(original["permisos"]!.ToJsonString());

        // El Administrador inicial es miembro del grupo Administrador.
        var admins = await JsonAsync(await admin.GetAsync("/api/v1/plataforma/sesion"));
        var grupoAdmin = admins["asignaciones"]![0]!["grupo"]!.GetValue<string>();
        grupoAdmin.Should().Be(GruposIniciales.Administrador);
        var r = await admin.PostAsync($"/api/v1/plataforma/grupos/{ids.Comercial}/archivar", null);
        r.StatusCode.Should().Be(HttpStatusCode.OK); // sin miembros
        await api.CrearUsuarioAsync("sup1", "Supervisor", ("PRODUCCION_SUPERVISOR", "SC", false));
        var conMiembros = await admin.PostAsync($"/api/v1/plataforma/grupos/{copia["id"]}/archivar", null);
        conMiembros.StatusCode.Should().Be(HttpStatusCode.Conflict);
        JsonDocument.Parse(await conMiembros.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString().Should().Be("GRUPO_CON_MIEMBROS");
    }

    [Fact]
    public async Task Sin_el_permiso_de_administrar_la_API_responde_403_con_su_razon()
    {
        var (_, api, _, ids) = await LevantarAsync(sql);
        await using var _ = api;
        await api.CrearUsuarioAsync("planner1", "Planner Uno", (GruposIniciales.Planner, "PIM", false));
        var planner = await api.ClienteAsync("planner1");

        var r = await planner.PostAsJsonAsync("/api/v1/plataforma/usuarios", new
        {
            usuario = "x", nombre = "X", contrasena = "Algo2026x",
            asignaciones = new[] { new { grupoId = ids.Comercial, plantaId = ids.Pim, suplente = false } },
        });

        r.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var cuerpo = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        cuerpo.GetProperty("code").GetString().Should().Be("PERMISO_DENEGADO");
        cuerpo.GetProperty("razon").GetString().Should().Be("Tu grupo no tiene el permiso Plataforma › Usuarios › Administrar.");
    }

    [Fact]
    public async Task Archivar_restaurar_editar_y_restablecer_la_contrasena()
    {
        var (_, api, admin, ids) = await LevantarAsync(sql);
        await using var _ = api;
        var id = await api.CrearUsuarioAsync("cobranza1", "Cobranza Uno", (GruposIniciales.Cobranza, "PIM", false));

        var u = await JsonAsync(await admin.GetAsync($"/api/v1/plataforma/usuarios/{id}"));
        var editado = await JsonAsync(await admin.PutAsJsonAsync($"/api/v1/plataforma/usuarios/{id}", new
        {
            rowVersion = u["rowVersion"]!.GetValue<string>(), nombre = "Cobranza Uno", email = "c1@poly.mx",
            asignaciones = new[] { new { grupoId = ids.Cobranza, plantaId = ids.Pim, suplente = false }, new { grupoId = ids.Cobranza, plantaId = ids.Sc, suplente = true } },
        }));
        editado["asignaciones"]!.AsArray().Should().HaveCount(2);

        // Con la versión vieja, 409 DOCUMENTO_MODIFICADO.
        var vieja = await admin.PutAsJsonAsync($"/api/v1/plataforma/usuarios/{id}", new
        {
            rowVersion = u["rowVersion"]!.GetValue<string>(), nombre = "Otro", email = (string?)null,
            asignaciones = new[] { new { grupoId = ids.Cobranza, plantaId = ids.Pim, suplente = false } },
        });
        vieja.StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await JsonAsync(await admin.PostAsync($"/api/v1/plataforma/usuarios/{id}/archivar", null)))["activo"]!.GetValue<bool>().Should().BeFalse();
        (await JsonAsync(await admin.PostAsync($"/api/v1/plataforma/usuarios/{id}/restaurar", null)))["activo"]!.GetValue<bool>().Should().BeTrue();

        var corta = await admin.PostAsJsonAsync($"/api/v1/plataforma/usuarios/{id}/contrasena", new { contrasena = "corta" });
        corta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PostAsJsonAsync($"/api/v1/plataforma/usuarios/{id}/contrasena", new { contrasena = "Nueva2026x" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        await api.ClienteAsync("cobranza1", "Nueva2026x");
    }

    [Fact]
    public async Task D153_el_Administrador_liga_el_usuario_a_su_agente_de_CONTPAQi()
    {
        var (entorno, api, admin, _) = await LevantarAsync(sql);
        await using var _ = api;
        var id = await api.CrearUsuarioAsync("ac1", "Celia Villarreal", (GruposIniciales.AtencionClientes, "PIM", false));
        long agente;
        await using (var db = entorno.Contexto())
        {
            var a = new PolyConecta.Domain.Ventas.ErpAgent(3, "AG-01", "Celia Villarreal", PolyConecta.Domain.Ventas.TipoAgente.Venta);
            db.AgentesErp.Add(a);
            await db.SaveChangesAsync();
            agente = a.Id;
        }

        var u = await JsonAsync(await admin.PutAsJsonAsync($"/api/v1/plataforma/usuarios/{id}/agente", new { agenteId = agente }));
        u["agenteId"]!.GetValue<long>().Should().Be(agente);
        (await admin.PutAsJsonAsync($"/api/v1/plataforma/usuarios/{id}/agente", new { agenteId = 999_999 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var ac = await api.ClienteAsync("ac1");
        (await ac.PutAsJsonAsync($"/api/v1/plataforma/usuarios/{id}/agente", new { agenteId = agente })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>La bitácora de un catálogo registra quién cambió qué (D-157, decisión del 9-oct): grupos de un usuario y permisos de un grupo.</summary>
    [Fact]
    public async Task La_bitacora_de_usuarios_y_grupos_anota_sus_cambios()
    {
        var (_, api, admin, ids) = await LevantarAsync(sql);
        await using var _ = api;

        var creado = await JsonAsync(await admin.PostAsJsonAsync("/api/v1/plataforma/usuarios", new
        {
            usuario = "bitacora1", nombre = "Bitácora Uno", contrasena = "Bitacora2026",
            asignaciones = new[] { new { grupoId = ids.Comercial, plantaId = ids.Pim, suplente = false } },
        }));
        var id = creado["id"]!.GetValue<long>();
        await JsonAsync(await admin.PutAsJsonAsync($"/api/v1/plataforma/usuarios/{id}", new
        {
            rowVersion = creado["rowVersion"]!.GetValue<string>(), nombre = "Bitácora Dos", email = (string?)null,
            asignaciones = new[]
            {
                new { grupoId = ids.Comercial, plantaId = ids.Pim, suplente = false },
                new { grupoId = ids.Cobranza, plantaId = ids.Sc, suplente = true },
            },
        }));

        var usuario = (await JsonAsync(await admin.GetAsync($"/api/v1/plataforma/chatter/plataforma.usuario/{id}"))).AsArray()
            .Select(m => m!["texto"]!.GetValue<string>()).ToList();
        usuario.Should().Contain("Creó el usuario.");
        usuario.Should().Contain("Nombre: Bitácora Uno → Bitácora Dos");
        usuario.Should().Contain(t => t.StartsWith("Agregó ", StringComparison.Ordinal) && t.EndsWith(" · SC (suplente)", StringComparison.Ordinal));

        var grupo = await JsonAsync(await admin.GetAsync($"/api/v1/plataforma/grupos/{ids.Supervisor}"));
        var permisos = grupo["permisos"]!.AsArray().Select(x => x!.GetValue<string>()).Append(Permisos.PedidoLeer).Distinct().ToArray();
        await JsonAsync(await admin.PutAsJsonAsync($"/api/v1/plataforma/grupos/{ids.Supervisor}", new
        {
            rowVersion = grupo["rowVersion"]!.GetValue<string>(), nombre = grupo["nombre"]!.GetValue<string>(),
            descripcion = (string?)null, permisos,
        }));
        var mensajesGrupo = (await JsonAsync(await admin.GetAsync($"/api/v1/plataforma/chatter/plataforma.grupo/{ids.Supervisor}"))).AsArray();
        mensajesGrupo.Select(m => m!["clase"]!.GetValue<string>()).Should().OnlyContain(c => c == "Cambio");
        mensajesGrupo.Select(m => m!["texto"]!.GetValue<string>()).Should().Contain(t => t.StartsWith("Agregó Ventas › Pedido ›", StringComparison.Ordinal));
    }
}
