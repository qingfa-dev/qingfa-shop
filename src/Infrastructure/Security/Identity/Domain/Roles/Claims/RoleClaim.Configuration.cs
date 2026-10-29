using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Infrastructure.Security.Identity.Domain.Shared;

namespace Infrastructure.Security.Identity.Domain.Roles.Claims;

public sealed class RoleClaimEntityConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        // Table
        builder.ToTable(IdentitySchema.TableNames.RoleClaims, IdentitySchema.Name);

        // Key
        builder.HasKey(c => c.Id);

        // Relationship
        builder.HasOne(rc => rc.Role)
            .WithMany(r => r.RoleClaims)
            .HasForeignKey(rc => rc.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
