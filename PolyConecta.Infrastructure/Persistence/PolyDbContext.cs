using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using PolyConecta.Domain.Common;
using PolyConecta.Domain.Entities;
using PolyConecta.Domain.Plataforma;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Infrastructure.Plataforma.Identidad;

namespace PolyConecta.Infrastructure.Persistence;

/// <summary>
/// Contexto de PolyConecta. Hereda de <see cref="IdentityUserContext{TUser, TKey}"/> solo por las
/// credenciales (R-01): sin las tablas de roles de Identity, que no se usan (los grupos son del dominio).
/// </summary>
public class PolyDbContext : IdentityUserContext<CredencialUsuario, long>
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

    // Plataforma (base común, F0)
    public DbSet<StateTransitionLog> StateTransitionLogs => Set<StateTransitionLog>();
    public DbSet<ReferenceSequence> ReferenceSequences => Set<ReferenceSequence>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // Seguridad (F1, D-148)
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<User> Usuarios => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Permission> Permissions => Set<Permission>();

    // Catálogos sincronizados de CONTPAQi (F1, R-08)
    public DbSet<Domain.Inventario.Product> Productos => Set<Domain.Inventario.Product>();
    public DbSet<Domain.Inventario.ProductClassification> Clasificaciones => Set<Domain.Inventario.ProductClassification>();
    public DbSet<Domain.Inventario.ErpWarehouse> AlmacenesErp => Set<Domain.Inventario.ErpWarehouse>();
    public DbSet<Domain.Ventas.ErpAgent> AgentesErp => Set<Domain.Ventas.ErpAgent>();
    public DbSet<Domain.Ventas.Customer> Clientes => Set<Domain.Ventas.Customer>();

    // Additional Entities
    public DbSet<PolyLocation> Locations => Set<PolyLocation>();
    public DbSet<LotGenealogy> LotGenealogies => Set<LotGenealogy>();
    public DbSet<MassBalanceAudit> MassBalanceAudits => Set<MassBalanceAudit>();
    public DbSet<RawMaterialCatalog> RawMaterialCatalogs => Set<RawMaterialCatalog>();
    public DbSet<SupplierProductMapping> SupplierProductMappings => Set<SupplierProductMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Las transiciones pendientes viven en memoria hasta guardarse en StateTransitionLog.
        modelBuilder.Ignore<TransicionRegistrada>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PolyDbContext).Assembly);

        // Identity reducida a credenciales, en plt (R-01).
        modelBuilder.Entity<IdentityUserClaim<long>>().ToTable("user_credential_claim", "plt");
        modelBuilder.Entity<IdentityUserLogin<long>>().ToTable("user_credential_login", "plt");
        modelBuilder.Entity<IdentityUserToken<long>>().ToTable("user_credential_token", "plt");

        // Un esquema por módulo (CT-12). Las entidades previas a F0 se ubican en su módulo sin
        // rediseñarlas; cada fase las rehace con el modelo de 04-modelo-de-dominio.md.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.GetSchema() is null && SchemaPorEntidad.TryGetValue(entity.ClrType, out var schema))
                entity.SetSchema(schema);

            // Mixins (04 §1): versión de fila para concurrencia y lo archivado oculto por omisión.
            if (typeof(AuditableEntity).IsAssignableFrom(entity.ClrType) && entity.BaseType is null)
                modelBuilder.Entity(entity.ClrType).Property(nameof(AuditableEntity.RowVersion)).IsRowVersion();
            if (typeof(ArchivableEntity).IsAssignableFrom(entity.ClrType) && entity.BaseType is null)
                entity.SetQueryFilter(SoloActivos(entity.ClrType));

            // Lo referenciado se archiva, no se borra (04 §1): ninguna llave borra en cascada.
            foreach (var fk in entity.GetForeignKeys().Where(fk => !fk.IsOwnership))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static LambdaExpression SoloActivos(Type tipo)
    {
        var e = Expression.Parameter(tipo, "e");
        return Expression.Lambda(Expression.Property(e, nameof(ArchivableEntity.IsActive)), e);
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
