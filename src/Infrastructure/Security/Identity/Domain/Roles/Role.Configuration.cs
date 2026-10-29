using Infrastructure.Security.Identity.Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Security.Identity.Domain.Roles;

/// <summary>Configures identity roles and their permission claims.</summary>
public sealed class RoleEntityConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable(IdentitySchema.TableNames.Roles, IdentitySchema.Name);
        builder.HasKey(role => role.Id);
        builder.Ignore(role => role.Events);

        builder.Property(role => role.Name)
            .IsRequired()
            .HasMaxLength(RoleConstant.Constraints.Name.MaxLength);

        builder.Property(role => role.NormalizedName)
            .IsRequired()
            .HasMaxLength(RoleConstant.Constraints.Name.MaxLength);

        builder.Property(role => role.Description)
            .HasMaxLength(RoleConstant.Constraints.Description.MaxLength);

        builder.Property(role => role.CreatedBy).HasMaxLength(MaxActorLength);
        builder.Property(role => role.ModifiedBy).HasMaxLength(MaxActorLength);

        builder.HasIndex(role => role.NormalizedName).IsUnique();

        builder.HasMany(role => role.UserRoles)
            .WithOne(userRole => userRole.Role)
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(role => role.RoleClaims)
            .WithOne(roleClaim => roleClaim.Role)
            .HasForeignKey(roleClaim => roleClaim.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private const int MaxActorLength = 450;
}
