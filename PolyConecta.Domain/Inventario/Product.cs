using PolyConecta.Domain.Common;

namespace PolyConecta.Domain.Inventario;

/// <summary>Lo que trae la lectura de productos del bridge (contrato §6, `1.1`).</summary>
public sealed record DatosErpProducto(long IdErp, string Codigo, string Nombre, string UnidadBase, bool LlevaLote, bool Activo);

/// <summary>
/// Producto sincronizado de CONTPAQi (R-08, <c>inv.product</c>). Lo de CONTPAQi es de solo lectura; lo de
/// PolyConecta es la clasificación (D-86) y la ficha técnica (CT-14, FR-015). Inactivo en CONTPAQi se
/// archiva, no se borra.
/// </summary>
public sealed class Product : ArchivableEntity
{
    private readonly List<PackagingUnit> _unidades = [];

    /// <summary>CIDPRODUCTO.</summary>
    public long ErpProductId { get; private set; }

    /// <summary>La clave (`PT1113 C567`).</summary>
    public string ErpCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Unidad base de CONTPAQi (CABREVIATURA), D-127.</summary>
    public string ErpUom { get; private set; } = string.Empty;

    public bool TracksLots { get; private set; }

    public long? ClassificationId { get; private set; }

    /// <summary>Última vez que la sincronización lo cambió.</summary>
    public DateTimeOffset? ErpSyncedAt { get; private set; }

    public IReadOnlyList<PackagingUnit> PackagingUnits => _unidades;

    public RollSpecification? Roll { get; private set; }

    public PtSpecification? Pt { get; private set; }

    /// <summary>"Clave - Nombre" (D-141).</summary>
    public string Etiqueta => $"{ErpCode} - {Name}";

    public PackagingUnit UnidadBase => _unidades.First(u => u.IsErpBaseUnit);

    private Product() { }

    public static Product DesdeErp(DatosErpProducto datos, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(datos);
        var p = new Product { ErpProductId = datos.IdErp };
        p.ActualizarDesdeErp(datos, ahora);
        return p;
    }

    /// <summary>Copia lo de CONTPAQi. Devuelve si algo cambió; sin cambios no toca nada (SC-004).</summary>
    public bool ActualizarDesdeErp(DatosErpProducto datos, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(datos);
        var uom = datos.UnidadBase.Trim().ToUpperInvariant();
        var cambio = ErpCode != datos.Codigo || Name != datos.Nombre || ErpUom != uom || TracksLots != datos.LlevaLote;
        if (cambio)
        {
            ErpCode = datos.Codigo;
            Name = datos.Nombre;
            ErpUom = uom;
            TracksLots = datos.LlevaLote;
            ErpSyncedAt = ahora;
        }
        // En F1 cada producto tiene una sola unidad: la base de CONTPAQi, sin conversión (D-124, D-127).
        var base_ = _unidades.FirstOrDefault(u => u.IsErpBaseUnit);
        if (base_ is null) _unidades.Add(new PackagingUnit(uom, isErpBaseUnit: true));
        else base_.CambiarCodigo(uom);
        return cambio;
    }

    /// <summary>Inactivo o ausente en CONTPAQi. Los pedidos que ya lo tienen no cambian.</summary>
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

    /// <summary>La clasificación es de PolyConecta (D-86): la sincronización solo la llena si está vacía.</summary>
    public void Clasificar(long? classificationId) => ClassificationId = classificationId;

    /// <summary>Valor inicial desde CONTPAQi (FR-017): solo si todavía no tiene clasificación.</summary>
    public bool ClasificarSiVacia(long classificationId)
    {
        if (ClassificationId is not null) return false;
        ClassificationId = classificationId;
        return true;
    }

    /// <summary>
    /// Ficha técnica (FR-018): siempre los dos bloques, y el PT ligado a un rollo: el de este mismo producto
    /// o, si se indica, el de otro producto de segundo proceso.
    /// </summary>
    public void GuardarFichaTecnica(DatosRollo rollo, DatosPt pt, RollSpecification? rolloLigado = null)
    {
        if (rollo is null || pt is null)
            throw new ReglaDeNegocioException("FICHA_INCOMPLETA", "La ficha técnica lleva los dos bloques, Rollo y PT.");
        rollo.Validar();
        if (Roll is null) Roll = new RollSpecification(rollo);
        else Roll.Actualizar(rollo);
        var ligado = rolloLigado ?? Roll;
        if (Pt is null) Pt = new PtSpecification(pt, ligado);
        else Pt.Actualizar(pt, ligado);
    }
}

/// <summary>Unidad de empaque (<c>inv.packaging_unit</c>). En F1 solo existe la base (D-127).</summary>
public sealed class PackagingUnit : AuditableEntity
{
    public long ProductId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public bool IsErpBaseUnit { get; private set; }

    private PackagingUnit() { }

    public PackagingUnit(string code, bool isErpBaseUnit)
    {
        Code = code;
        IsErpBaseUnit = isErpBaseUnit;
    }

    internal void CambiarCodigo(string code)
    {
        if (Code != code) Code = code;
    }
}

/// <summary>Clasificación propia del producto (D-86, <c>inv.product_classification</c>). La edita el Administrador.</summary>
public sealed class ProductClassification : ArchivableEntity
{
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>El valor de "TIPO DE PRODUCTOS" de CONTPAQi del que nació, si lo hay.</summary>
    public string? ErpValue { get; private set; }

    private ProductClassification() { }

    public ProductClassification(string code, string name, string? erpValue = null)
    {
        Editar(code, name);
        ErpValue = erpValue;
    }

    public void Editar(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new ReglaDeNegocioException("CLASIFICACION_INCOMPLETA", "La clasificación lleva código y nombre.");
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
    }
}

/// <summary>Almacén de CONTPAQi sincronizado (<c>inv.erp_warehouse</c>). No crea ubicaciones (D-43).</summary>
public sealed class ErpWarehouse : ArchivableEntity
{
    public long ErpWarehouseId { get; private set; }

    public string ErpCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    private ErpWarehouse() { }

    public ErpWarehouse(long erpWarehouseId, string erpCode, string name)
    {
        ErpWarehouseId = erpWarehouseId;
        ActualizarDesdeErp(erpCode, name);
    }

    public bool ActualizarDesdeErp(string erpCode, string name)
    {
        if (ErpCode == erpCode && Name == name) return false;
        ErpCode = erpCode;
        Name = name;
        return true;
    }
}
