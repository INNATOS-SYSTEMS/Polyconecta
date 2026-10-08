using PolyConecta.Domain.Common;

namespace PolyConecta.Domain.Inventario;

/// <summary>Bloque Rollo de la ficha técnica (04 §3).</summary>
public sealed record DatosRollo(
    string MaterialType, string RollTypeSize, decimal? GaugeMicrons, decimal? KgPerRoll, int? TreatmentDynes,
    string? Pigment, string? Additive, string? Perforation, string? PreliminaryPrint)
{
    internal void Validar()
    {
        if (string.IsNullOrWhiteSpace(MaterialType) || string.IsNullOrWhiteSpace(RollTypeSize))
            throw new ReglaDeNegocioException("FICHA_INCOMPLETA", "El bloque Rollo lleva tipo de material y tipo y medida del rollo.");
        if (GaugeMicrons < 0 || KgPerRoll < 0 || TreatmentDynes < 0)
            throw new ReglaDeNegocioException("FICHA_INVALIDA", "Calibre, kilos por rollo y tratamiento no son negativos.");
    }
}

/// <summary>Bloque PT de la ficha técnica (04 §3).</summary>
public sealed record DatosPt(
    string? CustomerPartNumber, string? FinalSize, string? Inks, string? Pantones, string? DieCut, string? Packaging,
    string? SealType, decimal? KgPerThousand);

/// <summary>Ficha técnica, bloque Rollo (<c>inv.roll_specification</c>), 1:1 con el producto.</summary>
public sealed class RollSpecification : AuditableEntity
{
    public long ProductId { get; private set; }

    public string MaterialType { get; private set; } = string.Empty;

    public string RollTypeSize { get; private set; } = string.Empty;

    public decimal? GaugeMicrons { get; private set; }

    public decimal? KgPerRoll { get; private set; }

    public int? TreatmentDynes { get; private set; }

    public string? Pigment { get; private set; }

    public string? Additive { get; private set; }

    public string? Perforation { get; private set; }

    public string? PreliminaryPrint { get; private set; }

    private RollSpecification() { }

    internal RollSpecification(DatosRollo d) => Actualizar(d);

    internal void Actualizar(DatosRollo d)
    {
        MaterialType = d.MaterialType.Trim();
        RollTypeSize = d.RollTypeSize.Trim();
        GaugeMicrons = d.GaugeMicrons;
        KgPerRoll = d.KgPerRoll;
        TreatmentDynes = d.TreatmentDynes;
        Pigment = d.Pigment;
        Additive = d.Additive;
        Perforation = d.Perforation;
        PreliminaryPrint = d.PreliminaryPrint;
    }

    public DatosRollo Datos() =>
        new(MaterialType, RollTypeSize, GaugeMicrons, KgPerRoll, TreatmentDynes, Pigment, Additive, Perforation, PreliminaryPrint);
}

/// <summary>Ficha técnica, bloque PT (<c>inv.pt_specification</c>), siempre ligado a un bloque Rollo.</summary>
public sealed class PtSpecification : AuditableEntity
{
    public long ProductId { get; private set; }

    public long RelatedRollSpecificationId { get; private set; }

    public RollSpecification RelatedRoll { get; private set; } = null!;

    public string? CustomerPartNumber { get; private set; }

    public string? FinalSize { get; private set; }

    public string? Inks { get; private set; }

    public string? Pantones { get; private set; }

    public string? DieCut { get; private set; }

    public string? Packaging { get; private set; }

    public string? SealType { get; private set; }

    /// <summary>Factor de ficha: kilos por millar.</summary>
    public decimal? KgPerThousand { get; private set; }

    private PtSpecification() { }

    internal PtSpecification(DatosPt d, RollSpecification rollo) => Actualizar(d, rollo);

    internal void Actualizar(DatosPt d, RollSpecification rollo)
    {
        if (d.KgPerThousand < 0) throw new ReglaDeNegocioException("FICHA_INVALIDA", "Los kilos por millar no son negativos.");
        RelatedRoll = rollo ?? throw new ReglaDeNegocioException("FICHA_INCOMPLETA", "El bloque PT va ligado a un bloque Rollo.");
        if (rollo.Id != 0) RelatedRollSpecificationId = rollo.Id;
        CustomerPartNumber = d.CustomerPartNumber;
        FinalSize = d.FinalSize;
        Inks = d.Inks;
        Pantones = d.Pantones;
        DieCut = d.DieCut;
        Packaging = d.Packaging;
        SealType = d.SealType;
        KgPerThousand = d.KgPerThousand;
    }

    public DatosPt Datos() => new(CustomerPartNumber, FinalSize, Inks, Pantones, DieCut, Packaging, SealType, KgPerThousand);
}
