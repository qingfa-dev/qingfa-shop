using Application.Modules.Payments.Persistence;

using Core.Modules.Payments.Intents;

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
        builder.Property(model => model.Provider).HasColumnName("provider").HasMaxLength(PaymentConstant.Constraints.ProviderMaxLength).IsRequired();
        builder.Property(model => model.ProviderEventId).HasColumnName("provider_event_id").HasMaxLength(PaymentConstant.Constraints.ProviderEventIdMaxLength).IsRequired();
        builder.Property(model => model.EventType).HasColumnName("event_type").HasMaxLength(PaymentConstant.Constraints.EventTypeMaxLength).IsRequired();
        builder.Property(model => model.RawBody).HasColumnName("raw_body").IsRequired();
        builder.Property(model => model.Outcome).HasColumnName("outcome").HasMaxLength(PaymentConstant.Constraints.StatusMaxLength).IsRequired();
        builder.Property(model => model.ReceivedAt).HasColumnName("received_at");
        builder.Property(model => model.ProcessedAt).HasColumnName("processed_at");

        builder.HasIndex(model => new { model.Provider, model.ProviderEventId }).IsUnique();
        builder.HasIndex(model => model.ReceivedAt);
    }
}
