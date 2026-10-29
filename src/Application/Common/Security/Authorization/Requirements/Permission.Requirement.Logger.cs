using Microsoft.Extensions.Logging;

namespace Application.Common.Security.Authorization.Requirements;

public partial class PermissionRequirementAuthorizationHandler
{
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 162,
            Level = LogLevel.Warning,
            Message = "PermissionHandler: authenticated principal has no NameIdentifier claim")]
        public static partial void LogNoNameIdentifier(ILogger logger);

        [LoggerMessage(
            EventId = 163,
            Level = LogLevel.Warning,
            Message = "PermissionHandler: authenticated principal has invalid NameIdentifier claim value: {UserId}")]
        public static partial void LogInvalidNameIdentifier(ILogger logger, string userId);

        [LoggerMessage(
            EventId = 164,
            Level = LogLevel.Error,
            Message = "PermissionHandler: failed to get permissions from cache for user {UserId}: {Error}")]
        public static partial void LogCacheAccessFailed(ILogger logger, Guid userId, string? error);

        [LoggerMessage(
            EventId = 165,
            Level = LogLevel.Debug,
            Message = "PermissionHandler: cache miss for user {UserId} — loading from store")]
        public static partial void LogCacheMiss(ILogger logger, Guid userId);

        [LoggerMessage(
            EventId = 166,
            Level = LogLevel.Error,
            Message = "PermissionHandler: failed to get permissions from store for user {UserId}: {Error}. Falling back to empty set.")]
        public static partial void LogStoreLookupFailed(ILogger logger, Guid userId, string? error);

        [LoggerMessage(
            EventId = 167,
            Level = LogLevel.Debug,
            Message = "PermissionHandler: cache hit for user {UserId} ({Count} permissions)")]
        public static partial void LogCacheHit(ILogger logger, Guid userId, int count);

        [LoggerMessage(
            EventId = 168,
            Level = LogLevel.Debug,
            Message = "PermissionHandler: authorization succeeded for user {UserId} with permission {Permission}")]
        public static partial void LogAuthorizationSucceeded(ILogger logger, Guid? userId, string permission);

        [LoggerMessage(
            EventId = 169,
            Level = LogLevel.Debug,
            Message = "PermissionHandler: authorization failed for user {UserId} - missing permission {Permission}")]
        public static partial void LogAuthorizationFailed(ILogger logger, Guid? userId, string permission);

        // Log: Record admin privilege bypass for permission check.
        [LoggerMessage(
            EventId = 14,
            Level = LogLevel.Debug,
            Message = "Admin bypass: user {UserId} granted permission {Permission}")]
        public static partial void LogAdminBypass(ILogger logger, Guid userId, string permission);
    }
}
