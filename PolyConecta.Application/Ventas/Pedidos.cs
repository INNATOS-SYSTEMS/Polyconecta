using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Folios;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Application.Ventas;

/// <summary>Monedas admitidas y la base, de la configuración del ERP (D-146). Lo implementa Infrastructure.</summary>
public interface IConfiguracionDeMonedas
{
    ReglasDeMoneda Monedas { get; }
}

public sealed record LineaPedidoDto(long? Id, long ProductoId, decimal Cantidad, decimal? PrecioUnitario, decimal? MetaProduccionKg, decimal? ToleranciaPorcentaje);

/// <summary>Cuerpo de crear y editar (contracts/api-f1.md, Pedidos). La unidad no viaja: es la base del producto (D-127).</summary>
public sealed record DatosPedidoDto(
    long ClienteId, string? OrdenCompraCliente, long? AgenteId, DateOnly? FechaPedido, DateOnly? FechaPromesa, long? DomicilioEntregaId,
    string? Moneda, decimal? TipoCambio, IReadOnlyList<LineaPedidoDto>? Lineas);

public sealed record Referencia(long Id, string Clave, string Nombre);

public sealed record LineaDetalle(
    long Id, long ProductoId, string Clave, string Producto, decimal Cantidad, string Unidad, decimal? PrecioUnitario,
    decimal? Subtotal, decimal? MetaProduccionKg, decimal? ToleranciaPorcentaje);

public sealed record FirmaDetalle(string Rol, string Usuario, long UsuarioId, string? Grupo, bool Suplente, DateTimeOffset Fecha);

public sealed record PedidoDetalle(
    long Id, string Folio, string Estado, byte[] RowVersion, Referencia Cliente, string? OrdenCompraCliente, Referencia? Agente,
    DateOnly FechaPedido, DateOnly? FechaPromesa, string Moneda, decimal? TipoCambio, string MonedaBase,
    DomicilioEnvio? DomicilioEntrega, IReadOnlyList<LineaDetalle> Lineas, IReadOnlyList<FirmaDetalle> Firmas,
    IReadOnlyList<string> FirmasPendientes, IReadOnlyList<string> RolesPorFirmar, object Sincronizacion,
    IReadOnlyList<AccionDisponible> Acciones);

public sealed record ObtenerPedido(long Id) : IRequierePermiso
{
    public string Permiso => Permisos.PedidoLeer;
}

public sealed record CrearPedido(DatosPedidoDto Datos) : IRequierePermiso
{
    public string Permiso => Permisos.PedidoCrear;
}

public sealed record EditarPedido(long Id, byte[] RowVersion, bool RevocarAutorizacion, DatosPedidoDto Datos) : IRequierePermiso
{
    public string Permiso => Permisos.PedidoEditar;
}

public sealed record ConfirmarPedido(long Id, byte[] RowVersion) : IRequierePermiso
{
    public string Permiso => Permisos.PedidoConfirmar;
}

/// <summary><see cref="Rol"/> solo si el usuario puede firmar por los dos y ninguno ha firmado (contracts/api-f1.md).</summary>
public sealed record AutorizarPedido(long Id, byte[] RowVersion, RolFirma? Rol) : IRequiereAlgunPermiso
{
    public IReadOnlyList<string> Permisos => [Domain.Plataforma.Seguridad.Permisos.PedidoFirmarComercial, Domain.Plataforma.Seguridad.Permisos.PedidoFirmarCobranza];
}

public sealed record RevocarAutorizacion(long Id, byte[] RowVersion, string? Motivo) : IRequierePermiso
{
    public string Permiso => Permisos.PedidoRevocar;
}

public sealed record CancelarPedido(long Id, byte[] RowVersion, string? Motivo) : IRequierePermiso
{
    public string Permiso => Permisos.PedidoCancelar;
}

public sealed class ValidarDatosPedido : IValidator<CrearPedido>, IValidator<EditarPedido>
{
    public IEnumerable<ErrorValidacion> Validate(CrearPedido request) => Validar(request.Datos);

    public IEnumerable<ErrorValidacion> Validate(EditarPedido request)
    {
        if (request.RowVersion is not { Length: > 0 }) yield return new("rowVersion", "Falta la versión del pedido.");
        foreach (var e in Validar(request.Datos)) yield return e;
    }

    private static IEnumerable<ErrorValidacion> Validar(DatosPedidoDto? d)
    {
        if (d is null)
        {
            yield return new("datos", "Faltan los datos del pedido.");
            yield break;
        }
        if (d.ClienteId <= 0) yield return new("clienteId", "Elige el cliente.");
        if (string.IsNullOrWhiteSpace(d.Moneda)) yield return new("moneda", "Falta la moneda.");
        if (d.OrdenCompraCliente?.Length > 60) yield return new("ordenCompraCliente", "La orden de compra tiene más de 60 caracteres.");
        foreach (var (l, i) in (d.Lineas ?? []).Select((l, i) => (l, i)))
            if (l.ProductoId <= 0) yield return new($"lineas[{i}].productoId", $"Elige el producto de la línea {i + 1}.");
    }
}

/// <summary>Lo común de los casos de uso del pedido: arma los datos del dominio y el detalle con sus acciones.</summary>
public sealed class ServicioDePedidos(
    IAlmacen<SalesOrder> pedidos,
    IAlmacen<Customer> clientes,
    IAlmacen<Product> productos,
    IAlmacen<ErpAgent> agentes,
    IAlmacen<User> usuarios,
    IPermisosDelUsuario permisos,
    ICurrentUser usuario,
    IConfiguracionDeMonedas monedas,
    IClock clock)
{
    public ReglasDeMoneda Monedas => monedas.Monedas;

    public Task<SalesOrder> ObtenerAsync(long id, CancellationToken ct) => pedidos.ObtenerAsync(id, "el pedido", ct);

    public void ExigirVersion(SalesOrder pedido, byte[] rowVersion)
    {
        if (rowVersion is not { Length: > 0 }) throw new ValidacionException([new ErrorValidacion("rowVersion", "Falta la versión del pedido.")]);
        pedidos.ExigirVersion(pedido, rowVersion);
    }

    public Actor Actor() => new(usuario.UserId ?? 0, usuario.NombreVisible, usuario.GrupoEjercido, usuario.EsSuplente);

    public async Task<DatosPedido> DatosAsync(DatosPedidoDto d, bool proponerAgente, CancellationToken ct)
    {
        var cliente = await clientes.PorIdAsync(d.ClienteId, incluirArchivados: true, ct)
            ?? throw new ValidacionException([new ErrorValidacion("clienteId", "El cliente no existe.")]);
        var lineas = d.Lineas ?? [];
        var ids = lineas.Select(l => l.ProductoId).Distinct().ToList();
        var encontrados = (await productos.ListarAsync(p => ids.Contains(p.Id), incluirArchivados: true, ct)).ToDictionary(p => p.Id);
        var faltan = lineas.Select((l, i) => (l, i)).Where(x => !encontrados.ContainsKey(x.l.ProductoId))
            .Select(x => new ErrorValidacion($"lineas[{x.i}].productoId", "El producto no existe.")).ToList();
        if (faltan.Count > 0) throw new ValidacionException(faltan);

        var agenteId = d.AgenteId;
        // D-153: sin agente, se propone el del usuario que captura.
        if (agenteId is null && proponerAgente && usuario.UserId is { } uid)
            agenteId = (await usuarios.PorIdAsync(uid, cancellationToken: ct))?.ErpAgentId;
        ErpAgent? agente = null;
        if (agenteId is { } aid)
            agente = await agentes.PorIdAsync(aid, incluirArchivados: true, ct)
                ?? throw new ValidacionException([new ErrorValidacion("agenteId", "El agente no existe.")]);

        return new DatosPedido(cliente, d.OrdenCompraCliente, agente, d.FechaPedido ?? DateOnly.FromDateTime(clock.Now.UtcDateTime),
            d.FechaPromesa, d.DomicilioEntregaId, d.Moneda ?? Monedas.Base, d.TipoCambio,
            lineas.Select(l => new LineaSolicitada(l.Id, encontrados[l.ProductoId], l.Cantidad, l.PrecioUnitario, l.MetaProduccionKg, l.ToleranciaPorcentaje)).ToList());
    }

    /// <summary>El usuario es "el Administrador" de D-33 si tiene el grupo Administrador con el permiso de revocar.</summary>
    public async Task<bool> EsAdministradorAsync(CancellationToken ct) =>
        (await permisos.AsignacionesAsync(ct)).Any(a => a.GrupoCodigo == GruposIniciales.Administrador && a.Permisos.Contains(Permisos.PedidoRevocar));

    public async Task<PedidoDetalle> DetalleAsync(SalesOrder p, CancellationToken ct)
    {
        var efectivos = (await permisos.AsignacionesAsync(ct)).SelectMany(a => a.Permisos).ToHashSet();
        bool Tiene(string clave) => efectivos.Contains(clave);
        var agente = p.AgentId is { } aid ? await agentes.PorIdAsync(aid, incluirArchivados: true, ct) : null;
        var domicilio = p.DeliveryAddressId is { } did ? p.Customer.Addresses.FirstOrDefault(a => a.Id == did) : null;
        var userId = usuario.UserId ?? 0;
        return new PedidoDetalle(
            p.Id, p.Name, SalesOrder.Etiqueta(p.State), p.RowVersion,
            new Referencia(p.Customer.Id, p.Customer.ErpCode, p.Customer.LegalName), p.CustomerPo,
            agente is null ? null : new Referencia(agente.Id, agente.ErpCode, agente.Name),
            p.OrderDate, p.PromiseDate, p.Currency, p.ExchangeRate, Monedas.Base,
            domicilio is null && p.DeliveryAddressText is null ? null : new DomicilioEnvio(p.DeliveryAddressId ?? 0, p.DeliveryAddressText ?? domicilio!.Texto),
            p.Lines.OrderBy(l => l.Sequence).Select(l => new LineaDetalle(l.Id, l.ProductId, l.Product.ErpCode, l.Product.Etiqueta, l.RequestedQty,
                l.Product.PackagingUnits.FirstOrDefault(u => u.Id == l.RequestedPackagingUnitId)?.Code ?? l.Product.ErpUom,
                l.UnitPrice, l.UnitPrice is { } precio ? Math.Round(precio * l.RequestedQty, 2) : null, l.TargetProductionKg, l.TolerancePercentageOverride)).ToList(),
            p.Signatures.OrderBy(f => f.SignedAt).Select(f => new FirmaDetalle(f.Role.ToString(), f.UserName, f.UserId, f.GroupExercised, f.IsSubstitute, f.SignedAt)).ToList(),
            Enum.GetValues<RolFirma>().Where(r => p.Signatures.All(f => f.Role != r)).Select(r => r.ToString()).ToList(),
            p.State == SalesOrderState.Confirmado && p.Signatures.All(f => f.UserId != userId)
                ? p.RolesPorFirmar(Tiene).Select(r => r.ToString()).ToList() : [],
            new { estado = p.Sync.Status.ToString() },
            p.AccionesDisponibles(userId, Tiene, await EsAdministradorAsync(ct), Monedas)
                .Select(a => new AccionDisponible(a.Accion, a.Disponible, a.Razon, a.Aviso)).ToList());
    }
}

public sealed class ObtenerPedidoCaso(ServicioDePedidos servicio) : IUseCase<ObtenerPedido, PedidoDetalle>
{
    public async Task<PedidoDetalle> ExecuteAsync(ObtenerPedido request, CancellationToken cancellationToken = default) =>
        await servicio.DetalleAsync(await servicio.ObtenerAsync(request.Id, cancellationToken), cancellationToken);
}

/// <summary>"Nuevo": maestro y líneas en un solo guardado, en Borrador, con folio PV (FR-019, FR-021, D-136).</summary>
public sealed class CrearPedidoCaso(ServicioDePedidos servicio, IAlmacen<SalesOrder> pedidos, IReferenceSequenceService folios, IUnitOfWork uow)
    : IUseCase<CrearPedido, PedidoDetalle>
{
    public async Task<PedidoDetalle> ExecuteAsync(CrearPedido request, CancellationToken cancellationToken = default)
    {
        var datos = await servicio.DatosAsync(request.Datos, proponerAgente: true, cancellationToken);
        var pedido = SalesOrder.Crear(await folios.NextAsync(SalesOrder.TipoDocumento, cancellationToken: cancellationToken), datos, servicio.Monedas);
        pedidos.Agregar(pedido);
        await uow.SaveChangesAsync(cancellationToken);
        return await servicio.DetalleAsync(pedido, cancellationToken);
    }
}

/// <summary>Editar (FR-024, D-147): con firmas exige <c>revocarAutorizacion</c> y revoca en la misma transacción.</summary>
public sealed class EditarPedidoCaso(ServicioDePedidos servicio, IUnitOfWork uow) : IUseCase<EditarPedido, PedidoDetalle>
{
    public async Task<PedidoDetalle> ExecuteAsync(EditarPedido request, CancellationToken cancellationToken = default)
    {
        var pedido = await servicio.ObtenerAsync(request.Id, cancellationToken);
        servicio.ExigirVersion(pedido, request.RowVersion);
        pedido.Editar(await servicio.DatosAsync(request.Datos, proponerAgente: false, cancellationToken), request.RevocarAutorizacion, servicio.Monedas);
        await uow.SaveChangesAsync(cancellationToken);
        return await servicio.DetalleAsync(pedido, cancellationToken);
    }
}

public sealed class ConfirmarPedidoCaso(ServicioDePedidos servicio, IUnitOfWork uow) : IUseCase<ConfirmarPedido, PedidoDetalle>
{
    public async Task<PedidoDetalle> ExecuteAsync(ConfirmarPedido request, CancellationToken cancellationToken = default)
    {
        var pedido = await servicio.ObtenerAsync(request.Id, cancellationToken);
        servicio.ExigirVersion(pedido, request.RowVersion);
        pedido.Confirmar(servicio.Monedas);
        await uow.SaveChangesAsync(cancellationToken);
        return await servicio.DetalleAsync(pedido, cancellationToken);
    }
}

/// <summary>
/// "Autorizar" (FR-023): un solo botón para Comercial y Cobranza. Firma con el rol que le queda al
/// usuario; si puede por los dos y ninguno ha firmado, el rol lo elige él. El grupo ejercido es el que
/// le da el permiso de ese rol (titular antes que suplente, D-38).
/// </summary>
public sealed class AutorizarPedidoCaso(ServicioDePedidos servicio, IPermisosDelUsuario permisos, Autorizacion autorizacion, ICurrentUser usuario, IClock clock, IUnitOfWork uow)
    : IUseCase<AutorizarPedido, PedidoDetalle>
{
    public async Task<PedidoDetalle> ExecuteAsync(AutorizarPedido request, CancellationToken cancellationToken = default)
    {
        var pedido = await servicio.ObtenerAsync(request.Id, cancellationToken);
        servicio.ExigirVersion(pedido, request.RowVersion);
        var efectivos = (await permisos.AsignacionesAsync(cancellationToken)).SelectMany(a => a.Permisos).ToHashSet();
        var posibles = pedido.RolesPorFirmar(efectivos.Contains);

        RolFirma rol;
        if (request.Rol is { } pedido_)
        {
            if (!posibles.Contains(pedido_))
                throw PermisoOFirma(pedido, pedido_, efectivos);
            rol = pedido_;
        }
        else if (posibles.Count == 1) rol = posibles[0];
        else if (posibles.Count == 0)
            throw pedido.Signatures.Any(f => f.UserId == usuario.UserId)
                ? new ReglaDeNegocioException("TRANSICION_INVALIDA", SalesOrder.RazonFirmaDoble)
                : new ReglaDeNegocioException("TRANSICION_INVALIDA", $"Ya está firmado por {string.Join(" y ", pedido.Signatures.Select(f => f.Role))}.");
        else throw new ValidacionException([new ErrorValidacion("rol", "Puedes firmar como Comercial o como Cobranza: elige con qué rol firmas.")]);

        var permiso = rol == RolFirma.Comercial ? Permisos.PedidoFirmarComercial : Permisos.PedidoFirmarCobranza;
        var grupo = await autorizacion.ResolverAsync(permiso, null, cancellationToken) ?? throw PermisoDenegadoException.Para(permiso);
        usuario.EjercerGrupo(grupo.Nombre, grupo.EsSuplente);
        pedido.Firmar(servicio.Actor(), rol, clock.Now);
        await uow.SaveChangesAsync(cancellationToken);
        return await servicio.DetalleAsync(pedido, cancellationToken);
    }

    private static Exception PermisoOFirma(SalesOrder pedido, RolFirma rol, HashSet<string> efectivos)
    {
        var permiso = rol == RolFirma.Comercial ? Permisos.PedidoFirmarComercial : Permisos.PedidoFirmarCobranza;
        return !efectivos.Contains(permiso)
            ? PermisoDenegadoException.Para(permiso)
            : new ReglaDeNegocioException("TRANSICION_INVALIDA", $"Ya está firmado por {rol}.");
    }
}

public sealed class RevocarAutorizacionCaso(ServicioDePedidos servicio, IUnitOfWork uow) : IUseCase<RevocarAutorizacion, PedidoDetalle>
{
    public async Task<PedidoDetalle> ExecuteAsync(RevocarAutorizacion request, CancellationToken cancellationToken = default)
    {
        var pedido = await servicio.ObtenerAsync(request.Id, cancellationToken);
        servicio.ExigirVersion(pedido, request.RowVersion);
        pedido.Revocar(servicio.Actor(), request.Motivo ?? string.Empty, await servicio.EsAdministradorAsync(cancellationToken));
        await uow.SaveChangesAsync(cancellationToken);
        return await servicio.DetalleAsync(pedido, cancellationToken);
    }
}

public sealed class CancelarPedidoCaso(ServicioDePedidos servicio, IUnitOfWork uow) : IUseCase<CancelarPedido, PedidoDetalle>
{
    public async Task<PedidoDetalle> ExecuteAsync(CancelarPedido request, CancellationToken cancellationToken = default)
    {
        var pedido = await servicio.ObtenerAsync(request.Id, cancellationToken);
        servicio.ExigirVersion(pedido, request.RowVersion);
        pedido.Cancelar(request.Motivo ?? string.Empty);
        await uow.SaveChangesAsync(cancellationToken);
        return await servicio.DetalleAsync(pedido, cancellationToken);
    }
}
