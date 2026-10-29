using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Seeders;
using Application.Common.Security.Authorization.Domain;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Security.Identity.Domain.Roles;
using Infrastructure.Security.Identity.Domain.Roles.Claims;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security.Identity.Seeders;

/// <summary>Seeds the default application roles and their identity permissions.</summary>
public sealed class RoleSeeder(ApplicationDbContext dbContext) : IPersistenceSeeder<Role, Guid>
{
    /// <inheritdoc />
    public string Name => "seeder:identity-roles";

    /// <inheritdoc />
    public string DisplayName => "Seed Identity Roles";

    /// <inheritdoc />
    public int Order => 30;

    /// <inheritdoc />
    public string[] DependsOn => [];

    /// <inheritdoc />
    public bool IsRequired => false;

    Type? IPersistenceSeeder.EntityType => typeof(Role);

    Type? IPersistenceSeeder.KeyType => typeof(Guid);


    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<Role>().AnyAsync(cancellationToken).ConfigureAwait(false))
            return Result.Ok();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Role admin = CreateRole(RoleConstant.Defaults.Admin, "System administrator", now);
        Role manager = CreateRole(RoleConstant.Defaults.Manager, "Operational manager", now);
        Role user = CreateRole(RoleConstant.Defaults.User, "Standard user", now);

        dbContext.Set<Role>().AddRange(admin, manager, user);
        dbContext.Set<RoleClaim>().AddRange(
            CreateClaims(admin.Id, RoleConstant.RolePermissions.Admin));
        dbContext.Set<RoleClaim>().AddRange(
            CreateClaims(manager.Id, RoleConstant.RolePermissions.Manager));
        dbContext.Set<RoleClaim>().AddRange(
            CreateClaims(user.Id, RoleConstant.RolePermissions.User));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InitializationOperation>>> PreviewAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        bool hasRoles = await dbContext.Set<Role>().AnyAsync(cancellationToken).ConfigureAwait(false);
        if (hasRoles)
        {
            return Result<IReadOnlyList<InitializationOperation>>.Ok(
            [
                new InitializationOperation
                {
                    StepName = Name,
                    Description = "Identity roles already exist. No changes needed.",
                    Type = InitializationOperationType.DataCheck
                }
            ]);
        }

        return Result<IReadOnlyList<InitializationOperation>>.Ok(
        [
            .. RoleConstant.Defaults.All.Select(role => new InitializationOperation
            {
                StepName = Name,
                Description = $"Insert identity role '{role}' with default permissions.",
                Type = InitializationOperationType.DataChange,
                EstimatedRowsAffected = 1
            })
        ]);
    }

    private static Role CreateRole(string name, string description, DateTimeOffset now)
    {
        Result<Role> result = Role.Create(name, description);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to create role '{name}': {result.Errors[0].Code} - {result.Errors[0].Message}");
        }

        Role role = result.Value;
        role.NormalizedName = name.ToUpperInvariant();
        role.IsSystem = true;
        role.CreatedAtUtc = now;
        role.CreatedBy = "System";
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        return role;
    }

    private static IEnumerable<RoleClaim> CreateClaims(
        Guid roleId,
        IReadOnlyList<PermissionMetadata> permissions) =>
        permissions.Select(permission => new RoleClaim
        {
            RoleId = roleId,
            ClaimType = PermissionMetadataConstant.ClaimType,
            ClaimValue = permission.Identifier
        });
}
