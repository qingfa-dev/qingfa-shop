using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

/// <summary>Configures the identity user aggregate and its child collections.</summary>
public sealed class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(IdentitySchema.TableNames.Users, IdentitySchema.Name);
        builder.Ignore(user => user.Events);

        builder.Property(user => user.FirstName)
            .IsRequired()
            .HasMaxLength(UserConstant.Constraints.Name.MaxFirstNameLength);

        builder.Property(user => user.LastName)
            .HasMaxLength(UserConstant.Constraints.Name.MaxLastNameLength);

        builder.Property(user => user.CreatedBy).HasMaxLength(IdentitySchemaConstant.Columns.Actor.MaxLength);
        builder.Property(user => user.ModifiedBy).HasMaxLength(IdentitySchemaConstant.Columns.Actor.MaxLength);

        builder.HasIndex(user => user.NormalizedUserName).IsUnique();
        builder.HasIndex(user => user.NormalizedEmail);

        builder.HasMany(user => user.Claims)
            .WithOne(claim => claim.User)
            .HasForeignKey(claim => claim.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.UserRoles)
            .WithOne(userRole => userRole.User)
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.UserLogins)
            .WithOne(login => login.User)
            .HasForeignKey(login => login.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.UserTokens)
            .WithOne(token => token.User)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.Passkeys)
            .WithOne(passkey => passkey.User)
            .HasForeignKey(passkey => passkey.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
