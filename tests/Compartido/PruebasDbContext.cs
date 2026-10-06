using Microsoft.EntityFrameworkCore;
using PolyConecta.Infrastructure.Persistence;

namespace PolyConecta.Tests.Compartido;

/// <summary>El modelo real más el documento de prueba, en su propio esquema.</summary>
public sealed class PruebasDbContext(DbContextOptions<PolyDbContext> options) : PolyDbContext(options)
{
    public DbSet<DocumentoDePrueba> Documentos => Set<DocumentoDePrueba>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentoDePrueba>(b =>
        {
            b.ToTable("documento_de_prueba", "prueba");
            b.Property(x => x.Folio).HasMaxLength(50);
            b.Property(x => x.Producto).HasMaxLength(50);
            b.Property(x => x.Cantidad).HasPrecision(18, 3);
            b.Property(x => x.Unidad).HasMaxLength(10);
            b.Property(x => x.Origen).HasMaxLength(30);
            b.Property(x => x.Destino).HasMaxLength(30);
            b.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.CreatedBy).HasMaxLength(100);
            b.Property(x => x.ModifiedBy).HasMaxLength(100);
            b.OwnsSyncState();
        });
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Tabla del documento de prueba. Solo en la base desechable de las pruebas: el esquema real
    /// se crea siempre con las migraciones de PolyDbContext (CT-06).
    /// </summary>
    public const string CrearTabla = """
        EXEC('CREATE SCHEMA prueba');
        CREATE TABLE prueba.documento_de_prueba (
            Id bigint IDENTITY PRIMARY KEY,
            Folio nvarchar(50) NOT NULL,
            Producto nvarchar(50) NOT NULL,
            Cantidad decimal(18,3) NOT NULL,
            Unidad nvarchar(10) NOT NULL,
            Origen nvarchar(30) NOT NULL,
            Destino nvarchar(30) NOT NULL,
            State nvarchar(20) NOT NULL,
            IsActive bit NOT NULL,
            CreatedAt datetimeoffset NOT NULL,
            CreatedBy nvarchar(100) NOT NULL,
            ModifiedAt datetimeoffset NULL,
            ModifiedBy nvarchar(100) NULL,
            RowVersion rowversion NOT NULL,
            sync_status nvarchar(12) NOT NULL,
            erp_folio nvarchar(50) NULL,
            erp_id nvarchar(50) NULL,
            erp_documents nvarchar(max) NULL,
            sync_last_error_code nvarchar(60) NULL,
            sync_last_error_message nvarchar(500) NULL,
            sync_last_at datetimeoffset NULL);
        """;
}
