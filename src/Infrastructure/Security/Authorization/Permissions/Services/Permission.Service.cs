using Application.Common.Security.Authorization.Permissions.Caches;
using Application.Common.Security.Authorization.Permissions.Services;
using Application.Common.Security.Authorization.Permissions.Store;

namespace Infrastructure.Security.Authorization.Permissions.Services;

/// <summary>Resolves effective user permissions by merging role-based and direct permissions through a cache-aside pattern.</summary>
// Invariant: Effective permissions are the union of role permissions + direct user permissions; cache is invalidated on every store mutation.
// Context: Permission resolution must never throw in the authorization pipeline — empty set is the safe default (Threat TMT-AUTH-001).
// Boundary: Service → Cache | Store — orchestrates between two data sources; never calls the database directly.
// Contract: pre=cache!=null && store!=null && logger!=null
public sealed partial class PermissionService(
    IPermissionCache cache,
    IPermissionStore store,
    ILogger<PermissionService> logger)
    : IPermissionService
{
    /// <summary>Resolves effective permissions for a user by merging role and direct permissions.</summary>
    // Contract: pre=userId!=Guid.Empty, post=return.IsSuccess && return.Value!=null, throws=never — returns empty set on failure
    public async Task<Result<HashSet<string>>> GetEffectiveUserPermissionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return Result<HashSet<string>>.Fail(PermissionServiceOutcome.Failure.UserIdRequired);

        try
        {
            // Cache: probe user cache for a pre-resolved permission set (module boundary: Service → Cache)
            Result<PermissionCacheEntry> cacheResult = await cache.GetAsync(userId, ct);

            // Fallback: a cache read failure degrades to the store instead of failing authorization open.
            if (cacheResult.IsFailure)
            {
                Loggers.LogUserCacheReadFailed(
                    logger,
                    userId,
                    Describe(cacheResult.Errors));
            }
            else if (cacheResult.Value.IsHit)
            {
                HashSet<string> cached = cacheResult.Value.Permissions!;
                Loggers.LogEffectivePermissionsResolved(logger, cached.Count, userId);
                return Result<HashSet<string>>.Ok(cached);
            }

            // Call: fetch role IDs assigned to the user from store (module boundary: Service → Store)
            Result<HashSet<Guid>> rolesResult = await store.GetUserRoleIdsAsync(userId, ct);
            HashSet<Guid> roleIds = rolesResult.IsSuccess ? rolesResult.Value : [];

            if (rolesResult.IsFailure)
                Loggers.LogUserRoleIdsReadFailed(logger, userId, Describe(rolesResult.Errors));

            // Call: fetch direct user permissions (non-role claims) from store
            Result<HashSet<string>> userDirectResult = await store.GetUserDirectPermissionsAsync(userId, ct);
            HashSet<string> userDirectPerms = userDirectResult.IsSuccess ? userDirectResult.Value : [];

            if (userDirectResult.IsFailure)
                Loggers.LogUserDirectReadFailed(logger, userId, Describe(userDirectResult.Errors));

            // Compute: resolve permissions for each role sequentially — shared DbContext cannot parallelize
            var rolePermissionSets = new List<HashSet<string>>(roleIds.Count);
            foreach (Guid roleId in roleIds)
            {
                Result<PermissionCacheEntry> roleCacheResult = await cache.GetRoleAsync(roleId, ct);

                if (roleCacheResult.IsFailure)
                {
                    Loggers.LogRoleCacheReadFailed(
                        logger,
                        roleId,
                        Describe(roleCacheResult.Errors));
                }
                else if (roleCacheResult.Value.IsHit)
                {
                    rolePermissionSets.Add(roleCacheResult.Value.Permissions!);
                    continue;
                }

                Result<HashSet<string>> roleStoreResult = await store.GetRolePermissionsAsync(roleId, ct);
                HashSet<string> rolePerms = roleStoreResult.IsSuccess ? roleStoreResult.Value : [];

                if (roleStoreResult.IsFailure)
                    Loggers.LogRoleStoreReadFailed(logger, roleId, Describe(roleStoreResult.Errors));

                if (roleStoreResult.IsSuccess)
                {
                    Result writeResult = await cache.SetRoleAsync(roleId, rolePerms, ct);

                    if (writeResult.IsFailure)
                    {
                        Loggers.LogRoleCacheWriteFailed(
                            logger,
                            roleId,
                            Describe(writeResult.Errors));
                    }
                }

                rolePermissionSets.Add(rolePerms);
            }

            // Merge: union all role permission sets with direct user permissions — case-insensitive dedup
            var permissions = new HashSet<string>(userDirectPerms, StringComparer.OrdinalIgnoreCase);
            foreach (HashSet<string>? set in rolePermissionSets)
                permissions.UnionWith(set);

            // Cache: populate user cache with role tags for targeted invalidation on role change
            Result userWriteResult = await cache.SetUserAsync(userId, permissions, roleIds, ct);

            if (userWriteResult.IsFailure)
            {
                Loggers.LogUserCacheWriteFailed(
                    logger,
                    userId,
                    Describe(userWriteResult.Errors));
            }

            Loggers.LogEffectivePermissionsResolved(logger, permissions.Count, userId);
            return Result<HashSet<string>>.Ok(permissions);
        }
        catch (Exception ex)
        {
            // Catch: return empty set on unexpected failure — never throw in auth pipeline per TMT-AUTH-001
            Loggers.LogUserResolutionFailed(logger, userId, ex.Message);

            // Fallback: always return success with empty set to prevent cascading authorization failures
            return Result<HashSet<string>>.Ok(new HashSet<string>());
        }
    }

    /// <summary>Resolves permissions for a role using the cache-aside pattern.</summary>
    // Contract: pre=roleId!=Guid.Empty, post=return.IsSuccess && return.Value!=null, throws=never
    public async Task<Result<HashSet<string>>> GetRolePermissionsAsync(
        Guid roleId,
        CancellationToken ct = default)
    {
        if (roleId == Guid.Empty)
            return Result<HashSet<string>>.Fail(PermissionServiceOutcome.Failure.RoleIdRequired);

        try
        {
            // Cache: probe role cache first (module boundary: Service → Cache)
            Result<PermissionCacheEntry> roleCacheResult = await cache.GetRoleAsync(roleId, ct);

            if (roleCacheResult.IsFailure)
            {
                Loggers.LogRoleCacheReadFailed(
                    logger,
                    roleId,
                    Describe(roleCacheResult.Errors));
            }
            else if (roleCacheResult.Value.IsHit)
            {
                HashSet<string> cached = roleCacheResult.Value.Permissions!;
                Loggers.LogRolePermissionsResolved(logger, cached.Count, roleId);
                return Result<HashSet<string>>.Ok(cached);
            }

            // Call: query store on cache miss (module boundary: Service → Store)
            Result<HashSet<string>> roleStoreResult = await store.GetRolePermissionsAsync(roleId, ct);
            HashSet<string> rolePerms = roleStoreResult.IsSuccess ? roleStoreResult.Value : [];

            // Cache: populate role cache for subsequent requests
            if (roleStoreResult.IsSuccess)
            {
                Result writeResult = await cache.SetRoleAsync(roleId, rolePerms, ct);

                if (writeResult.IsFailure)
                {
                    Loggers.LogRoleCacheWriteFailed(
                        logger,
                        roleId,
                        Describe(writeResult.Errors));
                }
            }

            Loggers.LogRolePermissionsResolved(logger, rolePerms.Count, roleId);
            return Result<HashSet<string>>.Ok(rolePerms);
        }
        catch (Exception ex)
        {
            // Catch: return empty set on failure — prevents auth pipeline crash
            Loggers.LogRoleResolutionFailed(logger, roleId, ex.Message);
            return Result<HashSet<string>>.Ok(new HashSet<string>());
        }
    }

    /// <summary>Verifies that a user has ALL specified permissions.</summary>
    // Contract: pre=userId!=Guid.Empty && permissions!=null, post=return.IsSuccess, throws=never
    public async Task<Result<bool>> HasAllPermissionsAsync(
        Guid userId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        if (permissions is null)
            return Result<bool>.Fail(PermissionServiceOutcome.Failure.PermissionsRequired);

        // Call: resolve effective user permissions
        Result<HashSet<string>> userPermsResult = await GetEffectiveUserPermissionsAsync(userId, ct);

        // Guard: propagate failure early if permission resolution failed
        if (userPermsResult.IsFailure)
            return Result<bool>.Fail(userPermsResult.Errors);

        bool hasAll = permissions.All(permission => userPermsResult.Value.Contains(permission));
        return Result<bool>.Ok(hasAll);
    }

    /// <summary>Verifies that a user has ANY of the specified permissions.</summary>
    // Contract: pre=userId!=Guid.Empty && permissions!=null, post=return.IsSuccess, throws=never
    public async Task<Result<bool>> HasAnyPermissionsAsync(
        Guid userId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        if (permissions is null)
            return Result<bool>.Fail(PermissionServiceOutcome.Failure.PermissionsRequired);

        // Call: resolve effective user permissions
        Result<HashSet<string>> userPermsResult = await GetEffectiveUserPermissionsAsync(userId, ct);

        // Guard: propagate failure early if permission resolution failed
        if (userPermsResult.IsFailure)
            return Result<bool>.Fail(userPermsResult.Errors);

        bool hasAny = permissions.Any(permission => userPermsResult.Value.Contains(permission));
        return Result<bool>.Ok(hasAny);
    }

    /// <summary>Verifies that a role has ALL specified permissions.</summary>
    // Contract: pre=roleId!=Guid.Empty && permissions!=null, post=return.IsSuccess, throws=never
    public async Task<Result<bool>> RoleHasAllPermissionsAsync(
        Guid roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        if (permissions is null)
            return Result<bool>.Fail(PermissionServiceOutcome.Failure.PermissionsRequired);

        // Call: resolve role permissions
        Result<HashSet<string>> rolePermsResult = await GetRolePermissionsAsync(roleId, ct);
        if (rolePermsResult.IsFailure)
            return Result<bool>.Fail(rolePermsResult.Errors);

        bool hasAll = permissions.All(permission => rolePermsResult.Value.Contains(permission));
        return Result<bool>.Ok(hasAll);
    }

    /// <summary>Verifies that a role has ANY of the specified permissions.</summary>
    // Contract: pre=roleId!=Guid.Empty && permissions!=null, post=return.IsSuccess, throws=never
    public async Task<Result<bool>> RoleHasAnyPermissionsAsync(
        Guid roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        if (permissions is null)
            return Result<bool>.Fail(PermissionServiceOutcome.Failure.PermissionsRequired);

        // Call: resolve role permissions
        Result<HashSet<string>> rolePermsResult = await GetRolePermissionsAsync(roleId, ct);
        if (rolePermsResult.IsFailure)
            return Result<bool>.Fail(rolePermsResult.Errors);

        bool hasAny = permissions.Any(permission => rolePermsResult.Value.Contains(permission));
        return Result<bool>.Ok(hasAny);
    }

    /// <summary>Invalidates cached permissions for a role and all users tagged with that role.</summary>
    // Delegate: forward invalidation to cache layer for cascade (role + tagged users)
    public async Task<Result> InvalidateRolePermissionsAsync(
        Guid roleId,
        CancellationToken ct = default)
    {
        if (roleId == Guid.Empty)
            return Result.Fail(PermissionServiceOutcome.Failure.RoleIdRequired);

        Result result = await cache.InvalidateRoleAsync(roleId, ct);

        return result.IsSuccess
            ? result
            : Result.Fail(PermissionServiceOutcome.Failure.RoleInvalidationFailed(roleId, result.Errors));
    }

    /// <summary>Invalidates cached permissions for a single user.</summary>
    // Delegate: forward invalidation to cache layer for single user
    public async Task<Result> InvalidateUserPermissionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return Result.Fail(PermissionServiceOutcome.Failure.UserIdRequired);

        Result result = await cache.InvalidateUserAsync(userId, ct);

        return result.IsSuccess
            ? result
            : Result.Fail(PermissionServiceOutcome.Failure.UserInvalidationFailed(userId, result.Errors));
    }

    /// <summary>Persists new permissions to a role and invalidates the cache for consistency.</summary>
    // Contract: pre=roleId!=Guid.Empty && permissions!=null, post=return.IsSuccess if store succeeds, throws=never
    public async Task<Result> AddRolePermissionsAsync(
        Guid roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        if (roleId == Guid.Empty)
            return Result.Fail(PermissionServiceOutcome.Failure.RoleIdRequired);

        if (permissions is null)
            return Result.Fail(PermissionServiceOutcome.Failure.PermissionsRequired);

        Result addResult = await store.AddRolePermissionsAsync(roleId, permissions, ct);

        if (addResult.IsFailure)
            return addResult;

        // Cache: invalidate role + cascading user caches after successful store mutation
        Result invalidateResult = await cache.InvalidateRoleAsync(roleId, ct);

        return invalidateResult.IsSuccess
            ? Result.Ok()
            : Result.Fail(PermissionServiceOutcome.Failure.RoleInvalidationFailed(
                roleId,
                invalidateResult.Errors));
    }

    /// <summary>Removes permissions from a role and invalidates the cache for consistency.</summary>
    // Contract: pre=roleId!=Guid.Empty && permissions!=null, post=return.IsSuccess if store succeeds, throws=never
    public async Task<Result> RemoveRolePermissionsAsync(
        Guid roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        if (roleId == Guid.Empty)
            return Result.Fail(PermissionServiceOutcome.Failure.RoleIdRequired);

        if (permissions is null)
            return Result.Fail(PermissionServiceOutcome.Failure.PermissionsRequired);

        Result removeResult = await store.RemoveRolePermissionsAsync(roleId, permissions, ct);

        if (removeResult.IsFailure)
            return removeResult;

        Result invalidateResult = await cache.InvalidateRoleAsync(roleId, ct);

        return invalidateResult.IsSuccess
            ? Result.Ok()
            : Result.Fail(PermissionServiceOutcome.Failure.RoleInvalidationFailed(
                roleId,
                invalidateResult.Errors));
    }

    /// <summary>Persists direct permissions to a user and invalidates the user cache.</summary>
    // Contract: pre=userId!=Guid.Empty && permissions!=null, post=return.IsSuccess if store succeeds, throws=never
    public async Task<Result> AddUserDirectPermissionsAsync(
        Guid userId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return Result.Fail(PermissionServiceOutcome.Failure.UserIdRequired);

        if (permissions is null)
            return Result.Fail(PermissionServiceOutcome.Failure.PermissionsRequired);

        Result addResult = await store.AddUserDirectPermissionsAsync(userId, permissions, ct);

        if (addResult.IsFailure)
            return addResult;

        Result invalidateResult = await cache.InvalidateUserAsync(userId, ct);

        return invalidateResult.IsSuccess
            ? Result.Ok()
            : Result.Fail(PermissionServiceOutcome.Failure.UserInvalidationFailed(
                userId,
                invalidateResult.Errors));
    }

    /// <summary>Removes direct permissions from a user and invalidates the user cache.</summary>
    // Contract: pre=userId!=Guid.Empty && permissions!=null, post=return.IsSuccess if store succeeds, throws=never
    public async Task<Result> RemoveUserDirectPermissionsAsync(
        Guid userId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return Result.Fail(PermissionServiceOutcome.Failure.UserIdRequired);

        if (permissions is null)
            return Result.Fail(PermissionServiceOutcome.Failure.PermissionsRequired);

        Result removeResult = await store.RemoveUserDirectPermissionsAsync(userId, permissions, ct);

        if (removeResult.IsFailure)
            return removeResult;

        Result invalidateResult = await cache.InvalidateUserAsync(userId, ct);

        return invalidateResult.IsSuccess
            ? Result.Ok()
            : Result.Fail(PermissionServiceOutcome.Failure.UserInvalidationFailed(
                userId,
                invalidateResult.Errors));
    }

    private static string Describe(IEnumerable<Error> errors) =>
        string.Join("; ", errors.Select(error => $"{error.Code}: {error.Message}"));
}
