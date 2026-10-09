using PolyConecta.Application.Common;
using PolyConecta.Application.Plataforma.Chatter;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma.Seguridad;

namespace PolyConecta.Application.Inventario;

public sealed record FichaTecnicaDto(DatosRollo Rollo, DatosPt Pt, long? RolloLigadoProductoId, string? RolloLigadoProducto);

public sealed record ProductoDetalle(
    long Id, string Clave, string Nombre, string Etiqueta, string Unidad, bool LlevaLote, bool Activo,
    long? ClasificacionId, string? Clasificacion, FichaTecnicaDto? Ficha, IReadOnlyList<AccionDisponible> Acciones);

public sealed record ProductoBusqueda(long Id, string Clave, string Nombre, string Etiqueta, string Unidad, long UnidadId, bool LlevaLote);

public sealed record ClasificacionDto(long Id, string Codigo, string Nombre, string? ValorErp);

public sealed record AlmacenDto(long Id, string Clave, string Nombre, long IdErp);

public sealed record ObtenerProducto(long Id) : IRequierePermiso
{
    public string Permiso => Permisos.ProductoLeer;
}

public sealed record BuscarProductos(string? Texto) : IRequierePermiso
{
    public string Permiso => Permisos.PedidoCrear;
}

public sealed record ClasificarProducto(long Id, long? ClasificacionId) : IRequierePermiso
{
    public string Permiso => Permisos.ProductoClasificar;
}

/// <summary>Los dos bloques siempre (FR-018). <c>RolloLigadoProductoId</c>: el rollo de otro producto de 2.º proceso.</summary>
public sealed record GuardarFichaTecnica(long Id, DatosRollo Rollo, DatosPt Pt, long? RolloLigadoProductoId) : IRequierePermiso
{
    public string Permiso => Permisos.FichaEditar;
}

public sealed record ListarClasificaciones : IRequierePermiso
{
    public string Permiso => Permisos.ProductoLeer;
}

public sealed record GuardarClasificacion(long? Id, string Codigo, string Nombre) : IRequierePermiso
{
    public string Permiso => Permisos.ProductoClasificar;
}

public sealed record ListarAlmacenes : IRequierePermiso
{
    public string Permiso => Permisos.AlmacenLeer;
}

public sealed class DetalleDeProducto(IAlmacen<Product> productos, IAlmacen<ProductClassification> clasificaciones, Autorizacion autorizacion)
{
    public async Task<ProductoDetalle> ArmarAsync(Product p, CancellationToken ct)
    {
        var clasificacion = p.ClassificationId is { } c ? await clasificaciones.PorIdAsync(c, true, ct) : null;
        FichaTecnicaDto? ficha = null;
        if (p.Roll is not null && p.Pt is not null)
        {
            var ligadoId = p.Pt.RelatedRoll.ProductId;
            var ligado = ligadoId == p.Id ? null : await productos.PorIdAsync(ligadoId, true, ct);
            ficha = new FichaTecnicaDto(p.Roll.Datos(), p.Pt.Datos(), ligado?.Id, ligado?.Etiqueta);
        }
        var acciones = new List<AccionDisponible>
        {
            await AccionAsync("clasificar", Permisos.ProductoClasificar, ct),
            await AccionAsync("editar_ficha", Permisos.FichaEditar, ct),
        };
        return new ProductoDetalle(p.Id, p.ErpCode, p.Name, p.Etiqueta, p.ErpUom, p.TracksLots, p.IsActive,
            p.ClassificationId, clasificacion?.Name, ficha, acciones);
    }

    private async Task<AccionDisponible> AccionAsync(string accion, string permiso, CancellationToken ct) =>
        await autorizacion.PuedeAsync(permiso, null, ct) ? AccionDisponible.Si(accion) : AccionDisponible.No(accion, PermisoDenegadoException.Para(permiso).Razon);
}

public sealed class ObtenerProductoCaso(IAlmacen<Product> productos, DetalleDeProducto detalle) : IUseCase<ObtenerProducto, ProductoDetalle>
{
    public async Task<ProductoDetalle> ExecuteAsync(ObtenerProducto request, CancellationToken cancellationToken = default) =>
        await detalle.ArmarAsync(await productos.ObtenerAsync(request.Id, "el producto", cancellationToken), cancellationToken);
}

/// <summary>Para la captura de líneas: activos, hasta 20, con su unidad base (D-127).</summary>
public sealed class BuscarProductosCaso(IAlmacen<Product> productos) : IUseCase<BuscarProductos, IReadOnlyList<ProductoBusqueda>>
{
    public async Task<IReadOnlyList<ProductoBusqueda>> ExecuteAsync(BuscarProductos request, CancellationToken cancellationToken = default)
    {
        var t = (request.Texto ?? string.Empty).Trim();
        var encontrados = await productos.PrimerosAsync(p => t == "" || p.ErpCode.Contains(t) || p.Name.Contains(t), p => p.ErpCode, 20, cancellationToken);
        return encontrados.Select(p => new ProductoBusqueda(p.Id, p.ErpCode, p.Name, p.Etiqueta, p.ErpUom, p.UnidadBase.Id, p.TracksLots)).ToList();
    }
}

/// <summary>La clasificación es de PolyConecta (D-86); una sincronización posterior no la sobrescribe.</summary>
public sealed class ClasificarProductoCaso(IAlmacen<Product> productos, IAlmacen<ProductClassification> clasificaciones, DetalleDeProducto detalle, IUnitOfWork uow)
    : IUseCase<ClasificarProducto, ProductoDetalle>
{
    public async Task<ProductoDetalle> ExecuteAsync(ClasificarProducto request, CancellationToken cancellationToken = default)
    {
        var p = await productos.ObtenerAsync(request.Id, "el producto", cancellationToken);
        if (request.ClasificacionId is { } c && !await clasificaciones.ExisteAsync(x => x.Id == c, cancellationToken: cancellationToken))
            throw new ValidacionException([new ErrorValidacion("clasificacionId", "La clasificación no existe.")]);
        var nombre = async (long? id) => id is { } x ? (await clasificaciones.PorIdAsync(x, true, cancellationToken))?.Name : null;
        p.AnotarCambio(Bitacora.Cambio("Clasificación", await nombre(p.ClassificationId), await nombre(request.ClasificacionId)) ?? string.Empty);
        p.Clasificar(request.ClasificacionId);
        await uow.SaveChangesAsync(cancellationToken);
        return await detalle.ArmarAsync(p, cancellationToken);
    }
}

public sealed class GuardarFichaTecnicaCaso(IAlmacen<Product> productos, DetalleDeProducto detalle, IUnitOfWork uow)
    : IUseCase<GuardarFichaTecnica, ProductoDetalle>
{
    public async Task<ProductoDetalle> ExecuteAsync(GuardarFichaTecnica request, CancellationToken cancellationToken = default)
    {
        var p = await productos.ObtenerAsync(request.Id, "el producto", cancellationToken);
        RollSpecification? ligado = null;
        if (request.RolloLigadoProductoId is { } otroId && otroId != p.Id)
        {
            var otro = await productos.ObtenerAsync(otroId, "el producto del rollo ligado", cancellationToken);
            ligado = otro.Roll ?? throw new ReglaDeNegocioException("FICHA_SIN_ROLLO", $"{otro.Etiqueta} no tiene bloque Rollo en su ficha técnica.");
        }
        var antes = (await detalle.ArmarAsync(p, cancellationToken)).Ficha;
        p.GuardarFichaTecnica(request.Rollo, request.Pt, ligado);
        var despues = (await detalle.ArmarAsync(p, cancellationToken)).Ficha;
        foreach (var c in Bitacora.Propiedades(antes?.Rollo, despues?.Rollo, EtiquetasFicha.Rollo, "Rollo")
                     .Concat(Bitacora.Propiedades(antes?.Pt, despues?.Pt, EtiquetasFicha.Pt, "PT")))
            p.AnotarCambio(c);
        p.AnotarCambio(Bitacora.Cambio("Rollo ligado", antes?.RolloLigadoProducto, despues?.RolloLigadoProducto) ?? string.Empty);
        await uow.SaveChangesAsync(cancellationToken);
        return await detalle.ArmarAsync(p, cancellationToken);
    }
}

public sealed class ListarClasificacionesCaso(IAlmacen<ProductClassification> clasificaciones) : IUseCase<ListarClasificaciones, IReadOnlyList<ClasificacionDto>>
{
    public async Task<IReadOnlyList<ClasificacionDto>> ExecuteAsync(ListarClasificaciones request, CancellationToken cancellationToken = default) =>
        (await clasificaciones.ListarAsync(cancellationToken: cancellationToken)).OrderBy(c => c.Name)
            .Select(c => new ClasificacionDto(c.Id, c.Code, c.Name, c.ErpValue)).ToList();
}

public sealed class GuardarClasificacionCaso(IAlmacen<ProductClassification> clasificaciones, IUnitOfWork uow) : IUseCase<GuardarClasificacion, ClasificacionDto>
{
    public async Task<ClasificacionDto> ExecuteAsync(GuardarClasificacion request, CancellationToken cancellationToken = default)
    {
        var codigo = (request.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        if (await clasificaciones.ExisteAsync(c => c.Code == codigo && c.Id != (request.Id ?? 0), true, cancellationToken))
            throw new ReglaDeNegocioException("CLASIFICACION_DUPLICADA", $"Ya existe la clasificación {codigo}.");
        ProductClassification c;
        if (request.Id is { } id)
        {
            c = await clasificaciones.ObtenerAsync(id, "la clasificación", cancellationToken);
            var (codigoAntes, nombreAntes) = (c.Code, c.Name);
            c.Editar(codigo, request.Nombre);
            c.AnotarCambio(Bitacora.Cambio("Código", codigoAntes, c.Code) ?? string.Empty);
            c.AnotarCambio(Bitacora.Cambio("Nombre", nombreAntes, c.Name) ?? string.Empty);
        }
        else
        {
            c = new ProductClassification(codigo, request.Nombre);
            c.AnotarCambio("Creó la clasificación.");
            clasificaciones.Agregar(c);
        }
        await uow.SaveChangesAsync(cancellationToken);
        return new ClasificacionDto(c.Id, c.Code, c.Name, c.ErpValue);
    }
}

public sealed class ListarAlmacenesCaso(IAlmacen<ErpWarehouse> almacenes) : IUseCase<ListarAlmacenes, IReadOnlyList<AlmacenDto>>
{
    public async Task<IReadOnlyList<AlmacenDto>> ExecuteAsync(ListarAlmacenes request, CancellationToken cancellationToken = default) =>
        (await almacenes.ListarAsync(cancellationToken: cancellationToken)).OrderBy(a => a.ErpCode)
            .Select(a => new AlmacenDto(a.Id, a.ErpCode, a.Name, a.ErpWarehouseId)).ToList();
}

/// <summary>Etiquetas de la ficha técnica en la bitácora, como en el formulario del producto (FR-018).</summary>
public static class EtiquetasFicha
{
    public static readonly IReadOnlyDictionary<string, string> Rollo = new Dictionary<string, string>
    {
        [nameof(DatosRollo.MaterialType)] = "Tipo de material", [nameof(DatosRollo.RollTypeSize)] = "Medida rollo",
        [nameof(DatosRollo.GaugeMicrons)] = "Calibre (micrones)", [nameof(DatosRollo.KgPerRoll)] = "Kilos por rollo",
        [nameof(DatosRollo.TreatmentDynes)] = "Tratamiento (dynas)", [nameof(DatosRollo.Pigment)] = "Pigmento",
        [nameof(DatosRollo.Additive)] = "Aditivo", [nameof(DatosRollo.Perforation)] = "Perforación",
        [nameof(DatosRollo.PreliminaryPrint)] = "Impresión preliminar",
    };

    public static readonly IReadOnlyDictionary<string, string> Pt = new Dictionary<string, string>
    {
        [nameof(DatosPt.CustomerPartNumber)] = "No. parte cliente", [nameof(DatosPt.FinalSize)] = "Medida final",
        [nameof(DatosPt.Inks)] = "Tintas", [nameof(DatosPt.Pantones)] = "Pantones", [nameof(DatosPt.DieCut)] = "Suaje",
        [nameof(DatosPt.Packaging)] = "Empaque", [nameof(DatosPt.SealType)] = "Tipo de sello", [nameof(DatosPt.KgPerThousand)] = "Kilos por millar",
    };
}
