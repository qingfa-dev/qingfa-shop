using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Application.Common.Caching;
using Application.Common.Security.Authorization.Options;

namespace Application.Common.Security.Authorization.Permissions.Caches;

/// <summary>Two-tier permission cache (local + distributed) with role-tagged invalidation and configurable TTL.</summary>
// Invariant: Cache keys follow perm:user|role:{id}; role invalidation cascades to every user tagged with that role.
// Boundary: Cache → CacheService — delegates to the monadic ICacheService port; never accesses cache infrastructure directly.
// Contract: pre=cacheService!=null && authzOptions!=null && logger!=null
public sealed partial class PermissionCache(
    ICacheService cacheService,
    IOptions<AuthzSetting> authzOptions,
    ILogger<PermissionCache> logger) : IPermissionCache
{
    private PermissionCacheOptions CacheOptions => authzOptions.Value.PermissionCache;

    // Compute: build the entry lifetime from the configured absolute and sliding values.
    private CachingEntryOption CreateEntryOptions() =>
        new()
        {
            Expiration = CacheOptions.AbsoluteExpiration,
            LocalCacheExpiration = CacheOptions.SlidingExpiration,
        };

    // Compute: misses are cached briefly so a failing store is not retried on every request.
    private static CachingEntryOption CreateMissEntryOptions()
    {
        TimeSpan missExpiration = TimeSpan.FromSeconds(
            PermissionCacheConstant.Defaults.MissExpirationSeconds);

        return new CachingEntryOption
        {
            Expiration = missExpiration,
            LocalCacheExpiration = missExpiration,
        };
    }

    /// <summary>Retrieves cached user permissions — returns a miss entry so the caller can fall back to the store.</summary>
    // Contract: pre=userId!=Guid.Empty, post=return.IsSuccess, throws=never
    public async Task<Result<PermissionCacheEntry>> GetAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return Result<PermissionCacheEntry>.Fail(PermissionCacheOutcome.Failure.UserIdRequired);

        // Cache: probe the user entry; the short-lived factory result is a miss marker.
        string key = $"{PermissionCacheConstant.Patterns.UserKeyPrefix}{userId}";

        Result<PermissionCacheEntry> cacheResult = await cacheService.GetOrCreateAsync(
            key,
            _ => ValueTask.FromResult(PermissionCacheEntry.Miss),
            CreateMissEntryOptions(),
            cancellationToken: ct);

        if (cacheResult.IsFailure)
            return Result<PermissionCacheEntry>.Fail(
                PermissionCacheOutcome.Failure.UserReadFailed(userId, cacheResult.Errors));

        PermissionCacheEntry entry = cacheResult.Value;

        if (entry.IsHit)
            Loggers.LogCacheHit(logger, userId);
        else
            Loggers.LogCacheMiss(logger, userId);

        return Result<PermissionCacheEntry>.Ok(entry);
    }

    /// <summary>Persists user permissions with role-tagged invalidation tags for cascade eviction.</summary>
    // Contract: pre=userId!=Guid.Empty && permissions!=null, post=cache entry created, throws=never
    public async Task<Result> SetUserAsync(
        Guid userId,
        HashSet<string> permissions,
        IEnumerable<Guid>? roleIds = null,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return Result.Fail(PermissionCacheOutcome.Failure.UserIdRequired);

        if (permissions is null)
            return Result.Fail(PermissionCacheOutcome.Failure.PermissionsRequired);

        string key = $"{PermissionCacheConstant.Patterns.UserKeyPrefix}{userId}";

        // Compute: tag every user entry with its own key, the global tag, and each role for cascade eviction.
        var tags = new List<string>(capacity: 2)
        {
            $"{PermissionCacheConstant.Patterns.UserKeyPrefix}{userId}",
            PermissionCacheConstant.Patterns.GlobalTag,
        };

        if (roleIds is not null)
            tags.AddRange(roleIds.Select(id => $"{PermissionCacheConstant.Patterns.RoleKeyPrefix}{id}"));

        Result writeResult = await cacheService.SetAsync(
            key,
            PermissionCacheEntry.Hit(permissions),
            CreateEntryOptions(),
            tags,
            ct);

        if (writeResult.IsFailure)
            return Result.Fail(
                PermissionCacheOutcome.Failure.UserWriteFailed(userId, writeResult.Errors));

        Loggers.LogPermissionsCached(logger, userId, CacheOptions.SlidingExpiration);
        return Result.Ok();
    }

    /// <summary>Removes the user-specific entry and every entry tagged with this user.</summary>
    // Contract: pre=userId!=Guid.Empty, post=user cache entries purged, throws=never
    public async Task<Result> InvalidateUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return Result.Fail(PermissionCacheOutcome.Failure.UserIdRequired);

        string key = $"{PermissionCacheConstant.Patterns.UserKeyPrefix}{userId}";
        string tag = $"{PermissionCacheConstant.Patterns.UserKeyPrefix}{userId}";

        Result removeResult = await cacheService.RemoveAsync(key, ct);
        if (removeResult.IsFailure)
            return Result.Fail(
                PermissionCacheOutcome.Failure.UserInvalidationFailed(userId, removeResult.Errors));

        Result evictResult = await cacheService.RemoveByTagAsync(tag, ct);
        if (evictResult.IsFailure)
            return Result.Fail(
                PermissionCacheOutcome.Failure.UserInvalidationFailed(userId, evictResult.Errors));

        Loggers.LogCacheInvalidated(logger, userId);
        return Result.Ok();
    }

    /// <summary>Purges every permission cache entry via the global tag.</summary>
    // Contract: post=all permission cache entries purged, throws=never
    public async Task<Result> InvalidateAllAsync(CancellationToken ct = default)
    {
        Result evictResult = await cacheService.RemoveByTagAsync(
            PermissionCacheConstant.Patterns.GlobalTag,
            ct);

        if (evictResult.IsFailure)
            return Result.Fail(
                PermissionCacheOutcome.Failure.AllInvalidationFailed(evictResult.Errors));

        Loggers.LogAllPermissionsInvalidated(logger);
        return Result.Ok();
    }

    /// <summary>Retrieves cached role permissions — returns a miss entry so the caller can fall back to the store.</summary>
    // Contract: pre=roleId!=Guid.Empty, post=return.IsSuccess, throws=never
    public async Task<Result<PermissionCacheEntry>> GetRoleAsync(
        Guid roleId,
        CancellationToken ct = default)
    {
        if (roleId == Guid.Empty)
            return Result<PermissionCacheEntry>.Fail(PermissionCacheOutcome.Failure.RoleIdRequired);

        // Cache: probe the role entry; the short-lived factory result is a miss marker.
        string key = $"{PermissionCacheConstant.Patterns.RoleKeyPrefix}{roleId}";

        Result<PermissionCacheEntry> cacheResult = await cacheService.GetOrCreateAsync(
            key,
            _ => ValueTask.FromResult(PermissionCacheEntry.Miss),
            CreateMissEntryOptions(),
            cancellationToken: ct);

        if (cacheResult.IsFailure)
            return Result<PermissionCacheEntry>.Fail(
                PermissionCacheOutcome.Failure.RoleReadFailed(roleId, cacheResult.Errors));

        PermissionCacheEntry entry = cacheResult.Value;

        if (entry.IsHit)
            Loggers.LogRoleCacheHit(logger, roleId);
        else
            Loggers.LogRoleCacheMiss(logger, roleId);

        return Result<PermissionCacheEntry>.Ok(entry);
    }

    /// <summary>Persists role permissions with role-scoped and global invalidation tags.</summary>
    // Contract: pre=roleId!=Guid.Empty && permissions!=null, post=cache entry created, throws=never
    public async Task<Result> SetRoleAsync(
        Guid roleId,
        HashSet<string> permissions,
        CancellationToken ct = default)
    {
        if (roleId == Guid.Empty)
            return Result.Fail(PermissionCacheOutcome.Failure.RoleIdRequired);

        if (permissions is null)
            return Result.Fail(PermissionCacheOutcome.Failure.PermissionsRequired);

        string key = $"{PermissionCacheConstant.Patterns.RoleKeyPrefix}{roleId}";

        // Compute: role entries carry both the role-scoped tag and the global tag.
        var tags = new List<string>(capacity: 2)
        {
            $"{PermissionCacheConstant.Patterns.RoleKeyPrefix}{roleId}",
            PermissionCacheConstant.Patterns.GlobalTag,
        };

        Result writeResult = await cacheService.SetAsync(
            key,
            PermissionCacheEntry.Hit(permissions),
            CreateEntryOptions(),
            tags,
            ct);

        if (writeResult.IsFailure)
            return Result.Fail(
                PermissionCacheOutcome.Failure.RoleWriteFailed(roleId, writeResult.Errors));

        Loggers.LogRolePermissionsCached(logger, roleId, CacheOptions.SlidingExpiration);
        return Result.Ok();
    }

    /// <summary>Removes the role entry and purges every user entry tagged with this role.</summary>
    // Contract: pre=roleId!=Guid.Empty, post=role cache entries purged, throws=never
    public async Task<Result> InvalidateRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        if (roleId == Guid.Empty)
            return Result.Fail(PermissionCacheOutcome.Failure.RoleIdRequired);

        string key = $"{PermissionCacheConstant.Patterns.RoleKeyPrefix}{roleId}";
        string tag = $"{PermissionCacheConstant.Patterns.RoleKeyPrefix}{roleId}";

        Result removeResult = await cacheService.RemoveAsync(key, ct);
        if (removeResult.IsFailure)
            return Result.Fail(
                PermissionCacheOutcome.Failure.RoleInvalidationFailed(roleId, removeResult.Errors));

        Result evictResult = await cacheService.RemoveByTagAsync(tag, ct);
        if (evictResult.IsFailure)
            return Result.Fail(
                PermissionCacheOutcome.Failure.RoleInvalidationFailed(roleId, evictResult.Errors));

        Loggers.LogRoleCacheInvalidated(logger, roleId);
        return Result.Ok();
    }
}
