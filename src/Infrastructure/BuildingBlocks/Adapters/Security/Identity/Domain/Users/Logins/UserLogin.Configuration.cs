using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Shared;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Logins;

public sealed class UserLoginEntityConfiguration : IEntityTypeConfiguration<UserLogin>
{
    public void Configure(EntityTypeBuilder<UserLogin> builder)
    {
        // Table
        builder.ToTable(IdentitySchema.TableNames.UserLogins, IdentitySchema.Name);

        // Key
        builder.HasKey(l => new { l.LoginProvider, l.ProviderKey });

        // Relationship
        builder.HasOne(ul => ul.User)
            .WithMany(u => u.UserLogins)
            .HasForeignKey(ul => ul.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
