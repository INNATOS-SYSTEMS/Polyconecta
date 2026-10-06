using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyConecta.Domain.Plataforma;

namespace PolyConecta.Infrastructure.Persistence.Configurations.Plataforma;

public sealed class StateTransitionLogConfiguration : IEntityTypeConfiguration<StateTransitionLog>
{
    public void Configure(EntityTypeBuilder<StateTransitionLog> builder)
    {
        builder.ToTable("state_transition_log", "plt");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FromState).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ToState).HasMaxLength(50).IsRequired();
        builder.Property(x => x.UserName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Role).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
    }
}

public sealed class ReferenceSequenceConfiguration : IEntityTypeConfiguration<ReferenceSequence>
{
    public void Configure(EntityTypeBuilder<ReferenceSequence> builder)
    {
        builder.ToTable("reference_sequence", "plt");
        builder.HasKey(x => x.DocumentType);
        builder.Property(x => x.DocumentType).HasMaxLength(50);
        builder.Property(x => x.Prefix).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ResetRule).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.CurrentPeriod).HasMaxLength(7);

        // Rollos de extrusión capturados por la API previa a F0 (RollsController). F4 define la
        // nomenclatura real de los lotes (D-54) y sus propias secuencias.
        builder.HasData(new
        {
            DocumentType = "ROLLO_EXTRUSION",
            Prefix = "EX-{linea}-{yy}",
            Padding = 6,
            NextNumber = 1L,
            ResetRule = ResetRule.Anual,
            CurrentPeriod = (string?)null,
        });
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_message", "plt");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sequence).UseIdentityColumn().ValueGeneratedOnAdd();
        builder.Property(x => x.Sequence).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        builder.HasIndex(x => x.Sequence).IsUnique();
        builder.Property(x => x.CommandType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Variant).HasMaxLength(30);
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LockKeys)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>(),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode(StringComparison.Ordinal))),
                    v => v.ToList()))
            .IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(12);
        builder.HasIndex(x => new { x.Status, x.Sequence });
        builder.Property(x => x.BridgeTransactionId).HasMaxLength(64);
        builder.Property(x => x.LastErrorCode).HasMaxLength(60);
        builder.Property(x => x.LastErrorMessage).HasMaxLength(500);
    }
}
