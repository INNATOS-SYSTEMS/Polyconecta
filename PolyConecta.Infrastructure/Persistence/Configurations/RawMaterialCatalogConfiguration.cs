using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyConecta.Domain.Entities;

namespace PolyConecta.Infrastructure.Persistence.Configurations;

public class RawMaterialCatalogConfiguration : IEntityTypeConfiguration<RawMaterialCatalog>
{
    // Fecha fija: un valor dinámico en HasData cambia el modelo en cada compilación.
    internal static readonly DateTime FechaSemilla = new(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<RawMaterialCatalog> builder)
    {
        builder.ToTable("poly_raw_material_catalogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InternalSku).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.InternalSku).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(40).IsRequired();
        builder.Property(x => x.TargetHopper).HasMaxLength(20).IsRequired();

        builder.HasMany(x => x.SupplierMappings)
               .WithOne(x => x.RawMaterialCatalog)
               .HasForeignKey(x => x.RawMaterialCatalogId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new RawMaterialCatalog
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                InternalSku = "MP-RES-HD-001",
                Name = "Resina PE Alta Densidad Alathon M6210",
                Category = "VirginResin",
                MfiMeltFlowIndex = 0.95m,
                DensityGcm3 = 0.958m,
                TargetHopper = "Tolva A",
                CidProductoContpaq = 101,
                IsActive = true,
                CreatedAt = FechaSemilla
            },
            new RawMaterialCatalog
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                InternalSku = "MP-RES-LD-002",
                Name = "Resina PE Baja Densidad Braskem BC818",
                Category = "VirginResin",
                MfiMeltFlowIndex = 2.00m,
                DensityGcm3 = 0.922m,
                TargetHopper = "Tolva B",
                CidProductoContpaq = 102,
                IsActive = true,
                CreatedAt = FechaSemilla
            },
            new RawMaterialCatalog
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                InternalSku = "MP-ADI-UV-001",
                Name = "Masterbatch Aditivo Anti-UV Clariant",
                Category = "Additive",
                MfiMeltFlowIndex = 1.10m,
                DensityGcm3 = 0.940m,
                TargetHopper = "Tolva C",
                CidProductoContpaq = 103,
                IsActive = true,
                CreatedAt = FechaSemilla
            }
        );
    }
}

public class SupplierProductMappingConfiguration : IEntityTypeConfiguration<SupplierProductMapping>
{
    public void Configure(EntityTypeBuilder<SupplierProductMapping> builder)
    {
        builder.ToTable("poly_supplier_product_mappings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SupplierCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SupplierSku).HasMaxLength(50).IsRequired();

        builder.HasData(
            new SupplierProductMapping
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                RawMaterialCatalogId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                SupplierCode = "LYONDELL",
                SupplierProductName = "Alathon M6210 High Density Polyethylene",
                SupplierSku = "LYO-M6210-HD",
                MappedAt = RawMaterialCatalogConfiguration.FechaSemilla
            },
            new SupplierProductMapping
            {
                Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                RawMaterialCatalogId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                SupplierCode = "BRASKEM",
                SupplierProductName = "Braskem BC818 Low Density Polyethylene",
                SupplierSku = "BRASK-BC818",
                MappedAt = RawMaterialCatalogConfiguration.FechaSemilla
            }
        );
    }
}
