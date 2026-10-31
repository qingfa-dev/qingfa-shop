using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Refunds.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Maps the payment_refunds projection table.</summary>
public sealed class RefundReadModelConfiguration : IEntityTypeConfiguration<RefundReadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefundReadModel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("payment_refunds", PaymentPersistenceSchemaCatalog.SchemaName);
        builder.HasKey(model => model.Id);

        builder.Property(model => model.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(model => model.PaymentId).HasColumnName("payment_id").IsRequired();
        builder.Property(model => model.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
        builder.Property(model => model.Amount).HasColumnName("amount").HasPrecision(19, 4);
        builder.Property(model => model.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(model => model.ProviderReference).HasColumnName("provider_reference").HasMaxLength(128);
        builder.Property(model => model.Reason).HasColumnName("reason").HasMaxLength(256).IsRequired();
        builder.Property(model => model.FailureCode).HasColumnName("failure_code").HasMaxLength(64);
        builder.Property(model => model.FailureMessage).HasColumnName("failure_message").HasMaxLength(512);
        builder.Property(model => model.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        builder.Property(model => model.CreatedAt).HasColumnName("created_at");
        builder.Property(model => model.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(model => model.PaymentId);
        builder.HasIndex(model => new { model.PaymentId, model.IdempotencyKey }).IsUnique();
    }
}
