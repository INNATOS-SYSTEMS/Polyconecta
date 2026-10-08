using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyConecta.Domain.Inventario;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Domain.Ventas;

namespace PolyConecta.Infrastructure.Persistence.Configurations.Catalogos;

internal static class Auditoria
{
    public static void Columnas<T>(EntityTypeBuilder<T> b) where T : Domain.Common.AuditableEntity
    {
        b.Property(x => x.CreatedBy).HasMaxLength(100);
        b.Property(x => x.ModifiedBy).HasMaxLength(100);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("product", "inv");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ErpProductId).IsUnique();
        b.Property(x => x.ErpCode).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.ErpCode).IsUnique();
        b.Property(x => x.Name).HasMaxLength(255).IsRequired();
        b.Property(x => x.ErpUom).HasMaxLength(10).IsRequired();
        b.HasOne<ProductClassification>().WithMany().HasForeignKey(x => x.ClassificationId);
        b.HasMany(x => x.PackagingUnits).WithOne().HasForeignKey(u => u.ProductId);
        b.Navigation(x => x.PackagingUnits).HasField("_unidades").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasOne(x => x.Roll).WithOne().HasForeignKey<RollSpecification>(r => r.ProductId);
        b.HasOne(x => x.Pt).WithOne().HasForeignKey<PtSpecification>(r => r.ProductId);
        b.Ignore(x => x.Etiqueta);
        b.Ignore(x => x.UnidadBase);
        Auditoria.Columnas(b);
    }
}

public sealed class PackagingUnitConfiguration : IEntityTypeConfiguration<PackagingUnit>
{
    public void Configure(EntityTypeBuilder<PackagingUnit> b)
    {
        b.ToTable("packaging_unit", "inv");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(10).IsRequired();
        b.HasIndex(x => new { x.ProductId, x.Code }).IsUnique();
        Auditoria.Columnas(b);
    }
}

public sealed class ProductClassificationConfiguration : IEntityTypeConfiguration<ProductClassification>
{
    public void Configure(EntityTypeBuilder<ProductClassification> b)
    {
        b.ToTable("product_classification", "inv");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.ErpValue).HasMaxLength(60);
        Auditoria.Columnas(b);
    }
}

public sealed class RollSpecificationConfiguration : IEntityTypeConfiguration<RollSpecification>
{
    public void Configure(EntityTypeBuilder<RollSpecification> b)
    {
        b.ToTable("roll_specification", "inv");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ProductId).IsUnique();
        b.Property(x => x.MaterialType).HasMaxLength(120).IsRequired();
        b.Property(x => x.RollTypeSize).HasMaxLength(120).IsRequired();
        b.Property(x => x.GaugeMicrons).HasPrecision(9, 2);
        b.Property(x => x.KgPerRoll).HasPrecision(12, 3);
        b.Property(x => x.Pigment).HasMaxLength(120);
        b.Property(x => x.Additive).HasMaxLength(120);
        b.Property(x => x.Perforation).HasMaxLength(120);
        b.Property(x => x.PreliminaryPrint).HasMaxLength(120);
        Auditoria.Columnas(b);
    }
}

public sealed class PtSpecificationConfiguration : IEntityTypeConfiguration<PtSpecification>
{
    public void Configure(EntityTypeBuilder<PtSpecification> b)
    {
        b.ToTable("pt_specification", "inv");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ProductId).IsUnique();
        b.HasOne(x => x.RelatedRoll).WithMany().HasForeignKey(x => x.RelatedRollSpecificationId);
        foreach (var p in new[] { nameof(PtSpecification.CustomerPartNumber), nameof(PtSpecification.FinalSize), nameof(PtSpecification.Inks),
                     nameof(PtSpecification.Pantones), nameof(PtSpecification.DieCut), nameof(PtSpecification.Packaging), nameof(PtSpecification.SealType) })
            b.Property(p).HasMaxLength(120);
        b.Property(x => x.KgPerThousand).HasPrecision(12, 4);
        Auditoria.Columnas(b);
    }
}

public sealed class ErpWarehouseConfiguration : IEntityTypeConfiguration<ErpWarehouse>
{
    public void Configure(EntityTypeBuilder<ErpWarehouse> b)
    {
        b.ToTable("erp_warehouse", "inv");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ErpWarehouseId).IsUnique();
        b.Property(x => x.ErpCode).HasMaxLength(30).IsRequired();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        Auditoria.Columnas(b);
    }
}

public sealed class ErpAgentConfiguration : IEntityTypeConfiguration<ErpAgent>
{
    public void Configure(EntityTypeBuilder<ErpAgent> b)
    {
        b.ToTable("erp_agent", "ven");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ErpAgentId).IsUnique();
        b.Property(x => x.ErpCode).HasMaxLength(30).IsRequired();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(12);
        b.Ignore(x => x.Etiqueta);
        Auditoria.Columnas(b);
    }
}

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customer", "ven");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ErpCustomerId).IsUnique();
        b.Property(x => x.ErpCode).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.ErpCode).IsUnique();
        b.Property(x => x.LegalName).HasMaxLength(255).IsRequired();
        b.Property(x => x.TaxId).HasMaxLength(20);
        b.Property(x => x.Currency).HasMaxLength(3).IsFixedLength();
        b.HasMany(x => x.Addresses).WithOne().HasForeignKey(a => a.CustomerId);
        b.Navigation(x => x.Addresses).HasField("_domicilios").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(x => x.Etiqueta);
        b.Ignore(x => x.DomiciliosDeEnvio);
        Auditoria.Columnas(b);
    }
}

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> b)
    {
        b.ToTable("customer_address", "ven");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ErpAddressId).IsUnique();
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
        foreach (var p in new[] { nameof(CustomerAddress.Street), nameof(CustomerAddress.Neighborhood), nameof(CustomerAddress.City),
                     nameof(CustomerAddress.Municipality), nameof(CustomerAddress.State), nameof(CustomerAddress.Country), nameof(CustomerAddress.Branch) })
            b.Property(p).HasMaxLength(120);
        b.Property(x => x.ExteriorNumber).HasMaxLength(30);
        b.Property(x => x.InteriorNumber).HasMaxLength(30);
        b.Property(x => x.PostalCode).HasMaxLength(10);
        b.Ignore(x => x.Texto);
        Auditoria.Columnas(b);
    }
}

/// <summary>El usuario se liga a su agente de CONTPAQi (D-153).</summary>
public sealed class UserAgenteConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b) => b.HasOne<ErpAgent>().WithMany().HasForeignKey(x => x.ErpAgentId);
}
