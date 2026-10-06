using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyConecta.Domain.Common;

namespace PolyConecta.Infrastructure.Persistence;

public static class SyncStateMapping
{
    /// <summary>Columnas erp_* y sync_* de un documento que escribe en CONTPAQi (CT-13, CT-15).</summary>
    public static void OwnsSyncState<T>(this EntityTypeBuilder<T> builder) where T : class, ISyncedDocument
    {
        builder.OwnsOne(x => x.Sync, sync =>
        {
            sync.Property(s => s.Status).HasColumnName("sync_status").HasConversion<string>().HasMaxLength(12);
            sync.Property(s => s.ErpFolio).HasColumnName("erp_folio").HasMaxLength(50);
            sync.Property(s => s.ErpId).HasColumnName("erp_id").HasMaxLength(50);
            sync.Property(s => s.ErpDocuments).HasColumnName("erp_documents");
            sync.Property(s => s.LastErrorCode).HasColumnName("sync_last_error_code").HasMaxLength(60);
            sync.Property(s => s.LastErrorMessage).HasColumnName("sync_last_error_message").HasMaxLength(500);
            sync.Property(s => s.LastSyncAt).HasColumnName("sync_last_at");
        });
        builder.Navigation(x => x.Sync).IsRequired();
    }
}
