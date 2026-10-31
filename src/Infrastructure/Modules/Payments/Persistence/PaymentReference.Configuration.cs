using Application.Modules.Payments.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Maps the payment_references table.</summary>
public sealed class PaymentReferenceConfiguration : IEntityTypeConfiguration<PaymentReference>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaymentReference> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("payment_references", PaymentPersistenceSchemaCatalog.SchemaName);
        builder.HasKey(model => model.Id);

        builder.Property(model => model.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(model => model.Provider).HasColumnName("provider").HasMaxLength(16).IsRequired();
        builder.Property(model => model.ProviderReference).HasColumnName("provider_reference").HasMaxLength(128).IsRequired();
        builder.Property(model => model.PaymentId).HasColumnName("payment_id").IsRequired();
        builder.Property(model => model.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(model => new { model.Provider, model.ProviderReference }).IsUnique();
    }
}
