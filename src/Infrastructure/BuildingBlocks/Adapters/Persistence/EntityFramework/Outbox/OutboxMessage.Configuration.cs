using Application.BuildingBlocks.Messaging.Outbox;
using Application.BuildingBlocks.Persistence;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Maps <see cref="OutboxMessage"/> to the <c>messaging.outbox_messages</c> table.</summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <summary>Applies the outbox table mapping, indexes, and lease fencing.</summary>
    /// <param name="builder">The entity type builder for <see cref="OutboxMessage"/>.</param>
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        // Map: the outbox lives in its own schema so reset and retention scope it alone.
        builder.ToTable("outbox_messages", PersistenceConstant.Schema.Messaging);

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(message => message.Kind)
            .HasColumnName("kind")
            .HasEnumConversion<OutboxMessageKind>()
            .IsRequired();

        builder.Property(message => message.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(message => message.Version)
            .HasColumnName("version")
            .IsRequired();

        // Map: the serialized event, stored as JSONB for queryability.
        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.Status)
            .HasColumnName("status")
            .HasEnumConversion<OutboxMessageStatus>()
            .IsRequired();

        builder.Property(message => message.OccurredOnUtc)
            .HasColumnName("occurred_on_utc")
            .IsRequired();

        builder.Property(message => message.AvailableOnUtc)
            .HasColumnName("available_on_utc")
            .IsRequired();

        builder.Property(message => message.LockedUntilUtc)
            .HasColumnName("locked_until_utc");

        // Lock: the lease is the fencing token; a tracked update must not overwrite a newer one.
        // The token is model-only; PostgreSQL needs no extra column for it.
        builder.Property(message => message.LeaseId)
            .HasColumnName("lease_id")
            .IsConcurrencyToken();

        builder.Property(message => message.ProcessedOnUtc)
            .HasColumnName("processed_on_utc");

        builder.Property(message => message.Attempts)
            .HasColumnName("attempts")
            .IsRequired();

        builder.Property(message => message.LastError)
            .HasColumnName("last_error");

        builder.Property(message => message.CorrelationId)
            .HasColumnName("correlation_id");

        builder.Property(message => message.CausationId)
            .HasColumnName("causation_id");

        builder.Property(message => message.TenantId)
            .HasColumnName("tenant_id");

        // Lock: pending rows become available in occurrence order.
        builder.HasIndex(message => new
        {
            message.Status,
            message.AvailableOnUtc,
            message.OccurredOnUtc,
        });

        // Update: retention deletes processed rows by age.
        builder.HasIndex(message => message.ProcessedOnUtc);
    }
}