namespace PolyConecta.Domain.Common;

/// <summary>
/// Mixin de archivado (04 §1): lo que otro registro referencia se archiva, no se borra. La
/// persistencia oculta lo archivado con un filtro global.
/// </summary>
public abstract class ArchivableEntity : AuditableEntity
{
    public bool IsActive { get; private set; } = true;

    public void Archive() => IsActive = false;

    public void Restore() => IsActive = true;
}
