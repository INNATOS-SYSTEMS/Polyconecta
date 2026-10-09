namespace PolyConecta.Domain.Plataforma.Chatter;

/// <summary><c>Nota</c> es interna (Odoo); <c>Cambio</c> solo lo escribe la persistencia al registrar una transición (R-04).</summary>
public enum TipoMensaje
{
    Mensaje,
    Nota,
    Cambio,
}

/// <summary>
/// Mensaje del chatter de un documento (<c>plt.chatter_message</c>, data-model §2, R-04). Solo se inserta.
/// </summary>
public sealed class ChatterMessage
{
    public const int LargoMaximo = 4000;

    public long Id { get; private set; }

    public string DocumentType { get; private set; } = string.Empty;

    public long DocumentId { get; private set; }

    public TipoMensaje Kind { get; private set; }

    public string Body { get; private set; } = string.Empty;

    /// <summary>Vacío en un <c>Cambio</c>: la transición solo guarda el nombre del usuario.</summary>
    public long? AuthorUserId { get; private set; }

    public string AuthorName { get; private set; } = string.Empty;

    /// <summary>El grupo con el que actuó el autor (CT-32).</summary>
    public string? GroupExercised { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private ChatterMessage() { }

    private ChatterMessage(
        string documentType, long documentId, TipoMensaje kind, string body,
        long? authorUserId, string authorName, string? groupExercised, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(documentType)) throw new ArgumentException("Falta el tipo de documento.", nameof(documentType));
        if (string.IsNullOrWhiteSpace(authorName)) throw new ArgumentException("Falta el autor.", nameof(authorName));
        var texto = body?.Trim() ?? string.Empty;
        if (texto.Length == 0) throw new ArgumentException("El mensaje está vacío.", nameof(body));
        if (texto.Length > LargoMaximo) throw new ArgumentException($"El mensaje pasa de {LargoMaximo} caracteres.", nameof(body));

        DocumentType = documentType;
        DocumentId = documentId;
        Kind = kind;
        Body = texto;
        AuthorUserId = authorUserId;
        AuthorName = authorName;
        GroupExercised = groupExercised;
        CreatedAt = createdAt;
    }

    /// <summary>Mensaje o nota interna que escribe un usuario; el autor sale de la sesión, no del cliente.</summary>
    public static ChatterMessage Publicar(
        string documentType, long documentId, TipoMensaje kind, string body,
        long authorUserId, string authorName, string? groupExercised, DateTimeOffset createdAt)
    {
        if (kind == TipoMensaje.Cambio) throw new ArgumentException("Un cambio solo lo registra la transición.", nameof(kind));
        return new ChatterMessage(documentType, documentId, kind, body, authorUserId, authorName, groupExercised, createdAt);
    }

    /// <summary>El registro de una transición ("Borrador → Confirmado", con su nota si la hay).</summary>
    public static ChatterMessage RegistrarCambio(
        string documentType, long documentId, string fromState, string toState, string? note,
        string authorName, string? groupExercised, DateTimeOffset createdAt)
    {
        var cuerpo = fromState == toState ? toState : $"{fromState} → {toState}";
        if (!string.IsNullOrWhiteSpace(note)) cuerpo += $": {note.Trim()}";
        return new ChatterMessage(documentType, documentId, TipoMensaje.Cambio, cuerpo, null, authorName, groupExercised, createdAt);
    }
}
