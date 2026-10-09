namespace PolyConecta.Application.Common;

/// <summary>
/// Una acción del documento con si está disponible y, si no, por qué (CT-26). La interfaz solo la pinta;
/// la API rechaza igual la acción no disponible. <see cref="Aviso"/> advierte algo antes de ejecutarla.
/// </summary>
public sealed record AccionDisponible(string Accion, bool Disponible, string? Razon = null, string? Aviso = null)
{
    public static AccionDisponible Si(string accion, string? aviso = null) => new(accion, true, null, aviso);

    public static AccionDisponible No(string accion, string razon) => new(accion, false, razon);
}
