using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.IntegrationTests.Plataforma;
using PolyConecta.IntegrationTests.Soporte;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Ventas;

/// <summary>
/// Pruebas de integración del pedido de venta (L2-T023, US1, SC-002, SC-003, SC-007, contracts/api-f1.md).
/// </summary>
public class PedidosTests(SqlServerFixture sql)
{
    private sealed record ContextoPrueba(
        ApiDePrueba Api,
        Catalogo Cat,
        Entorno Entorno,
        HttpClient Admin,
        HttpClient Ac,
        HttpClient Com,
        HttpClient Cob,
        HttpClient CobSup,
        HttpClient Doble,
        HttpClient Calidad,
        HttpClient Planner);

    private static async Task<ContextoPrueba> LevantarAsync(SqlServerFixture sql)
    {
        var entorno = await Entorno.CrearAsync(sql);
        var cat = await CatalogoDePrueba.SembrarAsync(entorno);
        var api = new ApiDePrueba(entorno);

        var acId = await api.CrearUsuarioAsync("ac1", "Celia Villarreal", (GruposIniciales.AtencionClientes, "PIM", false));
        await api.CrearUsuarioAsync("com1", "Ana Treviño", (GruposIniciales.Comercial, "PIM", false));
        await api.CrearUsuarioAsync("com2", "Carlos Comercial", (GruposIniciales.Comercial, "PIM", false));
        await api.CrearUsuarioAsync("cob1", "Beto Cobranza", (GruposIniciales.Cobranza, "PIM", false));
        await api.CrearUsuarioAsync("cob_sup", "Carlos Suplente", (GruposIniciales.Cobranza, "PIM", true));
        await api.CrearUsuarioAsync("doble", "Daniel Doble",
            (GruposIniciales.Comercial, "PIM", false),
            (GruposIniciales.Cobranza, "PIM", false));
        await api.CrearUsuarioAsync("calidad1", "Ernesto Calidad", (GruposIniciales.Calidad, "PIM", false));
        await api.CrearUsuarioAsync("planner1", "Pedro Planner", (GruposIniciales.Planner, "PIM", false));

        var admin = await api.ClienteAsync();

        // D-153: Ligar a Celia Villarreal con su agente de CONTPAQi.
        var rLigar = await admin.PutAsJsonAsync($"/api/v1/plataforma/usuarios/{acId}/agente", new { agenteId = cat.Agente });
        rLigar.IsSuccessStatusCode.Should().BeTrue();

        return new ContextoPrueba(
            api,
            cat,
            entorno,
            admin,
            await api.ClienteAsync("ac1"),
            await api.ClienteAsync("com1"),
            await api.ClienteAsync("cob1"),
            await api.ClienteAsync("cob_sup"),
            await api.ClienteAsync("doble"),
            await api.ClienteAsync("calidad1"),
            await api.ClienteAsync("planner1"));
    }

    private static async Task<JsonNode> JsonAsync(HttpResponseMessage r)
    {
        var texto = await r.Content.ReadAsStringAsync();
        r.IsSuccessStatusCode.Should().BeTrue(texto);
        return JsonNode.Parse(texto)!;
    }

    private static async Task<JsonNode> ErrorJsonAsync(HttpResponseMessage r, HttpStatusCode esperado)
    {
        var texto = await r.Content.ReadAsStringAsync();
        r.StatusCode.Should().Be(esperado, texto);
        return JsonNode.Parse(texto)!;
    }

    [Fact]
    public async Task US1_Recorrido_completo_del_pedido_con_cada_rol_y_su_bitacora()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        // 1. AC crea el pedido con "Nuevo" (FR-019, FR-020, D-136, D-153)
        var postRes = await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Emm,
            ordenCompraCliente = "OC-4471",
            fechaPedido = "2026-10-13",
            fechaPromesa = "2026-10-30",
            domicilioEntregaId = ctx.Cat.EnvioNorte,
            moneda = "USD",
            tipoCambio = 18.5m,
            lineas = new[]
            {
                new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 1200m, precioUnitario = (decimal?)0.85m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null }
            }
        });
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var creado = await JsonAsync(postRes);
        var pedidoId = creado["id"]!.GetValue<long>();
        pedidoId.Should().BeGreaterThan(0);
        creado["estado"]!.GetValue<string>().Should().Be("Borrador");
        creado["folio"]!.GetValue<string>().Should().StartWith("PV-");
        creado["moneda"]!.GetValue<string>().Should().Be("USD");
        creado["tipoCambio"]!.GetValue<decimal>().Should().Be(18.5m);
        creado["agente"]!["id"]!.GetValue<long>().Should().Be(ctx.Cat.Agente, "D-153 propone el agente ligado al usuario");
        creado["lineas"]!.AsArray().Should().HaveCount(1);
        creado["lineas"]![0]!["unidad"]!.GetValue<string>().Should().Be("PZA", "D-127 la unidad es la base del producto");
        creado["lineas"]![0]!["subtotal"]!.GetValue<decimal>().Should().Be(1020m);
        creado["firmas"]!.AsArray().Should().BeEmpty();
        creado["firmasPendientes"]!.AsArray().Select(f => f!.GetValue<string>()).Should().Equal("Comercial", "Cobranza");

        // 2. AC confirma el pedido (FR-022)
        var rowVersion0 = creado["rowVersion"]!.GetValue<string>();
        var rConfirmado = await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/confirmar", new { rowVersion = rowVersion0 });
        var confirmado = await JsonAsync(rConfirmado);
        confirmado["estado"]!.GetValue<string>().Should().Be("Confirmado");
        var rowVersion1 = confirmado["rowVersion"]!.GetValue<string>();
        rowVersion1.Should().NotBe(rowVersion0);

        // 3. Comercial ve las acciones disponibles y firma como Comercial (FR-023, RF-3)
        var detalleCom = await JsonAsync(await ctx.Com.GetAsync($"/api/v1/ventas/pedidos/{pedidoId}"));
        detalleCom["acciones"]!.AsArray().Single(a => a!["accion"]!.GetValue<string>() == "autorizar")!["disponible"]!.GetValue<bool>().Should().BeTrue();

        var rFirmaCom = await ctx.Com.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = rowVersion1 });
        var conFirmaCom = await JsonAsync(rFirmaCom);
        conFirmaCom["estado"]!.GetValue<string>().Should().Be("Confirmado", "con 1 firma sigue Confirmado");
        conFirmaCom["firmas"]!.AsArray().Should().HaveCount(1);
        conFirmaCom["firmas"]![0]!["rol"]!.GetValue<string>().Should().Be("Comercial");
        conFirmaCom["firmas"]![0]!["usuario"]!.GetValue<string>().Should().Be("Ana Treviño");
        conFirmaCom["firmas"]![0]!["grupo"]!.GetValue<string>().Should().Be("Comercial");
        conFirmaCom["firmas"]![0]!["suplente"]!.GetValue<bool>().Should().BeFalse();
        conFirmaCom["firmasPendientes"]!.AsArray().Select(f => f!.GetValue<string>()).Should().Equal("Cobranza");
        var rowVersion2 = conFirmaCom["rowVersion"]!.GetValue<string>();

        // 4. Cobranza firma y el pedido pasa a Autorizado (FR-023)
        var rFirmaCob = await ctx.Cob.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = rowVersion2 });
        var autorizado = await JsonAsync(rFirmaCob);
        autorizado["estado"]!.GetValue<string>().Should().Be("Autorizado");
        autorizado["firmas"]!.AsArray().Should().HaveCount(2);
        autorizado["firmasPendientes"]!.AsArray().Should().BeEmpty();
        var rowVersion3 = autorizado["rowVersion"]!.GetValue<string>();

        // 5. El firmante Comercial revoca la autorización con motivo (FR-025, D-33)
        var rRevocar = await ctx.Com.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/revocar", new
        {
            rowVersion = rowVersion3,
            motivo = "Cambio en condiciones comerciales solicitado por el cliente"
        });
        var revocado = await JsonAsync(rRevocar);
        revocado["estado"]!.GetValue<string>().Should().Be("Confirmado", "regresa a Confirmado al revocar");
        revocado["firmas"]!.AsArray().Should().BeEmpty();
        var rowVersion4 = revocado["rowVersion"]!.GetValue<string>();

        // 6. AC cancela el pedido con motivo (FR-026)
        var rCancelar = await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/cancelar", new
        {
            rowVersion = rowVersion4,
            motivo = "Cancelado por solicitud del cliente"
        });
        var cancelado = await JsonAsync(rCancelar);
        cancelado["estado"]!.GetValue<string>().Should().Be("Cancelado");

        // 7. Verificación de bitácora StateTransitionLog con usuario y grupo ejercido (SC-007, CT-32)
        await using var db = ctx.Entorno.Contexto();
        var logs = await db.StateTransitionLogs
            .Where(x => x.EntityType == "SalesOrder" && x.EntityId == pedidoId)
            .OrderBy(x => x.Id)
            .ToListAsync();

        logs.Should().HaveCount(6);

        // Nuevo -> Borrador
        logs[0].FromState.Should().Be("Nuevo");
        logs[0].ToState.Should().Be("Borrador");
        logs[0].UserName.Should().Be("ac1");
        logs[0].Role.Should().Be("Atención a Clientes");

        // Borrador -> Confirmado
        logs[1].FromState.Should().Be("Borrador");
        logs[1].ToState.Should().Be("Confirmado");
        logs[1].UserName.Should().Be("ac1");
        logs[1].Role.Should().Be("Atención a Clientes");

        // Confirmado -> Confirmado (1 de 2 firmas)
        logs[2].FromState.Should().Be("Confirmado");
        logs[2].ToState.Should().Be("Confirmado");
        logs[2].UserName.Should().Be("com1");
        logs[2].Role.Should().Be("Comercial");
        logs[2].Note.Should().Contain("Firma de Comercial, 1 de 2");

        // Confirmado -> Autorizado (2 de 2 firmas)
        logs[3].FromState.Should().Be("Confirmado");
        logs[3].ToState.Should().Be("Autorizado");
        logs[3].UserName.Should().Be("cob1");
        logs[3].Role.Should().Be("Crédito y Cobranza");
        logs[3].Note.Should().Contain("Firma de Cobranza, 2 de 2");

        // Autorizado -> Confirmado (revocación)
        logs[4].FromState.Should().Be("Autorizado");
        logs[4].ToState.Should().Be("Confirmado");
        logs[4].UserName.Should().Be("com1");
        logs[4].Role.Should().Be("Comercial");
        logs[4].Note.Should().Contain("Autorización revocada");

        // Confirmado -> Cancelado
        logs[5].FromState.Should().Be("Confirmado");
        logs[5].ToState.Should().Be("Cancelado");
        logs[5].UserName.Should().Be("ac1");
        logs[5].Role.Should().Be("Atención a Clientes");
        logs[5].Note.Should().Contain("Cancelado: Cancelado por solicitud del cliente");
    }

    [Fact]
    public async Task SC002_Usuario_con_ambos_roles_no_aporta_las_dos_firmas()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        // Crear y confirmar
        var creado = await JsonAsync(await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 100m, precioUnitario = (decimal?)10m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        }));
        var pedidoId = creado["id"]!.GetValue<long>();
        var rConf = await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/confirmar", new { rowVersion = creado["rowVersion"]!.GetValue<string>() });
        var confirmado = await JsonAsync(rConf);
        var rowVersion1 = confirmado["rowVersion"]!.GetValue<string>();

        // Usuario 'doble' firma como Comercial (elige rol porque puede ambos)
        var rFirma1 = await ctx.Doble.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new
        {
            rowVersion = rowVersion1,
            rol = "Comercial"
        });
        var conFirma1 = await JsonAsync(rFirma1);
        conFirma1["firmas"]!.AsArray().Should().HaveCount(1);
        var rowVersion2 = conFirma1["rowVersion"]!.GetValue<string>();

        // 'doble' intenta dar la segunda firma como Cobranza -> 409 TRANSICION_INVALIDA (RF-4, D-34)
        var rFirma2 = await ctx.Doble.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new
        {
            rowVersion = rowVersion2,
            rol = "Cobranza"
        });
        var errorFirma2 = await ErrorJsonAsync(rFirma2, HttpStatusCode.Conflict);
        errorFirma2["code"]!.GetValue<string>().Should().Be("TRANSICION_INVALIDA");
        errorFirma2["razon"]!.GetValue<string>().Should().Be("Ya firmaste este pedido; la otra firma la da otra persona");

        // 'doble' ve la acción autorizar deshabilitada con su razón en GET
        var detalleDoble = await JsonAsync(await ctx.Doble.GetAsync($"/api/v1/ventas/pedidos/{pedidoId}"));
        var accionAut = detalleDoble["acciones"]!.AsArray().Single(a => a!["accion"]!.GetValue<string>() == "autorizar")!;
        accionAut["disponible"]!.GetValue<bool>().Should().BeFalse();
        accionAut["razon"]!.GetValue<string>().Should().Be("Ya firmaste este pedido; la otra firma la da otra persona");

        // Cobranza (otro usuario distinto) sí puede firmar y pasa a Autorizado
        var rCob = await ctx.Cob.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = rowVersion2 });
        var finalizado = await JsonAsync(rCob);
        finalizado["estado"]!.GetValue<string>().Should().Be("Autorizado");
    }

    [Fact]
    public async Task SC003_Matriz_de_permisos_cada_accion_con_y_sin_permiso_responde_403_con_razon()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        // Crear pedido con AC (permitido)
        var datosCrear = new
        {
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 500m, precioUnitario = (decimal?)2m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        };
        var creado = await JsonAsync(await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", datosCrear));
        var pedidoId = creado["id"]!.GetValue<long>();
        var rowVersion = creado["rowVersion"]!.GetValue<string>();

        // 1. Calidad no tiene permiso para crear pedidos -> 403
        var rCrearDenegado = await ctx.Calidad.PostAsJsonAsync("/api/v1/ventas/pedidos", datosCrear);
        var errCrear = await ErrorJsonAsync(rCrearDenegado, HttpStatusCode.Forbidden);
        errCrear["code"]!.GetValue<string>().Should().Be("PERMISO_DENEGADO");
        errCrear["razon"]!.GetValue<string>().Should().Contain("Crear");

        // 2. Calidad no tiene permiso para editar pedidos -> 403
        var rEditarDenegado = await ctx.Calidad.PutAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}", new
        {
            rowVersion,
            revocarAutorizacion = false,
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 600m, precioUnitario = (decimal?)2m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        });
        var errEditar = await ErrorJsonAsync(rEditarDenegado, HttpStatusCode.Forbidden);
        errEditar["code"]!.GetValue<string>().Should().Be("PERMISO_DENEGADO");
        errEditar["razon"]!.GetValue<string>().Should().Contain("Editar");

        // 3. Calidad no tiene permiso para confirmar -> 403
        var rConfDenegado = await ctx.Calidad.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/confirmar", new { rowVersion });
        var errConf = await ErrorJsonAsync(rConfDenegado, HttpStatusCode.Forbidden);
        errConf["code"]!.GetValue<string>().Should().Be("PERMISO_DENEGADO");
        errConf["razon"]!.GetValue<string>().Should().Contain("Confirmar");

        // 4. Calidad no tiene permiso para autorizar -> 403
        var rAutDenegado = await ctx.Calidad.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion });
        var errAut = await ErrorJsonAsync(rAutDenegado, HttpStatusCode.Forbidden);
        errAut["code"]!.GetValue<string>().Should().Be("PERMISO_DENEGADO");
        errAut["razon"]!.GetValue<string>().Should().Contain("Firmar como Comercial");

        // 5. Calidad no tiene permiso para revocar -> 403
        var rRevDenegado = await ctx.Calidad.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/revocar", new { rowVersion, motivo = "Test" });
        var errRev = await ErrorJsonAsync(rRevDenegado, HttpStatusCode.Forbidden);
        errRev["code"]!.GetValue<string>().Should().Be("PERMISO_DENEGADO");
        errRev["razon"]!.GetValue<string>().Should().Contain("Revocar autorización");

        // 6. Calidad no tiene permiso para cancelar -> 403
        var rCancDenegado = await ctx.Calidad.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/cancelar", new { rowVersion, motivo = "Test" });
        var errCanc = await ErrorJsonAsync(rCancDenegado, HttpStatusCode.Forbidden);
        errCanc["code"]!.GetValue<string>().Should().Be("PERMISO_DENEGADO");
        errCanc["razon"]!.GetValue<string>().Should().Contain("Cancelar");

        // 7. Calidad no tiene ventas.pedido.leer -> GET responde 403 PERMISO_DENEGADO
        var rGetCalidad = await ctx.Calidad.GetAsync($"/api/v1/ventas/pedidos/{pedidoId}");
        var errGetCalidad = await ErrorJsonAsync(rGetCalidad, HttpStatusCode.Forbidden);
        errGetCalidad["code"]!.GetValue<string>().Should().Be("PERMISO_DENEGADO");

        // 8. Planner sí puede leer (ventas.pedido.leer), pero no puede actuar: todas las acciones deshabilitadas con su razón
        var detallePlanner = await JsonAsync(await ctx.Planner.GetAsync($"/api/v1/ventas/pedidos/{pedidoId}"));
        foreach (var a in detallePlanner["acciones"]!.AsArray())
        {
            a!["disponible"]!.GetValue<bool>().Should().BeFalse();
            a!["razon"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task D038_Suplente_de_cobranza_firma_atribuido_como_suplente()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        // Crear y confirmar
        var creado = await JsonAsync(await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 100m, precioUnitario = (decimal?)10m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        }));
        var pedidoId = creado["id"]!.GetValue<long>();
        var conf = await JsonAsync(await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/confirmar", new { rowVersion = creado["rowVersion"]!.GetValue<string>() }));

        // Comercial firma primero
        var conFirmaCom = await JsonAsync(await ctx.Com.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = conf["rowVersion"]!.GetValue<string>() }));

        // Suplente de cobranza firma (D-38)
        var conFirmaSup = await JsonAsync(await ctx.CobSup.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = conFirmaCom["rowVersion"]!.GetValue<string>() }));
        conFirmaSup["estado"]!.GetValue<string>().Should().Be("Autorizado");
        var firmaCob = conFirmaSup["firmas"]!.AsArray().Single(f => f!["rol"]!.GetValue<string>() == "Cobranza")!;
        firmaCob["usuario"]!.GetValue<string>().Should().Be("Carlos Suplente");
        firmaCob["suplente"]!.GetValue<bool>().Should().BeTrue();
        firmaCob["grupo"]!.GetValue<string>().Should().Be("Crédito y Cobranza");

        // Bitácora registra la firma con suplente
        await using var db = ctx.Entorno.Contexto();
        var log = await db.StateTransitionLogs
            .Where(x => x.EntityType == "SalesOrder" && x.EntityId == pedidoId && x.ToState == "Autorizado")
            .SingleAsync();
        log.Note.Should().Contain("suplente");
        log.UserName.Should().Be("cob_sup");
    }

    [Fact]
    public async Task Dos_firmas_simultaneas_del_mismo_rol_una_entra_y_la_otra_recibe_409()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        var com2Client = await ctx.Api.ClienteAsync("com2");

        // Crear y confirmar
        var creado = await JsonAsync(await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 100m, precioUnitario = (decimal?)10m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        }));
        var pedidoId = creado["id"]!.GetValue<long>();
        var conf = await JsonAsync(await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/confirmar", new { rowVersion = creado["rowVersion"]!.GetValue<string>() }));
        var rowVersionOriginal = conf["rowVersion"]!.GetValue<string>();

        // Usuario 1 firma como Comercial con la rowVersion actual -> éxito
        var r1 = await ctx.Com.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = rowVersionOriginal });
        r1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Usuario 2 intenta firmar como Comercial (concurrente / posterior) -> 409 TRANSICION_INVALIDA ("Ya está firmado por Comercial")
        var r2Vieja = await com2Client.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = rowVersionOriginal, rol = "Comercial" });
        var errVieja = await ErrorJsonAsync(r2Vieja, HttpStatusCode.Conflict);
        errVieja["code"]!.GetValue<string>().Should().Be("TRANSICION_INVALIDA");
        errVieja["razon"]!.GetValue<string>().Should().Be("Ya está firmado por Comercial.");

        // Control de concurrencia optimista: editar con rowVersion vieja produce 409 DOCUMENTO_MODIFICADO (R-09)
        var rEditViejo = await ctx.Ac.PutAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}", new
        {
            rowVersion = rowVersionOriginal,
            revocarAutorizacion = true,
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 500m, precioUnitario = (decimal?)10m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        });
        var errEditViejo = await ErrorJsonAsync(rEditViejo, HttpStatusCode.Conflict);
        errEditViejo["code"]!.GetValue<string>().Should().Be("DOCUMENTO_MODIFICADO");
        errEditViejo["razon"]!.GetValue<string>().Should().Contain("Otro usuario cambió el documento");
    }

    [Fact]
    public async Task D147_Editar_pedido_con_firmas_exige_confirmacion_y_revoca_la_autorizacion()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        // Crear y confirmar
        var creado = await JsonAsync(await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 100m, precioUnitario = (decimal?)10m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        }));
        var pedidoId = creado["id"]!.GetValue<long>();
        var conf = await JsonAsync(await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/confirmar", new { rowVersion = creado["rowVersion"]!.GetValue<string>() }));

        // 1 firma de Comercial
        var conFirma = await JsonAsync(await ctx.Com.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = conf["rowVersion"]!.GetValue<string>() }));
        var rowVersion1Firma = conFirma["rowVersion"]!.GetValue<string>();

        // Intento de edición SIN revocarAutorizacion -> 409 EDICION_REVOCA_AUTORIZACION
        var rEditarSinConfirmar = await ctx.Ac.PutAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}", new
        {
            rowVersion = rowVersion1Firma,
            revocarAutorizacion = false,
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 200m, precioUnitario = (decimal?)10m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        });
        var errEdicion = await ErrorJsonAsync(rEditarSinConfirmar, HttpStatusCode.Conflict);
        errEdicion["code"]!.GetValue<string>().Should().Be("EDICION_REVOCA_AUTORIZACION");
        errEdicion["razon"]!.GetValue<string>().Should().Contain("El pedido tiene 1 firma: guardar un cambio revoca la autorización.");

        // Edición CON revocarAutorizacion = true -> éxito, borra firmas y queda Confirmado
        var rEditarConConfirmar = await ctx.Ac.PutAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}", new
        {
            rowVersion = rowVersion1Firma,
            revocarAutorizacion = true,
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 200m, precioUnitario = (decimal?)10m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        });
        var editado = await JsonAsync(rEditarConConfirmar);
        editado["estado"]!.GetValue<string>().Should().Be("Confirmado");
        editado["firmas"]!.AsArray().Should().BeEmpty();
        editado["lineas"]![0]!["cantidad"]!.GetValue<decimal>().Should().Be(200m);

        // Ahora firmamos ambas (Comercial y Cobranza) -> estado Autorizado
        var rowVersionPostEdit = editado["rowVersion"]!.GetValue<string>();
        var firma1 = await JsonAsync(await ctx.Com.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = rowVersionPostEdit }));
        var firma2 = await JsonAsync(await ctx.Cob.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = firma1["rowVersion"]!.GetValue<string>() }));
        firma2["estado"]!.GetValue<string>().Should().Be("Autorizado");
        firma2["firmas"]!.AsArray().Should().HaveCount(2);

        // Edición de pedido Autorizado con revocarAutorizacion = true -> regresa a Confirmado y borra firmas
        var rEditarAutorizado = await ctx.Ac.PutAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}", new
        {
            rowVersion = firma2["rowVersion"]!.GetValue<string>(),
            revocarAutorizacion = true,
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 300m, precioUnitario = (decimal?)10m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        });
        var editadoDesdeAut = await JsonAsync(rEditarAutorizado);
        editadoDesdeAut["estado"]!.GetValue<string>().Should().Be("Confirmado");
        editadoDesdeAut["firmas"]!.AsArray().Should().BeEmpty();
        editadoDesdeAut["lineas"]![0]!["cantidad"]!.GetValue<decimal>().Should().Be(300m);
    }

    [Fact]
    public async Task FR022_No_se_puede_confirmar_pedido_incompleto_y_dice_que_falta()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        // 1. Pedido en Borrador sin líneas
        var sinLineas = await JsonAsync(await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = Array.Empty<object>()
        }));
        var rConfSinLineas = await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{sinLineas["id"]!.GetValue<long>()}/confirmar",
            new { rowVersion = sinLineas["rowVersion"]!.GetValue<string>() });
        var errSinLineas = await ErrorJsonAsync(rConfSinLineas, HttpStatusCode.BadRequest);
        errSinLineas["code"]!.GetValue<string>().Should().Be("VALIDACION");
        errSinLineas["errores"]!.AsArray().Should().Contain(e => e!["campo"]!.GetValue<string>() == "lineas");

        // 2. Pedido con línea sin precio
        var sinPrecio = await JsonAsync(await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 100m, precioUnitario = (decimal?)null, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        }));
        var rConfSinPrecio = await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{sinPrecio["id"]!.GetValue<long>()}/confirmar",
            new { rowVersion = sinPrecio["rowVersion"]!.GetValue<string>() });
        var errSinPrecio = await ErrorJsonAsync(rConfSinPrecio, HttpStatusCode.BadRequest);
        errSinPrecio["errores"]!.AsArray().Should().Contain(e => e!["campo"]!.GetValue<string>() == "lineas[0].precioUnitario");

        // 3. Pedido en moneda extranjera sin tipo de cambio
        var sinTc = await JsonAsync(await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Emm,
            moneda = "USD",
            tipoCambio = (decimal?)null,
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 100m, precioUnitario = (decimal?)1.5m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        }));
        var rConfSinTc = await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{sinTc["id"]!.GetValue<long>()}/confirmar",
            new { rowVersion = sinTc["rowVersion"]!.GetValue<string>() });
        var errSinTc = await ErrorJsonAsync(rConfSinTc, HttpStatusCode.BadRequest);
        errSinTc["errores"]!.AsArray().Should().Contain(e => e!["campo"]!.GetValue<string>() == "tipoCambio");
    }

    [Fact]
    public async Task D149_D153_Propone_agente_del_usuario_y_domicilio_de_envio_unico()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        // Cliente con un solo domicilio de envío (CLI-002 -> Envio Norte/Única)
        var cliUnico = await JsonAsync(await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Cli002,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 10m, precioUnitario = (decimal?)5m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        }));
        // D-149 propone automáticamente el único domicilio de entrega
        cliUnico["domicilioEntrega"]!["id"]!.GetValue<long>().Should().Be(ctx.Cat.Cli002 == 0 ? 0 : cliUnico["domicilioEntrega"]!["id"]!.GetValue<long>());
        cliUnico["agente"]!["id"]!.GetValue<long>().Should().Be(ctx.Cat.Agente, "D-153 propone el agente de CONTPAQi del usuario");

        // Si se intenta poner el domicilio FISCAL como domicilio de entrega -> error 400
        var rFiscal = await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Emm,
            domicilioEntregaId = ctx.Cat.Fiscal,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 10m, precioUnitario = (decimal?)5m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        });
        var errFiscal = await ErrorJsonAsync(rFiscal, HttpStatusCode.BadRequest);
        errFiscal["errores"]!.AsArray().Should().Contain(e => e!["campo"]!.GetValue<string>() == "domicilioEntregaId");
    }

    [Fact]
    public async Task D154_Rutas_por_id_devuelven_404_si_no_existe()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        const long inexistente = 999999;
        var rGet = await ctx.Ac.GetAsync($"/api/v1/ventas/pedidos/{inexistente}");
        var errGet = await ErrorJsonAsync(rGet, HttpStatusCode.NotFound);
        errGet["code"]!.GetValue<string>().Should().Be("NO_ENCONTRADO");

        var rPut = await ctx.Ac.PutAsJsonAsync($"/api/v1/ventas/pedidos/{inexistente}", new
        {
            rowVersion = Convert.ToBase64String(new byte[8]),
            revocarAutorizacion = false,
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = Array.Empty<object>()
        });
        (await ErrorJsonAsync(rPut, HttpStatusCode.NotFound))["code"]!.GetValue<string>().Should().Be("NO_ENCONTRADO");

        var rConf = await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{inexistente}/confirmar", new { rowVersion = Convert.ToBase64String(new byte[8]) });
        (await ErrorJsonAsync(rConf, HttpStatusCode.NotFound))["code"]!.GetValue<string>().Should().Be("NO_ENCONTRADO");
    }

    [Fact]
    public async Task Revocar_y_cancelar_validan_motivo_y_quien_puede_revocar()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        // Crear y confirmar
        var creado = await JsonAsync(await ctx.Ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 100m, precioUnitario = (decimal?)10m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        }));
        var pedidoId = creado["id"]!.GetValue<long>();
        var conf = await JsonAsync(await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/confirmar", new { rowVersion = creado["rowVersion"]!.GetValue<string>() }));

        // Firma Comercial
        var conFirma = await JsonAsync(await ctx.Com.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/autorizar", new { rowVersion = conf["rowVersion"]!.GetValue<string>() }));
        var rowVersion = conFirma["rowVersion"]!.GetValue<string>();

        // 1. Revocar sin motivo -> 400
        var rRevSinMotivo = await ctx.Com.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/revocar", new { rowVersion, motivo = "" });
        var errRevSinMotivo = await ErrorJsonAsync(rRevSinMotivo, HttpStatusCode.BadRequest);
        errRevSinMotivo["code"]!.GetValue<string>().Should().Be("VALIDACION");

        // 2. Cobranza (que no firmó este pedido) intenta revocar -> 409 TRANSICION_INVALIDA (D-33)
        var rRevNoFirmante = await ctx.Cob.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/revocar", new { rowVersion, motivo = "No me gusta" });
        var errRevNoFirmante = await ErrorJsonAsync(rRevNoFirmante, HttpStatusCode.Conflict);
        errRevNoFirmante["code"]!.GetValue<string>().Should().Be("TRANSICION_INVALIDA");
        errRevNoFirmante["razon"]!.GetValue<string>().Should().Be("Solo revoca quien firmó el pedido o el Administrador.");

        // 3. Administrador sí puede revocar aunque no haya firmado (D-33)
        var rRevAdmin = await ctx.Admin.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/revocar", new { rowVersion, motivo = "Revocado por dirección general" });
        var revocadoAdmin = await JsonAsync(rRevAdmin);
        revocadoAdmin["estado"]!.GetValue<string>().Should().Be("Confirmado");
        revocadoAdmin["firmas"]!.AsArray().Should().BeEmpty();

        // 4. Cancelar sin motivo -> 400
        var rCancSinMotivo = await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/cancelar", new { rowVersion = revocadoAdmin["rowVersion"]!.GetValue<string>(), motivo = "   " });
        var errCancSinMotivo = await ErrorJsonAsync(rCancSinMotivo, HttpStatusCode.BadRequest);
        errCancSinMotivo["code"]!.GetValue<string>().Should().Be("VALIDACION");

        // 5. Cancelar pedido con motivo -> éxito
        var rCancOk = await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}/cancelar", new { rowVersion = revocadoAdmin["rowVersion"]!.GetValue<string>(), motivo = "Cancelado por el cliente" });
        var cancelado = await JsonAsync(rCancOk);
        cancelado["estado"]!.GetValue<string>().Should().Be("Cancelado");

        // 6. Intento de editar pedido Cancelado -> 409 TRANSICION_INVALIDA
        var rEditCancelado = await ctx.Ac.PutAsJsonAsync($"/api/v1/ventas/pedidos/{pedidoId}", new
        {
            rowVersion = cancelado["rowVersion"]!.GetValue<string>(),
            revocarAutorizacion = false,
            clienteId = ctx.Cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { id = (long?)null, productoId = ctx.Cat.Bolsa, cantidad = 50m, precioUnitario = (decimal?)10m, metaProduccionKg = (decimal?)null, toleranciaPorcentaje = (decimal?)null } }
        });
        var errEditCanc = await ErrorJsonAsync(rEditCancelado, HttpStatusCode.Conflict);
        errEditCanc["code"]!.GetValue<string>().Should().Be("TRANSICION_INVALIDA");
        errEditCanc["razon"]!.GetValue<string>().Should().Be("Un pedido Cancelado no se edita.");
    }
}
