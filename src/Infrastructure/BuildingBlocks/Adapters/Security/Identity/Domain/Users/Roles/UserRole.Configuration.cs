using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Shared;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Roles;

public sealed class UserRoleEntityConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // Table
        builder.ToTable(IdentitySchema.TableNames.UserRoles, IdentitySchema.Name);
        builder.HasKey(userRole => new { userRole.UserId, userRole.RoleId });

        // Relationships
        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
