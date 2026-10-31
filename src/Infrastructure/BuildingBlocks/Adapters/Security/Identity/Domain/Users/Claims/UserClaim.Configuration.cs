using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Shared;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Claims;

public sealed class UserClaimEntityConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        // Table
        builder.ToTable(IdentitySchema.TableNames.UserClaims, IdentitySchema.Name);

        // Key
        builder.HasKey(c => c.Id);

        // Relationship
        builder.HasOne(uc => uc.User)
            .WithMany(u => u.Claims)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
