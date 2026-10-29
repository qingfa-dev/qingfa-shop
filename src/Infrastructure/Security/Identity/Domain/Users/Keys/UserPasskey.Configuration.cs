using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Infrastructure.Security.Identity.Domain.Shared;

namespace Infrastructure.Security.Identity.Domain.Users.Keys;

public sealed class UserPasskeyConfiguration : IEntityTypeConfiguration<UserPasskey>
{
    public void Configure(EntityTypeBuilder<UserPasskey> builder)
    {
        // Key
        builder.Property<Guid>("Id").ValueGeneratedOnAdd();
        builder.HasKey("Id");

        // Table
        builder.ToTable(IdentitySchema.TableNames.Passkeys, IdentitySchema.Name);

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany(u => u.Passkeys)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(passkey => passkey.Data, data =>
            data.ToTable(IdentitySchema.TableNames.PasskeyData, IdentitySchema.Name));

    }
}
