namespace PolyConecta.Domain.Common;

/// <summary>
/// Una operación del dominio no procede por una regla de negocio. La API la traduce a 409 con su
/// <see cref="Codigo"/> estable y la <see cref="Razon"/> que pinta la interfaz (CT-26).
/// </summary>
public sealed class ReglaDeNegocioException(string codigo, string razon) : InvalidOperationException(razon)
{
    public string Codigo { get; } = codigo;

    public string Razon { get; } = razon;
}
