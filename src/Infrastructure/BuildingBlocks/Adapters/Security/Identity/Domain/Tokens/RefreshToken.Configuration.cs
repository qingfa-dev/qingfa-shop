using Application.BuildingBlocks.Security.Authentication.Tokens.Domain;

using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Tokens;

/// <summary>Persists application refresh tokens in the identity schema.</summary>
public sealed class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(IdentitySchema.TableNames.RefreshTokens, IdentitySchema.Name);
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(RefreshTokenConstant.Constraints.TokenHash.MaxLength);

        builder.Property(token => token.UserId)
            .IsRequired()
            .HasMaxLength(RefreshTokenConstant.Constraints.UserId.MaxLength);

        builder.Property(token => token.TokenFamilyId).IsRequired();
        builder.Property(token => token.CreatedAtUtc).IsRequired();
        builder.Property(token => token.ExpiresAtUtc).IsRequired();
        builder.Property(token => token.RevocationReason);

        builder.HasOne(token => token.ReplacedByToken)
            .WithMany()
            .HasForeignKey("ReplacedByTokenId")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.TokenFamilyId);
    }
}
