using PolyConecta.Domain.Common;

namespace PolyConecta.Domain.Ventas;

public enum TipoAgente
{
    Venta,
    VentaCobro,
    Cobro,
}

/// <summary>Agente de CONTPAQi sincronizado de <c>admAgentes</c> (D-153, <c>ven.erp_agent</c>). No se edita.</summary>
public sealed class ErpAgent : ArchivableEntity
{
    public long ErpAgentId { get; private set; }

    public string ErpCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public TipoAgente Kind { get; private set; }

    public string Etiqueta => $"{ErpCode} - {Name}";

    private ErpAgent() { }

    public ErpAgent(long erpAgentId, string erpCode, string name, TipoAgente kind)
    {
        ErpAgentId = erpAgentId;
        ActualizarDesdeErp(erpCode, name, kind);
    }

    public bool ActualizarDesdeErp(string erpCode, string name, TipoAgente kind)
    {
        if (ErpCode == erpCode && Name == name && Kind == kind) return false;
        ErpCode = erpCode;
        Name = name;
        Kind = kind;
        return true;
    }
}

public enum TipoDomicilio
{
    Fiscal,
    Envio,
}

/// <summary>Un domicilio de <c>admDomicilios</c> como lo trae la lectura `1.1` (D-149, D-150).</summary>
public sealed record DatosDomicilio(
    long IdErp, TipoDomicilio Tipo, string? Calle, string? NumeroExterior, string? NumeroInterior, string? Colonia,
    string? CodigoPostal, string? Ciudad, string? Municipio, string? Estado, string? Pais, string? Sucursal);

/// <summary>Lo que trae la lectura de clientes del bridge (contrato §6, `1.1`).</summary>
public sealed record DatosErpCliente(
    long IdErp, string Codigo, string RazonSocial, string? Rfc, bool Activo, string? Moneda, IReadOnlyList<DatosDomicilio>? Domicilios);

/// <summary>
/// Cliente sincronizado de CONTPAQi (<c>ven.customer</c>), de solo lectura (CT-14), con su moneda (D-146,
/// D-150) y sus domicilios: uno fiscal y N de envío (D-149). Inactivo en CONTPAQi se archiva.
/// </summary>
public sealed class Customer : ArchivableEntity
{
    private readonly List<CustomerAddress> _domicilios = [];

    public long ErpCustomerId { get; private set; }

    public string ErpCode { get; private set; } = string.Empty;

    public string LegalName { get; private set; } = string.Empty;

    public string? TaxId { get; private set; }

    /// <summary>ISO, de CIDMONEDA; sin dato, el pedido propone la moneda base.</summary>
    public string? Currency { get; private set; }

    public IReadOnlyList<CustomerAddress> Addresses => _domicilios;

    public string Etiqueta => $"{ErpCode} - {LegalName}";

    /// <summary>Domicilios de envío vigentes: entre ellos elige el pedido (D-149).</summary>
    public IEnumerable<CustomerAddress> DomiciliosDeEnvio => _domicilios.Where(d => d.IsActive && d.Kind == TipoDomicilio.Envio);

    private Customer() { }

    public static Customer DesdeErp(DatosErpCliente datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        var c = new Customer { ErpCustomerId = datos.IdErp };
        c.ActualizarDesdeErp(datos);
        return c;
    }

    /// <summary>Copia lo de CONTPAQi y reemplaza sus domicilios. Devuelve si algo cambió.</summary>
    public bool ActualizarDesdeErp(DatosErpCliente datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        var moneda = string.IsNullOrWhiteSpace(datos.Moneda) ? null : datos.Moneda.Trim().ToUpperInvariant();
        var cambio = ErpCode != datos.Codigo || LegalName != datos.RazonSocial || TaxId != datos.Rfc || Currency != moneda;
        ErpCode = datos.Codigo;
        LegalName = datos.RazonSocial;
        TaxId = datos.Rfc;
        Currency = moneda;
        // Sin `domicilios` (contrato 1.0) no se tocan los que hay.
        if (datos.Domicilios is not null) cambio |= ReemplazarDomicilios(datos.Domicilios);
        return cambio;
    }

    /// <summary>Agrega, cambia y archiva los que ya no vienen; restaura los que regresan (data-model §4).</summary>
    private bool ReemplazarDomicilios(IReadOnlyList<DatosDomicilio> domicilios)
    {
        var cambio = false;
        foreach (var d in domicilios)
        {
            var actual = _domicilios.FirstOrDefault(x => x.ErpAddressId == d.IdErp);
            if (actual is null)
            {
                _domicilios.Add(new CustomerAddress(d));
                cambio = true;
                continue;
            }
            cambio |= actual.Actualizar(d);
            if (!actual.IsActive)
            {
                actual.Restore();
                cambio = true;
            }
        }
        foreach (var sobrante in _domicilios.Where(x => x.IsActive && domicilios.All(d => d.IdErp != x.ErpAddressId)))
        {
            sobrante.Archive();
            cambio = true;
        }
        return cambio;
    }

    public bool ArchivarPorErp()
    {
        if (!IsActive) return false;
        Archive();
        return true;
    }

    public bool RestaurarPorErp()
    {
        if (IsActive) return false;
        Restore();
        return true;
    }
}

/// <summary>Domicilio del cliente (<c>ven.customer_address</c>), sincronizado (D-149).</summary>
public sealed class CustomerAddress : ArchivableEntity
{
    public long CustomerId { get; private set; }

    /// <summary>CIDDIRECCION.</summary>
    public long ErpAddressId { get; private set; }

    public TipoDomicilio Kind { get; private set; }

    public string? Street { get; private set; }

    public string? ExteriorNumber { get; private set; }

    public string? InteriorNumber { get; private set; }

    public string? Neighborhood { get; private set; }

    public string? PostalCode { get; private set; }

    public string? City { get; private set; }

    public string? Municipality { get; private set; }

    public string? State { get; private set; }

    public string? Country { get; private set; }

    public string? Branch { get; private set; }

    private CustomerAddress() { }

    internal CustomerAddress(DatosDomicilio d)
    {
        ErpAddressId = d.IdErp;
        Actualizar(d);
    }

    internal bool Actualizar(DatosDomicilio d)
    {
        static string? N(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        var nuevos = (d.Tipo, N(d.Calle), N(d.NumeroExterior), N(d.NumeroInterior), N(d.Colonia), N(d.CodigoPostal), N(d.Ciudad),
            N(d.Municipio), N(d.Estado), N(d.Pais), N(d.Sucursal));
        var actuales = (Kind, Street, ExteriorNumber, InteriorNumber, Neighborhood, PostalCode, City, Municipality, State, Country, Branch);
        if (nuevos == actuales) return false;
        (Kind, Street, ExteriorNumber, InteriorNumber, Neighborhood, PostalCode, City, Municipality, State, Country, Branch) = nuevos;
        return true;
    }

    /// <summary>Una línea legible: "Planta Norte · Carretera Miguel Alemán Km 18 Nave 4, Centro, Apodaca, Nuevo León, CP 66600".</summary>
    public string Texto
    {
        get
        {
            var calle = string.Join(" ", new[] { Street, ExteriorNumber, InteriorNumber }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var partes = new[] { calle, Neighborhood, City ?? Municipality, State, PostalCode is null ? null : "CP " + PostalCode }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var texto = string.Join(", ", partes);
            return string.IsNullOrWhiteSpace(Branch) ? texto : $"{Branch} · {texto}";
        }
    }
}
