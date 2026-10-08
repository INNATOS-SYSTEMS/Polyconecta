using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Erp;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Infrastructure.Erp;

/// <summary>
/// Lecturas del contrato bridge-v1 §6 por HTTP (R-05): páginas con <c>limit</c> y <c>cursor</c>, nombres en
/// snake_case y el <c>correlation_id</c> del flujo (CT-31). Los campos de `1.1` son opcionales; sin
/// <c>id_erp</c> no se puede sincronizar y la lectura falla con su motivo.
/// </summary>
public sealed class BridgeLecturasHttp(HttpClient http, ICorrelationContext correlacion) : IBridgeLecturas
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private sealed record Pagina<T>(List<T> Items, string? NextCursor);

    private sealed record Clasificacion(string Codigo, string Nombre);

    private sealed record Producto(string Codigo, string Nombre, string UnidadBase, bool LlevaLote, bool Activo, long? IdErp, Clasificacion? Clasificacion);

    private sealed record Domicilio(
        long IdErp, string Tipo, string? Calle, string? NumeroExterior, string? NumeroInterior, string? Colonia, string? CodigoPostal,
        string? Ciudad, string? Municipio, string? Estado, string? Pais, string? Sucursal);

    private sealed record Cliente(string Codigo, string RazonSocial, string? Rfc, long? IdErp, bool? Activo, string? Moneda, List<Domicilio>? Domicilios);

    private sealed record Agente(string Codigo, string Nombre, long IdErp, string? Tipo);

    private sealed record Almacen(string Codigo, string Nombre, long? IdErp);

    public async Task<PaginaLectura<ProductoLeido>> ProductosAsync(int limite, string? cursor, CancellationToken cancellationToken = default)
    {
        var p = await LeerAsync<Pagina<Producto>>(Ruta("catalogs/products", limite, cursor), cancellationToken);
        return new(p.Items.Select(x => new ProductoLeido(
            new DatosErpProducto(Exigir(x.IdErp, "productos", x.Codigo), x.Codigo, x.Nombre, x.UnidadBase, x.LlevaLote, x.Activo),
            x.Clasificacion is null ? null : new ClasificacionLeida(x.Clasificacion.Codigo, x.Clasificacion.Nombre))).ToList(), p.NextCursor);
    }

    public async Task<PaginaLectura<DatosErpCliente>> ClientesAsync(int limite, string? cursor, CancellationToken cancellationToken = default)
    {
        var p = await LeerAsync<Pagina<Cliente>>(Ruta("catalogs/clients", limite, cursor), cancellationToken);
        return new(p.Items.Select(x => new DatosErpCliente(
            Exigir(x.IdErp, "clientes", x.Codigo), x.Codigo, x.RazonSocial, string.IsNullOrWhiteSpace(x.Rfc) ? null : x.Rfc, x.Activo ?? true, x.Moneda,
            x.Domicilios?.Select(d => new DatosDomicilio(d.IdErp, d.Tipo == "fiscal" ? TipoDomicilio.Fiscal : TipoDomicilio.Envio,
                d.Calle, d.NumeroExterior, d.NumeroInterior, d.Colonia, d.CodigoPostal, d.Ciudad, d.Municipio, d.Estado, d.Pais, d.Sucursal)).ToList()))
            .ToList(), p.NextCursor);
    }

    public async Task<PaginaLectura<AgenteLeido>> AgentesAsync(int limite, string? cursor, CancellationToken cancellationToken = default)
    {
        var p = await LeerAsync<Pagina<Agente>>(Ruta("catalogs/agents", limite, cursor), cancellationToken);
        return new(p.Items.Select(x => new AgenteLeido(x.IdErp, x.Codigo, x.Nombre, x.Tipo switch
        {
            "cobro" => TipoAgente.Cobro,
            "venta_cobro" => TipoAgente.VentaCobro,
            _ => TipoAgente.Venta,
        })).ToList(), p.NextCursor);
    }

    public async Task<IReadOnlyList<AlmacenLeido>> AlmacenesAsync(CancellationToken cancellationToken = default) =>
        (await LeerAsync<List<Almacen>>("/api/v1/catalogs/warehouses", cancellationToken))
        .Select(a => new AlmacenLeido(Exigir(a.IdErp, "almacenes", a.Codigo), a.Codigo, a.Nombre)).ToList();

    private static string Ruta(string lectura, int limite, string? cursor) =>
        $"/api/v1/{lectura}?limit={limite.ToString(CultureInfo.InvariantCulture)}" + (cursor is null ? string.Empty : "&cursor=" + Uri.EscapeDataString(cursor));

    private static long Exigir(long? idErp, string lectura, string codigo) =>
        idErp ?? throw new LecturaBridgeException($"La lectura de {lectura} no trae id_erp para {codigo} (contrato 1.1).");

    private async Task<T> LeerAsync<T>(string ruta, CancellationToken ct)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, ruta);
        peticion.Headers.TryAddWithoutValidation("X-Correlation-ID", correlacion.CorrelationId);
        try
        {
            using var r = await http.SendAsync(peticion, ct);
            if (!r.IsSuccessStatusCode)
            {
                var texto = await r.Content.ReadAsStringAsync(ct);
                throw new LecturaBridgeException($"El bridge respondió {(int)r.StatusCode} a {ruta}: {(texto.Length > 300 ? texto[..300] : texto)}");
            }
            return await r.Content.ReadFromJsonAsync<T>(Json, ct)
                ?? throw new LecturaBridgeException($"El bridge respondió vacío a {ruta}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new LecturaBridgeException($"No se pudo leer {ruta} del bridge: {ex.Message}", ex);
        }
    }
}
