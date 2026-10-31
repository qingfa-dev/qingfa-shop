namespace Infrastructure.BuildingBlocks.Adapters.Security.Authorization.Permissions.Caches;

public partial class PermissionCache
{
    internal static partial class Loggers
    {
        // Log: Cache miss for user — permissions will be loaded from the database store.
        [LoggerMessage(
            EventId = 128,
            Level = LogLevel.Debug,
            Message = "Cache miss for user {UserId} - permissions will be loaded from store")]
        public static partial void LogCacheMiss(ILogger logger, Guid userId);

        // Log: Cache hit for user — returning cached permission set.
        [LoggerMessage(
            EventId = 129,
            Level = LogLevel.Debug,
            Message = "Cache hit for user {UserId}")]
        public static partial void LogCacheHit(ILogger logger, Guid userId);

        // Log: Cache read failure — likely infrastructure issue.
        [LoggerMessage(
            EventId = 130,
            Level = LogLevel.Error,
            Message = "Failed to get permissions from cache for user {UserId}")]
        public static partial void LogGetCacheFailed(ILogger logger, Guid userId, Exception ex);

        // Log: User permission set written to cache with TTL.
        [LoggerMessage(
            EventId = 131,
            Level = LogLevel.Debug,
            Message = "Cached permissions for user {UserId} with sliding expiration {SlidingExpiration}")]
        public static partial void LogPermissionsCached(ILogger logger, Guid userId, TimeSpan? slidingExpiration);

        // Log: Cache write failure — permissions will be re-resolved on next request.
        [LoggerMessage(
            EventId = 132,
            Level = LogLevel.Error,
            Message = "Failed to set permissions cache for user {UserId}")]
        public static partial void LogSetCacheFailed(ILogger logger, Guid userId, Exception ex);

        // Log: User cache entry invalidated.
        [LoggerMessage(
            EventId = 133,
            Level = LogLevel.Debug,
            Message = "Invalidated cache for user {UserId}")]
        public static partial void LogCacheInvalidated(ILogger logger, Guid userId);

        // Log: User cache invalidation failed — stale data may persist.
        [LoggerMessage(
            EventId = 134,
            Level = LogLevel.Error,
            Message = "Failed to invalidate cache for user {UserId}")]
        public static partial void LogInvalidateCacheFailed(ILogger logger, Guid userId, Exception ex);

        // Log: Global cache invalidation completed.
        [LoggerMessage(
            EventId = 135,
            Level = LogLevel.Debug,
            Message = "Invalidated all permissions in cache")]
        public static partial void LogAllPermissionsInvalidated(ILogger logger);

        // Log: Global cache invalidation failed.
        [LoggerMessage(
            EventId = 136,
            Level = LogLevel.Error,
            Message = "Failed to invalidate all permissions in cache")]
        public static partial void LogInvalidateAllPermissionsFailed(ILogger logger, Exception ex);

        // Log: Role cache miss — permissions will be loaded from store.
        [LoggerMessage(
            EventId = 137,
            Level = LogLevel.Debug,
            Message = "Cache miss for role {RoleId}")]
        public static partial void LogRoleCacheMiss(ILogger logger, Guid roleId);

        // Log: Role cache hit — returning cached permission set.
        [LoggerMessage(
            EventId = 138,
            Level = LogLevel.Debug,
            Message = "Cache hit for role {RoleId}")]
        public static partial void LogRoleCacheHit(ILogger logger, Guid roleId);

        // Log: Role cache read failure.
        [LoggerMessage(
            EventId = 139,
            Level = LogLevel.Error,
            Message = "Failed to get permissions from cache for role {RoleId}")]
        public static partial void LogGetRoleCacheFailed(ILogger logger, Guid roleId, Exception ex);

        // Log: Role permission set written to cache with TTL.
        [LoggerMessage(
            EventId = 140,
            Level = LogLevel.Debug,
            Message = "Cached permissions for role {RoleId} with sliding expiration {SlidingExpiration}")]
        public static partial void LogRolePermissionsCached(ILogger logger, Guid roleId, TimeSpan? slidingExpiration);

        // Log: Role cache write failure.
        [LoggerMessage(
            EventId = 141,
            Level = LogLevel.Error,
            Message = "Failed to set permissions cache for role {RoleId}")]
        public static partial void LogSetRoleCacheFailed(ILogger logger, Guid roleId, Exception ex);

        // Log: Role cache entry invalidated.
        [LoggerMessage(
            EventId = 142,
            Level = LogLevel.Debug,
            Message = "Invalidated cache for role {RoleId}")]
        public static partial void LogRoleCacheInvalidated(ILogger logger, Guid roleId);

        // Log: Role cache invalidation failed.
        [LoggerMessage(
            EventId = 143,
            Level = LogLevel.Error,
            Message = "Failed to invalidate cache for role {RoleId}")]
        public static partial void LogInvalidateRoleCacheFailed(ILogger logger, Guid roleId, Exception ex);
    }
}
