namespace PolyConecta.Domain.Entities;

public class ManufacturingOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Parent/Child Hierarchy for Master Order vs Process Sub-Orders
    public Guid? ParentId { get; set; }
    public ManufacturingOrder? ParentOrder { get; set; }
    public List<ManufacturingOrder> ChildOrders { get; set; } = new();

    public string Name { get; set; } = string.Empty; // "OM-2026-0421" or "OF-EXT-2026-0421-1"
    public string ProcessType { get; set; } = "Master"; // "Master", "Extrusion", "Printing", "Bagging"
    public int? ContpaqDocumentId { get; set; } // CONTPAQi admDocumentos CIDDOCUMENTO
    public string CustomerCode { get; set; } = string.Empty;

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal ProductQtyTarget { get; set; }
    public decimal ProductQtyProduced { get; set; } = 0.0m;
    public decimal ScrapQty { get; set; } = 0.0m;

    public string State { get; set; } = "Draft"; // "Draft", "Approved", "Progress", "To_Close", "Done", "Cancel"
    public bool SalesApproved { get; set; }
    public bool CreditApproved { get; set; }

    public string WorkCenterId { get; set; } = string.Empty; // Machine ID
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
