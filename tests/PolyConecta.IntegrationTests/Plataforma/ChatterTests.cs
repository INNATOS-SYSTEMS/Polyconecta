using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Chatter;
using PolyConecta.Domain.Plataforma.Chatter;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Domain.Ventas;
using PolyConecta.Infrastructure.Persistence;
using PolyConecta.IntegrationTests.Soporte;
using PolyConecta.Tests.Compartido;
using Xunit;

namespace PolyConecta.IntegrationTests.Plataforma;

/// <summary>Chatter guardado (L2-T031, R-04, US4 escenarios 3 a 5, SC-007).</summary>
public class ChatterTests(SqlServerFixture sql)
{
    private sealed record Contexto(ApiDePrueba Api, Entorno Entorno, HttpClient Ac, long AcId, long PedidoId, string RowVersion);

    private static async Task<Contexto> LevantarAsync(SqlServerFixture sql)
    {
        var entorno = await Entorno.CrearAsync(sql);
        var cat = await CatalogoDePrueba.SembrarAsync(entorno);
        var api = new ApiDePrueba(entorno);
        var acId = await api.CrearUsuarioAsync("ac1", "Celia Villarreal", (GruposIniciales.AtencionClientes, "PIM", false));
        var ac = await api.ClienteAsync("ac1");
        var r = await ac.PostAsJsonAsync("/api/v1/ventas/pedidos", new
        {
            clienteId = cat.Emm,
            moneda = "MXN",
            lineas = new[] { new { productoId = cat.Bolsa, cantidad = 10m, precioUnitario = (decimal?)2m } },
        });
        var creado = JsonNode.Parse(await r.Content.ReadAsStringAsync())!;
        r.StatusCode.Should().Be(HttpStatusCode.Created, creado.ToJsonString());
        return new Contexto(api, entorno, ac, acId, creado["id"]!.GetValue<long>(), creado["rowVersion"]!.GetValue<string>());
    }

    private static async Task<JsonArray> HistorialAsync(HttpClient c, long pedidoId)
    {
        var r = await c.GetAsync($"/api/v1/plataforma/chatter/ventas.pedido/{pedidoId}");
        var texto = await r.Content.ReadAsStringAsync();
        r.StatusCode.Should().Be(HttpStatusCode.OK, texto);
        return JsonNode.Parse(texto)!.AsArray();
    }

    [Fact]
    public async Task R04_Una_transicion_escribe_su_Cambio_con_usuario_y_grupo_y_se_transmite_al_confirmar()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        (await ctx.Ac.PostAsJsonAsync($"/api/v1/ventas/pedidos/{ctx.PedidoId}/confirmar", new { rowVersion = ctx.RowVersion }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var historial = await HistorialAsync(ctx.Ac, ctx.PedidoId);
        var cambio = historial.Single(m => m!["texto"]!.GetValue<string>() == "Borrador → Confirmado")!;
        cambio["clase"]!.GetValue<string>().Should().Be("Cambio");
        cambio["autor"]!.GetValue<string>().Should().Be("Celia Villarreal");
        cambio["grupo"]!.GetValue<string>().Should().Be("Atención a Clientes");
        ctx.Api.Transmitidos.Should().Contain(m => m.Texto == "Borrador → Confirmado" && m.DocumentoId == ctx.PedidoId);
    }

    [Fact]
    public async Task Un_mensaje_se_guarda_con_el_autor_de_la_sesion_y_se_transmite_a_su_documento()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        var publicado = await ctx.Api.ComoUsuarioAsync(ctx.AcId, "ac1", "Celia Villarreal", sp =>
            sp.GetRequiredService<IUseCase<PublicarMensaje, MensajeChatterDto>>()
                .ExecuteAsync(new PublicarMensaje(DocumentosConChatter.Pedido, ctx.PedidoId, "Nota", "  Revisar el precio  ")));

        publicado.Clase.Should().Be("Nota");
        publicado.Autor.Should().Be("Celia Villarreal");
        publicado.Grupo.Should().Be("Atención a Clientes");
        var historial = await HistorialAsync(ctx.Ac, ctx.PedidoId);
        historial[0]!["texto"]!.GetValue<string>().Should().Be("Revisar el precio", "el historial va del más reciente al más antiguo");
        ctx.Api.Transmitidos.Should().ContainSingle(m => m.Texto == "Revisar el precio");
    }

    [Fact]
    public async Task Sin_permiso_de_leer_el_pedido_no_se_ve_ni_se_escribe_su_chatter()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;
        var almacenistaId = await ctx.Api.CrearUsuarioAsync("alm1", "Almacenista", (GruposIniciales.Almacenista, "PIM", false));
        var almacenista = await ctx.Api.ClienteAsync("alm1");

        (await almacenista.GetAsync($"/api/v1/plataforma/chatter/ventas.pedido/{ctx.PedidoId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var publicar = () => ctx.Api.ComoUsuarioAsync(almacenistaId, "alm1", "Almacenista", sp =>
            sp.GetRequiredService<IUseCase<PublicarMensaje, MensajeChatterDto>>()
                .ExecuteAsync(new PublicarMensaje(DocumentosConChatter.Pedido, ctx.PedidoId, "Mensaje", "Hola")));
        await publicar.Should().ThrowAsync<PermisoDenegadoException>();
        (await ctx.Ac.GetAsync("/api/v1/plataforma/chatter/ventas.pedido/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task R04_Una_transaccion_revertida_no_deja_ni_mensaje_ni_transmision()
    {
        var ctx = await LevantarAsync(sql);
        await using var _ = ctx.Api;

        // Un caso de uso que cancela el pedido y luego falla: el decorador de transacción revierte.
        var falla = () => ctx.Api.ComoUsuarioAsync(ctx.AcId, "ac1", "Celia Villarreal", async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var decorado = new TransactionDecorator<long, Unit>(
                new CancelaYFalla(sp.GetRequiredService<PolyDbContext>(), uow), uow, sp.GetRequiredService<IChatterNotificador>());
            return await decorado.ExecuteAsync(ctx.PedidoId);
        });

        await falla.Should().ThrowAsync<InvalidOperationException>();
        await using var db = ctx.Entorno.Contexto();
        (await db.Set<ChatterMessage>().CountAsync(m => m.DocumentId == ctx.PedidoId && m.Body.Contains("Cancelado"))).Should().Be(0);
        (await db.Pedidos.SingleAsync(p => p.Id == ctx.PedidoId)).State.Should().Be(SalesOrderState.Borrador);
        ctx.Api.Transmitidos.Should().NotContain(m => m.Texto.Contains("Cancelado"));
    }

    private sealed class CancelaYFalla(PolyDbContext db, IUnitOfWork uow) : IUseCase<long, Unit>
    {
        public async Task<Unit> ExecuteAsync(long pedidoId, CancellationToken cancellationToken = default)
        {
            var pedido = await db.Pedidos.SingleAsync(p => p.Id == pedidoId, cancellationToken);
            pedido.Cancelar("Prueba de reversión");
            await uow.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Falla después de guardar.");
        }
    }
}
