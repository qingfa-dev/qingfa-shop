using Microsoft.Extensions.Logging;

namespace Infrastructure.Security.Authorization.Permissions.Services;

public partial class PermissionService
{
    /// <summary>Structured log event definitions for PermissionService operations.</summary>
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 144,
            Level = LogLevel.Debug,
            Message = "PermissionService: resolved {Count} effective permissions for user {UserId}")]
        public static partial void LogEffectivePermissionsResolved(ILogger logger, int count, Guid userId);

        [LoggerMessage(
            EventId = 145,
            Level = LogLevel.Debug,
            Message = "PermissionService: resolved {Count} permissions for role {RoleId}")]
        public static partial void LogRolePermissionsResolved(ILogger logger, int count, Guid roleId);

        [LoggerMessage(
            EventId = 146,
            Level = LogLevel.Error,
            Message = "PermissionService: unexpected failure while resolving permissions for user {UserId}: {Error}")]
        public static partial void LogUserResolutionFailed(ILogger logger, Guid userId, string? error);

        [LoggerMessage(
            EventId = 147,
            Level = LogLevel.Error,
            Message = "PermissionService: unexpected failure while resolving permissions for role {RoleId}: {Error}")]
        public static partial void LogRoleResolutionFailed(ILogger logger, Guid roleId, string? error);

        [LoggerMessage(
            EventId = 148,
            Level = LogLevel.Warning,
            Message = "PermissionService: user cache read failed for user {UserId}; falling back to the store: {Error}")]
        public static partial void LogUserCacheReadFailed(ILogger logger, Guid userId, string? error);

        [LoggerMessage(
            EventId = 149,
            Level = LogLevel.Warning,
            Message = "PermissionService: role cache read failed for role {RoleId}; falling back to the store: {Error}")]
        public static partial void LogRoleCacheReadFailed(ILogger logger, Guid roleId, string? error);

        [LoggerMessage(
            EventId = 150,
            Level = LogLevel.Warning,
            Message = "PermissionService: user cache write failed for user {UserId}; authorization continues without the entry: {Error}")]
        public static partial void LogUserCacheWriteFailed(ILogger logger, Guid userId, string? error);

        [LoggerMessage(
            EventId = 151,
            Level = LogLevel.Warning,
            Message = "PermissionService: role cache write failed for role {RoleId}; authorization continues without the entry: {Error}")]
        public static partial void LogRoleCacheWriteFailed(ILogger logger, Guid roleId, string? error);

        [LoggerMessage(
            EventId = 152,
            Level = LogLevel.Warning,
            Message = "PermissionService: role-id store read failed for user {UserId}; resolving direct permissions only: {Error}")]
        public static partial void LogUserRoleIdsReadFailed(ILogger logger, Guid userId, string? error);

        [LoggerMessage(
            EventId = 153,
            Level = LogLevel.Warning,
            Message = "PermissionService: direct-permission store read failed for user {UserId}; continuing without direct permissions: {Error}")]
        public static partial void LogUserDirectReadFailed(ILogger logger, Guid userId, string? error);

        [LoggerMessage(
            EventId = 154,
            Level = LogLevel.Warning,
            Message = "PermissionService: role-permission store read failed for role {RoleId}; continuing without that role: {Error}")]
        public static partial void LogRoleStoreReadFailed(ILogger logger, Guid roleId, string? error);
    }
}
