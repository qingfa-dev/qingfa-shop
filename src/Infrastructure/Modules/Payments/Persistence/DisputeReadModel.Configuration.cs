using Application.Modules.Payments.Disputes.Models;
using Application.Modules.Payments.Persistence;

using Core.Modules.Payments.Intents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Maps the payment_disputes projection table.</summary>
public sealed class DisputeReadModelConfiguration : IEntityTypeConfiguration<DisputeReadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DisputeReadModel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("payment_disputes", PaymentPersistenceSchemaCatalog.SchemaName);
        builder.HasKey(model => model.Id);

        builder.Property(model => model.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(model => model.PaymentId).HasColumnName("payment_id").IsRequired();
        builder.Property(model => model.ProviderReference).HasColumnName("provider_reference")
            .HasMaxLength(PaymentConstant.Constraints.ProviderReferenceMaxLength).IsRequired();
        builder.Property(model => model.Amount).HasColumnName("amount")
            .HasPrecision(PaymentConstant.Constraints.AmountPrecision, PaymentConstant.Constraints.AmountScale);
        builder.Property(model => model.Currency).HasColumnName("currency")
            .HasMaxLength(PaymentConstant.Constraints.CurrencyMaxLength).IsRequired();
        builder.Property(model => model.Reason).HasColumnName("reason")
            .HasMaxLength(PaymentConstant.Constraints.ReasonMaxLength).IsRequired();
        builder.Property(model => model.Status).HasColumnName("status")
            .HasMaxLength(PaymentConstant.Constraints.StatusMaxLength).IsRequired();
        builder.Property(model => model.OpenedAt).HasColumnName("opened_at");
        builder.Property(model => model.ResolvedAt).HasColumnName("resolved_at");

        builder.HasIndex(model => model.PaymentId);
        builder.HasIndex(model => new { model.ProviderReference }).IsUnique();
    }
}
