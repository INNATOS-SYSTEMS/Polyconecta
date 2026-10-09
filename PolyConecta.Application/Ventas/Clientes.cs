using PolyConecta.Application.Common;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Application.Ventas;

public sealed record DomicilioDto(
    long Id, string Tipo, string Texto, string? Calle, string? NumeroExterior, string? NumeroInterior, string? Colonia,
    string? CodigoPostal, string? Ciudad, string? Municipio, string? Estado, string? Pais, string? Sucursal)
{
    public static DomicilioDto De(CustomerAddress d) => new(d.Id, d.Kind.ToString(), d.Texto, d.Street, d.ExteriorNumber, d.InteriorNumber,
        d.Neighborhood, d.PostalCode, d.City, d.Municipality, d.State, d.Country, d.Branch);
}

public sealed record ClienteDetalle(long Id, string Clave, string RazonSocial, string Etiqueta, string? Rfc, string? Moneda, bool Activo,
    IReadOnlyList<DomicilioDto> Domicilios);

public sealed record DomicilioEnvio(long Id, string Texto);

/// <summary>Para el selector del pedido: propone su moneda y su domicilio de envío (D-146, D-149).</summary>
public sealed record ClienteBusqueda(long Id, string Clave, string Nombre, string Etiqueta, string? Moneda, IReadOnlyList<DomicilioEnvio> DomiciliosEnvio);

public sealed record AgenteDto(long Id, string Clave, string Nombre, string Etiqueta, string Tipo);

public sealed record ObtenerCliente(long Id) : IRequierePermiso
{
    public string Permiso => Permisos.ClienteLeer;
}

public sealed record BuscarClientes(string? Texto) : IRequierePermiso
{
    public string Permiso => Permisos.PedidoCrear;
}

public sealed record ListarAgentes : IRequierePermiso
{
    public string Permiso => Permisos.AgenteLeer;
}

public sealed class ObtenerClienteCaso(IAlmacen<Customer> clientes) : IUseCase<ObtenerCliente, ClienteDetalle>
{
    public async Task<ClienteDetalle> ExecuteAsync(ObtenerCliente request, CancellationToken cancellationToken = default)
    {
        var c = await clientes.ObtenerAsync(request.Id, "el cliente", cancellationToken);
        return new ClienteDetalle(c.Id, c.ErpCode, c.LegalName, c.Etiqueta, c.TaxId, c.Currency, c.IsActive,
            c.Addresses.Where(d => d.IsActive).OrderBy(d => d.Kind).ThenBy(d => d.ErpAddressId).Select(DomicilioDto.De).ToList());
    }
}

public sealed class BuscarClientesCaso(IAlmacen<Customer> clientes) : IUseCase<BuscarClientes, IReadOnlyList<ClienteBusqueda>>
{
    public async Task<IReadOnlyList<ClienteBusqueda>> ExecuteAsync(BuscarClientes request, CancellationToken cancellationToken = default)
    {
        var t = (request.Texto ?? string.Empty).Trim();
        var encontrados = await clientes.PrimerosAsync(c => t == "" || c.ErpCode.Contains(t) || c.LegalName.Contains(t), c => c.ErpCode, 20, cancellationToken);
        return encontrados.Select(c => new ClienteBusqueda(c.Id, c.ErpCode, c.LegalName, c.Etiqueta, c.Currency,
            c.DomiciliosDeEnvio.OrderBy(d => d.ErpAddressId).Select(d => new DomicilioEnvio(d.Id, d.Texto)).ToList())).ToList();
    }
}

public sealed class ListarAgentesCaso(IAlmacen<ErpAgent> agentes) : IUseCase<ListarAgentes, IReadOnlyList<AgenteDto>>
{
    public async Task<IReadOnlyList<AgenteDto>> ExecuteAsync(ListarAgentes request, CancellationToken cancellationToken = default) =>
        (await agentes.ListarAsync(cancellationToken: cancellationToken)).OrderBy(a => a.ErpCode)
            .Select(a => new AgenteDto(a.Id, a.ErpCode, a.Name, a.Etiqueta, a.Kind.ToString())).ToList();
}
