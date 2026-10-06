namespace PolyConecta.Domain.Common;

/// <summary>
/// Mixin de toda entidad de negocio (04 §1): id técnico inmutable, quién y cuándo la creó y la
/// modificó, y versión de fila para concurrencia optimista. Los campos de auditoría los llena la
/// persistencia al guardar; el dominio no los toca.
/// </summary>
public abstract class AuditableEntity
{
    private readonly List<TransicionRegistrada> _transiciones = [];

    public long Id { get; protected set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string CreatedBy { get; private set; } = string.Empty;

    public DateTimeOffset? ModifiedAt { get; private set; }

    public string? ModifiedBy { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Transiciones de estado pendientes de registrar en StateTransitionLog (CT-32).</summary>
    public IReadOnlyList<TransicionRegistrada> TransicionesPendientes => _transiciones;

    internal void AgregarTransicion(TransicionRegistrada transicion) => _transiciones.Add(transicion);

    public void LimpiarTransicionesPendientes() => _transiciones.Clear();

    public void MarcarCreado(DateTimeOffset cuando, string quien)
    {
        CreatedAt = cuando;
        CreatedBy = quien;
    }

    public void MarcarModificado(DateTimeOffset cuando, string quien)
    {
        ModifiedAt = cuando;
        ModifiedBy = quien;
    }
}

/// <summary>Una transición ocurrida en memoria, antes de guardarse en la bitácora.</summary>
public sealed record TransicionRegistrada(string Desde, string Hacia, string? Nota);
