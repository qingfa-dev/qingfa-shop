using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Shared;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Tokens;

public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        // Table
        builder.ToTable(IdentitySchema.TableNames.UserTokens, IdentitySchema.Name);
        builder.HasKey(token => new { token.UserId, token.LoginProvider, token.Name });

        // Relationship
        builder.HasOne(ut => ut.User)
            .WithMany(u => u.UserTokens)
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
