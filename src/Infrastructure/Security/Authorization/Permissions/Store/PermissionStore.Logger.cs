namespace Infrastructure.Security.Authorization.Permissions.Store;

public partial class PermissionStoreService
{
    internal static partial class Loggers
    {
        // Log: User permissions loaded from database count.
        [LoggerMessage(
            EventId = 148,
            Level = LogLevel.Debug,
            Message = "Loaded {Count} permissions for user {UserId}")]
        public static partial void LogPermissionsLoaded(ILogger logger, int count, Guid userId);

        // Log: Database query failed for user permissions — returning empty set.
        [LoggerMessage(
            EventId = 149,
            Level = LogLevel.Error,
            Message = "Failed to get permissions for user {UserId}. Returning empty permission set.")]
        public static partial void LogGetPermissionsFailed(ILogger logger, Guid userId, Exception ex);

        // Log: Role permissions loaded from database count.
        [LoggerMessage(
            EventId = 150,
            Level = LogLevel.Debug,
            Message = "Loaded {Count} permissions for role {RoleId}")]
        public static partial void LogRolePermissionsLoaded(ILogger logger, int count, Guid roleId);

        // Log: Database query failed for role permissions — returning empty set.
        [LoggerMessage(
            EventId = 151,
            Level = LogLevel.Error,
            Message = "Failed to get permissions for role {RoleId}. Returning empty permission set.")]
        public static partial void LogGetRolePermissionsFailed(ILogger logger, Guid roleId, Exception ex);

        // Log: Batch permissions added to role.
        [LoggerMessage(
            EventId = 152,
            Level = LogLevel.Information,
            Message = "Batch ADD {Count} permissions to role {RoleId}")]
        public static partial void LogBatchAddRolePermissions(ILogger logger, int count, Guid roleId);

        // Log: Batch add to role failed — see error.
        [LoggerMessage(
            EventId = 153,
            Level = LogLevel.Error,
            Message = "Failed to batch ADD permissions to role {RoleId}: {Error}")]
        public static partial void LogBatchAddRolePermissionsFailed(ILogger logger, Guid roleId, string? error);

        // Log: Batch permissions removed from role.
        [LoggerMessage(
            EventId = 154,
            Level = LogLevel.Information,
            Message = "Batch REMOVE {Count} permissions from role {RoleId}")]
        public static partial void LogBatchRemoveRolePermissions(ILogger logger, int count, Guid roleId);

        // Log: Batch remove from role failed — see error.
        [LoggerMessage(
            EventId = 155,
            Level = LogLevel.Error,
            Message = "Failed to batch REMOVE permissions from role {RoleId}: {Error}")]
        public static partial void LogBatchRemoveRolePermissionsFailed(ILogger logger, Guid roleId, string? error);

        // Log: Batch direct permissions added to user.
        [LoggerMessage(
            EventId = 156,
            Level = LogLevel.Information,
            Message = "Batch ADD {Count} direct permissions to user {UserId}")]
        public static partial void LogBatchAddUserPermissions(ILogger logger, int count, Guid userId);

        // Log: Batch add to user failed — see error.
        [LoggerMessage(
            EventId = 157,
            Level = LogLevel.Error,
            Message = "Failed to batch ADD direct permissions to user {UserId}: {Error}")]
        public static partial void LogBatchAddUserPermissionsFailed(ILogger logger, Guid userId, string? error);

        // Log: Batch direct permissions removed from user.
        [LoggerMessage(
            EventId = 158,
            Level = LogLevel.Information,
            Message = "Batch REMOVE {Count} direct permissions from user {UserId}")]
        public static partial void LogBatchRemoveUserPermissions(ILogger logger, int count, Guid userId);

        // Log: Batch remove from user failed — see error.
        [LoggerMessage(
            EventId = 159,
            Level = LogLevel.Error,
            Message = "Failed to batch REMOVE direct permissions from user {UserId}: {Error}")]
        public static partial void LogBatchRemoveUserPermissionsFailed(ILogger logger, Guid userId, string? error);

        // Log: Record total distinct permission identifiers loaded from store.
        [LoggerMessage(
            EventId = 160,
            Level = LogLevel.Information,
            Message = "PermissionStore: loaded {Count} distinct permission identifiers from store")]
        public static partial void LogAllIdentifiersLoaded(ILogger logger, int count);
    }
}
