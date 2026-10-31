using Application.Modules.Payments.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Maps the payment_webhook_events table.</summary>
public sealed class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("payment_webhook_events", PaymentPersistenceSchemaCatalog.SchemaName);
        builder.HasKey(model => model.Id);

        builder.Property(model => model.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(model => model.Provider).HasColumnName("provider").HasMaxLength(16).IsRequired();
        builder.Property(model => model.ProviderEventId).HasColumnName("provider_event_id").HasMaxLength(128).IsRequired();
        builder.Property(model => model.EventType).HasColumnName("event_type").HasMaxLength(64).IsRequired();
        builder.Property(model => model.RawBody).HasColumnName("raw_body").IsRequired();
        builder.Property(model => model.Outcome).HasColumnName("outcome").HasMaxLength(32).IsRequired();
        builder.Property(model => model.ReceivedAt).HasColumnName("received_at");
        builder.Property(model => model.ProcessedAt).HasColumnName("processed_at");

        builder.HasIndex(model => new { model.Provider, model.ProviderEventId }).IsUnique();
        builder.HasIndex(model => model.ReceivedAt);
    }
}
