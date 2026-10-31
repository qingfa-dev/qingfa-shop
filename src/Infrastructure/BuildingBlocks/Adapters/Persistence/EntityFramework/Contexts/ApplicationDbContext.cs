using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles.Claims;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Claims;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Keys;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Logins;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Tokens;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;

/// <summary>
/// Single EF Core DbContext for the application, including ASP.NET Core Identity.
/// Entity types are discovered from the <c>IEntityTypeConfiguration</c>
/// implementations in the assembly; no DbSet properties are required.
/// </summary>
public sealed class ApplicationDbContext
    : IdentityDbContext<
        User,
        Role,
        Guid,
        UserClaim,
        UserRole,
        UserLogin,
        RoleClaim,
        UserToken,
        UserPasskey>
{
    /// <summary>Creates a new ApplicationDbContext with the given options.</summary>
    /// <param name="options">The EF Core DbContext options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyPersistenceConventions(Database.ProviderName);
    }
}
