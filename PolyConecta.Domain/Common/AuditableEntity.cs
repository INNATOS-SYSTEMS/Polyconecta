namespace PolyConecta.Domain.Common;

/// <summary>
/// Mixin de toda entidad de negocio (04 §1): id técnico inmutable, quién y cuándo la creó y la
/// modificó, y versión de fila para concurrencia optimista. Los campos de auditoría los llena la
/// persistencia al guardar; el dominio no los toca.
/// </summary>
public abstract class AuditableEntity
{
    private readonly List<TransicionRegistrada> _transiciones = [];
    private readonly List<string> _cambios = [];

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

    /// <summary>
    /// Cambios descriptivos pendientes de escribir en la bitácora del registro ("Agregó Comercial · PIM",
    /// "Kilos por millar: 8.10 → 8.40"). Los registros sin estados (catálogos) los usan en lugar de transiciones.
    /// </summary>
    public IReadOnlyList<string> CambiosPendientes => _cambios;

    /// <summary>Anota un cambio para la bitácora; la persistencia lo escribe al guardar (D-157, decisión del 9-oct).</summary>
    public void AnotarCambio(string texto)
    {
        if (!string.IsNullOrWhiteSpace(texto)) _cambios.Add(texto.Trim());
    }

    public void LimpiarCambiosPendientes() => _cambios.Clear();

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
