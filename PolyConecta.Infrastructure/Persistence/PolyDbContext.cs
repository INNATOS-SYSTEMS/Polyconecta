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
#pragma warning disable CS0618 // Product legado: se retira con su fase (R-08).
    public DbSet<Product> Products => Set<Product>();
#pragma warning restore CS0618
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

    // Pedido de venta (F1)
    public DbSet<Domain.Ventas.SalesOrder> Pedidos => Set<Domain.Ventas.SalesOrder>();

    // Chatter y favoritos (F1, R-04)
    public DbSet<Domain.Plataforma.Chatter.ChatterMessage> MensajesChatter => Set<Domain.Plataforma.Chatter.ChatterMessage>();
    public DbSet<Domain.Plataforma.Listas.SavedSearch> Favoritos => Set<Domain.Plataforma.Listas.SavedSearch>();

    // Additional Entities
    public DbSet<PolyLocation> Locations => Set<PolyLocation>();
    public DbSet<LotGenealogy> LotGenealogies => Set<LotGenealogy>();
    public DbSet<MassBalanceAudit> MassBalanceAudits => Set<MassBalanceAudit>();
    public DbSet<RawMaterialCatalog> RawMaterialCatalogs => Set<RawMaterialCatalog>();
    public DbSet<SupplierProductMapping> SupplierProductMappings => Set<SupplierProductMapping>();

    // Las entidades previas a F0 no fijan la precisión de sus decimales: se hace explícita la que ya tienen en
    // la base, decimal(18,2). Cada fase fija la suya al rehacerlas (04-modelo-de-dominio.md).
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Las transiciones pendientes viven en memoria hasta guardarse en StateTransitionLog.
        builder.Ignore<TransicionRegistrada>();

        builder.ApplyConfigurationsFromAssembly(typeof(PolyDbContext).Assembly);

        // Identity reducida a credenciales, en plt (R-01).
        builder.Entity<IdentityUserClaim<long>>().ToTable("user_credential_claim", "plt");
        builder.Entity<IdentityUserLogin<long>>().ToTable("user_credential_login", "plt");
        builder.Entity<IdentityUserToken<long>>().ToTable("user_credential_token", "plt");

        // Un esquema por módulo (CT-12). Las entidades previas a F0 se ubican en su módulo sin
        // rediseñarlas; cada fase las rehace con el modelo de 04-modelo-de-dominio.md.
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (entity.GetSchema() is null && SchemaPorEntidad.TryGetValue(entity.ClrType, out var schema))
                entity.SetSchema(schema);

            // Mixins (04 §1): versión de fila para concurrencia y lo archivado oculto por omisión.
            if (typeof(AuditableEntity).IsAssignableFrom(entity.ClrType) && entity.BaseType is null)
                builder.Entity(entity.ClrType).Property(nameof(AuditableEntity.RowVersion)).IsRowVersion();
            if (typeof(ArchivableEntity).IsAssignableFrom(entity.ClrType) && entity.BaseType is null)
                entity.SetQueryFilter(SoloActivos(entity.ClrType));

            // Lo referenciado se archiva, no se borra (04 §1): ninguna llave borra en cascada, salvo la que
            // une un detalle con su documento (líneas, firmas), que el propio documento quita.
            foreach (var fk in entity.GetForeignKeys().Where(fk => !fk.IsOwnership))
                fk.DeleteBehavior = typeof(IParteDeDocumento).IsAssignableFrom(fk.DeclaringEntityType.ClrType)
                                    && fk.PrincipalEntityType.ClrType != typeof(Domain.Plataforma.Seguridad.User)
                                    && fk.DependentToPrincipal is null && fk.PrincipalToDependent is not null
                    ? DeleteBehavior.Cascade
                    : DeleteBehavior.Restrict;
        }
    }

    private static LambdaExpression SoloActivos(Type tipo)
    {
        var e = Expression.Parameter(tipo, "e");
        return Expression.Lambda(Expression.Property(e, nameof(ArchivableEntity.IsActive)), e);
    }

    private static readonly Dictionary<Type, string> SchemaPorEntidad = new()
    {
#pragma warning disable CS0618 // Product legado (R-08).
        [typeof(Product)] = "inv",
#pragma warning restore CS0618
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
