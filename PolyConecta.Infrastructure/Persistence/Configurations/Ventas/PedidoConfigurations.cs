using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Infrastructure.Persistence.Configurations.Ventas;

public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> b)
    {
        b.ToTable("sales_order", "ven");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.State);
        b.Property(x => x.Origin).HasConversion<string>().HasMaxLength(10);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        b.Property(x => x.CustomerPo).HasMaxLength(60);
        b.HasOne<ErpAgent>().WithMany().HasForeignKey(x => x.AgentId);
        b.HasOne<CustomerAddress>().WithMany().HasForeignKey(x => x.DeliveryAddressId);
        b.Property(x => x.DeliveryAddressText).HasMaxLength(400);
        b.Property(x => x.Currency).HasMaxLength(3).IsFixedLength();
        b.Property(x => x.ExchangeRate).HasPrecision(18, 6);
        b.Property(x => x.CreatedBy).HasMaxLength(100);
        b.Property(x => x.ModifiedBy).HasMaxLength(100);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.SalesOrderId);
        b.Navigation(x => x.Lines).HasField("_lineas").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Signatures).WithOne().HasForeignKey(s => s.SalesOrderId);
        b.Navigation(x => x.Signatures).HasField("_firmas").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(x => x.AvisoDeEdicion);
        b.OwnsSyncState();
    }
}

public sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> b)
    {
        b.ToTable("sales_order_line", "ven");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        b.HasOne<PackagingUnit>().WithMany().HasForeignKey(x => x.RequestedPackagingUnitId);
        b.Property(x => x.RequestedQty).HasPrecision(18, 4);
        b.Property(x => x.UnitPrice).HasPrecision(18, 6);
        b.Property(x => x.TargetProductionKg).HasPrecision(18, 4);
        b.Property(x => x.TolerancePercentageOverride).HasPrecision(5, 2);
        b.Property(x => x.ErpDocumentLineId).HasMaxLength(50);
        b.Property(x => x.CreatedBy).HasMaxLength(100);
        b.Property(x => x.ModifiedBy).HasMaxLength(100);
    }
}

/// <summary>La base también impide que un rol firme dos veces o que una persona aporte las dos firmas (RF-3, RF-4).</summary>
public sealed class AuthorizationSignatureConfiguration : IEntityTypeConfiguration<AuthorizationSignature>
{
    public void Configure(EntityTypeBuilder<AuthorizationSignature> b)
    {
        b.ToTable("authorization_signature", "ven");
        b.HasKey(x => x.Id);
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(12);
        b.Property(x => x.UserName).HasMaxLength(120).IsRequired();
        b.Property(x => x.GroupExercised).HasMaxLength(80);
        b.HasIndex(x => new { x.SalesOrderId, x.Role }).IsUnique();
        b.HasIndex(x => new { x.SalesOrderId, x.UserId }).IsUnique();
        b.HasOne<Domain.Plataforma.Seguridad.User>().WithMany().HasForeignKey(x => x.UserId);
    }
}

/// <summary>Folio del pedido: PV-2026-0001, relleno 4, reinicio anual (R-09, FR-021).</summary>
public sealed class SecuenciaPedidoVenta : IEntityTypeConfiguration<ReferenceSequence>
{
    public void Configure(EntityTypeBuilder<ReferenceSequence> b) => b.HasData(new
    {
        DocumentType = SalesOrder.TipoDocumento,
        Prefix = "PV-{yyyy}-",
        Padding = 4,
        NextNumber = 1L,
        ResetRule = ResetRule.Anual,
        CurrentPeriod = (string?)null,
    });
}
