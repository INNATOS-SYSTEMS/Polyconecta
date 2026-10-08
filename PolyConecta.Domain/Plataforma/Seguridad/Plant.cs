using PolyConecta.Domain.Common;

namespace PolyConecta.Domain.Plataforma.Seguridad;

/// <summary>Planta (PIM, SC, MTM). La asignación de grupos es por planta (01 §3, D-148).</summary>
public sealed class Plant : ArchivableEntity
{
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    private Plant() { }

    public Plant(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Falta la clave de la planta.", nameof(code));
        Code = code.Trim().ToUpperInvariant();
        Name = string.IsNullOrWhiteSpace(name) ? Code : name.Trim();
    }
}
