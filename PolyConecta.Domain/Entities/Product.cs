namespace PolyConecta.Domain.Entities;

[Obsolete("Se retira cuando su fase rediseñe a quien la usa. El producto de F1 es PolyConecta.Domain.Inventario.Product (research R-08).")]
public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "RawMaterial"; // "RawMaterial", "FinishedGood", "Wip", "Scrap"
    public string Uom { get; set; } = "KG"; // "KG", "MT", "PZA"
    public int? ContpaqProductId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
