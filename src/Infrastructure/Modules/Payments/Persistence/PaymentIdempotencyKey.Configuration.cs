using Application.Modules.Payments.Persistence;

using Core.Modules.Payments.Intents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Maps the payment_idempotency_keys table.</summary>
public sealed class PaymentIdempotencyKeyConfiguration : IEntityTypeConfiguration<PaymentIdempotencyKey>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaymentIdempotencyKey> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("payment_idempotency_keys", PaymentPersistenceSchemaCatalog.SchemaName);
        builder.HasKey(model => model.Id);

        builder.Property(model => model.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(model => model.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(model => model.Key).HasColumnName("idempotency_key").HasMaxLength(PaymentConstant.Constraints.IdempotencyKeyMaxLength).IsRequired();
        builder.Property(model => model.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64).IsRequired();
        builder.Property(model => model.PaymentId).HasColumnName("payment_id").IsRequired();
        builder.Property(model => model.CreatedAt).HasColumnName("created_at");
        builder.Property(model => model.Currency).HasColumnName("currency").HasMaxLength(PaymentConstant.Constraints.CurrencyMaxLength).IsRequired();

        builder.HasIndex(model => new { model.CustomerId, model.Key }).IsUnique();
    }
}
