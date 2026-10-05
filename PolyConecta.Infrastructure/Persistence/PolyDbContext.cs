using Microsoft.EntityFrameworkCore;
using PolyConecta.Domain.Entities;

namespace PolyConecta.Infrastructure.Persistence;

public class PolyDbContext : DbContext
{
    public PolyDbContext(DbContextOptions<PolyDbContext> options) : base(options) { }

    // Odoo-Native Core DbSets
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockLot> StockLots => Set<StockLot>();
    public DbSet<ManufacturingOrder> ManufacturingOrders => Set<ManufacturingOrder>();
    public DbSet<Bom> Boms => Set<Bom>();
    public DbSet<BomLine> BomLines => Set<BomLine>();
    public DbSet<StockLocation> StockLocations => Set<StockLocation>();
    public DbSet<StockPicking> StockPickings => Set<StockPicking>();
    public DbSet<StockMove> StockMoves => Set<StockMove>();
    public DbSet<QualityCheck> QualityChecks => Set<QualityCheck>();
    public DbSet<StockScrap> StockScraps => Set<StockScrap>();

    // Additional Entities
    public DbSet<PolyLocation> Locations => Set<PolyLocation>();
    public DbSet<LotGenealogy> LotGenealogies => Set<LotGenealogy>();
    public DbSet<MassBalanceAudit> MassBalanceAudits => Set<MassBalanceAudit>();
    public DbSet<RawMaterialCatalog> RawMaterialCatalogs => Set<RawMaterialCatalog>();
    public DbSet<SupplierProductMapping> SupplierProductMappings => Set<SupplierProductMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PolyDbContext).Assembly);

        // Un esquema por módulo (CT-12). Las entidades previas a F0 se ubican en su módulo sin
        // rediseñarlas; cada fase las rehace con el modelo de 04-modelo-de-dominio.md.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.GetSchema() is null && SchemaPorEntidad.TryGetValue(entity.ClrType, out var schema))
                entity.SetSchema(schema);

            // Lo referenciado se archiva, no se borra (04 §1): ninguna llave borra en cascada.
            foreach (var fk in entity.GetForeignKeys().Where(fk => !fk.IsOwnership))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static readonly Dictionary<Type, string> SchemaPorEntidad = new()
    {
        [typeof(Product)] = "inv",
        [typeof(StockLot)] = "inv",
        [typeof(StockLocation)] = "inv",
        [typeof(StockPicking)] = "inv",
        [typeof(StockMove)] = "inv",
        [typeof(PolyLocation)] = "inv",
        [typeof(RawMaterialCatalog)] = "inv",
        [typeof(SupplierProductMapping)] = "inv",
        [typeof(LotGenealogy)] = "inv",
        [typeof(ManufacturingOrder)] = "prd",
        [typeof(Bom)] = "prd",
        [typeof(BomLine)] = "prd",
        [typeof(MassBalanceAudit)] = "prd",
        [typeof(StockScrap)] = "prd",
        [typeof(QualityCheck)] = "cal",
    };
}
