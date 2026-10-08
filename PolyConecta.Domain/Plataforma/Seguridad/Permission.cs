using PolyConecta.Domain.Common;

namespace PolyConecta.Domain.Plataforma.Seguridad;

/// <summary>Lo que se protege: un documento (pedido) o una funcionalidad (sincronización, usuarios).</summary>
public enum TipoObjeto
{
    Documento,
    Funcionalidad,
}

/// <summary>
/// Un permiso del catálogo cerrado (D-148). Lo escribe el sembrador desde <see cref="Permisos"/>; nadie
/// lo edita desde la interfaz. Módulo, objeto y acción son los tres niveles del árbol de los dos paneles.
/// </summary>
public sealed class Permission : ArchivableEntity
{
    public string Key { get; private set; } = string.Empty;

    public string Module { get; private set; } = string.Empty;

    public string Object { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public TipoObjeto ObjectKind { get; private set; }

    public string ModuleLabel { get; private set; } = string.Empty;

    public string ObjectLabel { get; private set; } = string.Empty;

    /// <summary>Lo que se ve en el panel: la acción ("Confirmar").</summary>
    public string Label { get; private set; } = string.Empty;

    private Permission() { }

    public Permission(DefinicionPermiso definicion)
    {
        ArgumentNullException.ThrowIfNull(definicion);
        Key = definicion.Clave;
        Actualizar(definicion);
    }

    /// <summary>Copia las etiquetas del catálogo. Devuelve si algo cambió.</summary>
    public bool Actualizar(DefinicionPermiso d)
    {
        ArgumentNullException.ThrowIfNull(d);
        if (d.Clave != Key) throw new ArgumentException("La clave de un permiso no cambia.", nameof(d));
        var cambio = Module != d.Modulo || Object != d.Objeto || Action != d.Accion || ObjectKind != d.TipoObjeto
                     || ModuleLabel != d.EtiquetaModulo || ObjectLabel != d.EtiquetaObjeto || Label != d.EtiquetaAccion;
        Module = d.Modulo;
        Object = d.Objeto;
        Action = d.Accion;
        ObjectKind = d.TipoObjeto;
        ModuleLabel = d.EtiquetaModulo;
        ObjectLabel = d.EtiquetaObjeto;
        Label = d.EtiquetaAccion;
        return cambio;
    }
}
