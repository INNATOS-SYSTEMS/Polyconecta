using Microsoft.EntityFrameworkCore;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Ventas;
using PolyConecta.Tests.Compartido;

namespace PolyConecta.IntegrationTests.Soporte;

/// <summary>Ids del catálogo de prueba, como lo dejaría la sincronización (los ejemplos de `1.1` del contrato).</summary>
public sealed record Catalogo(long Bolsa, long Pebd, long Inactivo, long Emm, long Cli002, long EnvioNorte, long EnvioSaltillo, long Fiscal, long Agente, long OtroAgente);

/// <summary>Catálogo sincronizado sin bridge: lo mismo que traen los ejemplos del contrato `1.1`.</summary>
public static class CatalogoDePrueba
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 8, 0, 0, TimeSpan.Zero);

    private static DatosDomicilio Dom(long id, TipoDomicilio tipo, string calle, string? sucursal) =>
        new(id, tipo, calle, "120", null, "Centro", "66600", "Apodaca", "Apodaca", "Nuevo León", "México", sucursal);

    public static async Task<Catalogo> SembrarAsync(Entorno entorno)
    {
        await using var db = entorno.Contexto();
        var bolsa = Product.DesdeErp(new DatosErpProducto(1113, "PT1113 C567", "BOLSA MEDIANA 44X84 C.430 BOL-004 [77]", "PZA", true, true), Ahora);
        var pebd = Product.DesdeErp(new DatosErpProducto(201, "PEBD-001", "Polietileno baja densidad", "KG", false, true), Ahora);
        var inactivo = Product.DesdeErp(new DatosErpProducto(999, "VIEJO-01", "Producto descontinuado", "KG", false, true), Ahora);
        inactivo.ArchivarPorErp();
        var emm = Customer.DesdeErp(new DatosErpCliente(57, "EMM-001", "EMPRESA MEXICANA DE MANUFACTURA", "EMM010101AAA", true, "USD",
            [Dom(901, TipoDomicilio.Fiscal, "Av. Industrial", null), Dom(902, TipoDomicilio.Envio, "Carretera Miguel Alemán", "Planta Norte"),
             Dom(903, TipoDomicilio.Envio, "Blvd. Saltillo", "CEDIS Saltillo")]));
        var cli = Customer.DesdeErp(new DatosErpCliente(58, "CLI-002", "COMERCIALIZADORA DEL NORTE", null, true, "MXN",
            [Dom(904, TipoDomicilio.Fiscal, "Morelos", null), Dom(905, TipoDomicilio.Envio, "Juárez", "Única")]));
        var agente = new ErpAgent(3, "AG-01", "Celia Villarreal", TipoAgente.Venta);
        var otro = new ErpAgent(5, "AG-03", "Agente de mostrador", TipoAgente.Venta);
        db.Productos.AddRange(bolsa, pebd, inactivo);
        db.Clientes.AddRange(emm, cli);
        db.AgentesErp.AddRange(agente, otro);
        db.AlmacenesErp.Add(new ErpWarehouse(1, "MP-PIM", "Materia prima PIM"));
        await db.SaveChangesAsync();
        var d = await db.Clientes.Include(c => c.Addresses).IgnoreQueryFilters().ToListAsync();
        long Id(long erp) => d.SelectMany(c => c.Addresses).Single(a => a.ErpAddressId == erp).Id;
        return new Catalogo(bolsa.Id, pebd.Id, inactivo.Id, emm.Id, cli.Id, Id(902), Id(903), Id(901), agente.Id, otro.Id);
    }
}
