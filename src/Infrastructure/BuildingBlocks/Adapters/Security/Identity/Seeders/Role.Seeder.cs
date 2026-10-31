using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.Initialization.Seeders;
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles.Claims;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Seeders;

/// <summary>Seeds the default application roles and keeps their permission claims in sync.</summary>
/// <remarks>
/// The seeder is additive: it creates missing roles and adds missing default permissions, but it
/// never removes a claim. Custom grants therefore survive a seed run while newly introduced
/// catalog permissions reach existing installations.
/// </remarks>
public sealed class RoleSeeder(ApplicationDbContext dbContext, IPermissionCatalog catalog) : IPersistenceSeeder<Role, Guid>
{
    private readonly (string Name, string Description, IReadOnlyList<PermissionMetadata> Permissions)[] _defaults =
    [
        (RoleConstant.Defaults.Admin, "System administrator", catalog.Admin),
        (RoleConstant.Defaults.Manager, "Operational manager", catalog.Manager),
        (RoleConstant.Defaults.User, "Standard user", catalog.User),
    ];

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
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<Role> existingRoles = await dbContext.Set<Role>()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rolesByName = existingRoles.ToDictionary(
            role => role.Name!,
            StringComparer.OrdinalIgnoreCase);

        foreach ((string name, string description, IReadOnlyList<PermissionMetadata> permissions) in _defaults)
        {
            if (!rolesByName.TryGetValue(name, out Role? role))
            {
                role = CreateRole(name, description, now);
                dbContext.Set<Role>().Add(role);
                rolesByName[name] = role;
            }

            await AddMissingClaimsAsync(role, permissions, cancellationToken).ConfigureAwait(false);
        }

        if (dbContext.ChangeTracker.HasChanges())
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InitializationOperation>>> PreviewAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        List<Role> existingRoles = await dbContext.Set<Role>()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rolesByName = existingRoles.ToDictionary(
            role => role.Name!,
            StringComparer.OrdinalIgnoreCase);

        var operations = new List<InitializationOperation>(_defaults.Length);

        foreach ((string name, string description, IReadOnlyList<PermissionMetadata> permissions) in _defaults)
        {
            if (!rolesByName.TryGetValue(name, out Role? role))
            {
                operations.Add(new InitializationOperation
                {
                    StepName = Name,
                    Description = $"Insert identity role '{name}' with {permissions.Count} default permissions.",
                    Type = InitializationOperationType.DataChange,
                    EstimatedRowsAffected = 1 + permissions.Count,
                });
                continue;
            }

            int missing = await CountMissingClaimsAsync(role, permissions, cancellationToken)
                .ConfigureAwait(false);
            if (missing > 0)
            {
                operations.Add(new InitializationOperation
                {
                    StepName = Name,
                    Description = $"Add {missing} missing permission claim(s) to role '{name}'.",
                    Type = InitializationOperationType.DataChange,
                    EstimatedRowsAffected = missing,
                });
            }
        }

        if (operations.Count == 0)
        {
            operations.Add(new InitializationOperation
            {
                StepName = Name,
                Description = "Identity roles and permission claims are up to date. No changes needed.",
                Type = InitializationOperationType.DataCheck,
            });
        }

        return Result<IReadOnlyList<InitializationOperation>>.Ok(operations);
    }

    private async Task AddMissingClaimsAsync(
        Role role,
        IReadOnlyList<PermissionMetadata> permissions,
        CancellationToken cancellationToken)
    {
        HashSet<string> existing = await ExistingClaimValuesAsync(role.Id, cancellationToken)
            .ConfigureAwait(false);

        var missing = permissions
            .Select(permission => permission.Identifier)
            .Where(identifier => !existing.Contains(identifier))
            .Select(identifier => new RoleClaim
            {
                RoleId = role.Id,
                ClaimType = PermissionMetadataConstant.ClaimType,
                ClaimValue = identifier,
            })
            .ToList();

        if (missing.Count == 0)
            return;

        dbContext.Set<RoleClaim>().AddRange(missing);
    }

    private async Task<int> CountMissingClaimsAsync(
        Role role,
        IReadOnlyList<PermissionMetadata> permissions,
        CancellationToken cancellationToken)
    {
        HashSet<string> existing = await ExistingClaimValuesAsync(role.Id, cancellationToken)
            .ConfigureAwait(false);

        return permissions.Count(permission => !existing.Contains(permission.Identifier));
    }

    private async Task<HashSet<string>> ExistingClaimValuesAsync(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        List<string> values = await dbContext.Set<RoleClaim>()
            .AsNoTracking()
            .Where(claim =>
                claim.RoleId == roleId &&
                claim.ClaimType == PermissionMetadataConstant.ClaimType)
            .Select(claim => claim.ClaimValue!)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
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
}
