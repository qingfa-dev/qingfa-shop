using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Maps the payments projection table.</summary>
public sealed class PaymentReadModelConfiguration : IEntityTypeConfiguration<PaymentReadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaymentReadModel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("payments", PaymentPersistenceSchemaCatalog.SchemaName);
        builder.HasKey(model => model.Id);

        builder.Property(model => model.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(model => model.Provider).HasColumnName("provider").HasMaxLength(16).IsRequired();
        builder.Property(model => model.ProviderReference).HasColumnName("provider_reference").HasMaxLength(128);
        builder.Property(model => model.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(model => model.Amount).HasColumnName("amount").HasPrecision(19, 4);
        builder.Property(model => model.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(model => model.CapturedAmount).HasColumnName("captured_amount").HasPrecision(19, 4);
        builder.Property(model => model.RefundedAmount).HasColumnName("refunded_amount").HasPrecision(19, 4);
        builder.Property(model => model.ExternalReference).HasColumnName("external_reference").HasMaxLength(128).IsRequired();
        builder.Property(model => model.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        builder.Property(model => model.IdempotencyFingerprint).HasColumnName("idempotency_fingerprint").HasMaxLength(64).IsRequired();
        builder.Property(model => model.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(model => model.ApprovalUrl).HasColumnName("approval_url").HasMaxLength(2048);
        builder.Property(model => model.ExpiresAt).HasColumnName("expires_at");
        builder.Property(model => model.CreatedAt).HasColumnName("created_at");
        builder.Property(model => model.UpdatedAt).HasColumnName("updated_at");
        builder.Property(model => model.FailureCode).HasColumnName("failure_code").HasMaxLength(64);
        builder.Property(model => model.FailureMessage).HasColumnName("failure_message").HasMaxLength(512);
        builder.Property(model => model.Version).HasColumnName("version");

        builder.HasIndex(model => new { model.CustomerId, model.IdempotencyKey }).IsUnique();
        builder.HasIndex(model => new { model.Provider, model.ProviderReference }).IsUnique();
        builder.HasIndex(model => new { model.CustomerId, model.Provider, model.ExternalReference });
        builder.HasIndex(model => model.Status);
        builder.HasIndex(model => model.CreatedAt);
    }
}
