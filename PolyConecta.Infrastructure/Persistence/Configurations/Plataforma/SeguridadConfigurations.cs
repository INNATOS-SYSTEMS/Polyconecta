using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyConecta.Domain.Plataforma.Seguridad;
using PolyConecta.Infrastructure.Plataforma.Identidad;

namespace PolyConecta.Infrastructure.Persistence.Configurations.Plataforma;

public sealed class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.ToTable("plant", "plt");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permission", "plt");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Module).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Object).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ObjectKind).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ModuleLabel).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ObjectLabel).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
    }
}

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("group", "plt");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        // Único entre los activos (data-model §1).
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("[IsActive] = 1");
        builder.Property(x => x.Description).HasMaxLength(400);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.OwnsMany(x => x.Permissions, p =>
        {
            p.ToTable("group_permission", "plt");
            p.WithOwner().HasForeignKey("GroupId");
            p.HasKey("GroupId", nameof(GroupPermission.PermissionId));
            p.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionId);
        });
        builder.Navigation(x => x.Permissions).HasField("_permisos").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("user", "plt");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.UserName).IsUnique();
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(120);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.OwnsMany(x => x.Assignments, a =>
        {
            a.ToTable("group_assignment", "plt");
            a.WithOwner().HasForeignKey("UserId");
            a.HasKey("UserId", nameof(GroupAssignment.GroupId), nameof(GroupAssignment.PlantId));
            a.HasOne<Group>().WithMany().HasForeignKey(x => x.GroupId);
            a.HasOne<Plant>().WithMany().HasForeignKey(x => x.PlantId);
        });
        builder.Navigation(x => x.Assignments).HasField("_asignaciones").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Tablas de Identity reducidas a credenciales, en plt (R-01).</summary>
public sealed class CredencialUsuarioConfiguration : IEntityTypeConfiguration<CredencialUsuario>
{
    public void Configure(EntityTypeBuilder<CredencialUsuario> builder)
    {
        builder.ToTable("user_credential", "plt");
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne<User>().WithOne().HasForeignKey<CredencialUsuario>(x => x.UserId);
    }
}
