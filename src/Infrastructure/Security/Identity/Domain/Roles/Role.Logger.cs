namespace Infrastructure.Security.Identity.Domain.Roles;

public static partial class RoleLoggers
{
    public static partial class Management
    {
        [LoggerMessage(
            EventId = 1065,
            Level = LogLevel.Debug,
            Message = "[Role.Created]: {RoleName} ({RoleId}) by {ActionBy}")]
        public static partial void Created(ILogger logger, string RoleName, Guid RoleId, string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1066,
            Level = LogLevel.Debug,
            Message = "[Role.Updated]: {RoleName} ({RoleId}) by {ActionBy}")]
        public static partial void Updated(ILogger logger, string RoleName, Guid RoleId, string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1067,
            Level = LogLevel.Debug,
            Message = "[Role.Deleted]: {RoleName} ({RoleId}) by {ActionBy}")]
        public static partial void Deleted(ILogger logger, string RoleName, Guid RoleId, string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1068,
            Level = LogLevel.Warning,
            Message = "[Role.SystemRoleProtected]: Attempted modification of system role {RoleName} ({RoleId}) by {ActionBy}")]
        public static partial void SystemRoleProtected(ILogger logger, string RoleName, Guid RoleId, string? ActionBy = "System");
    }

    public static partial class Permissions
    {
        [LoggerMessage(
            EventId = 1069,
            Level = LogLevel.Debug,
            Message = "[Role.PermissionsAssigned]: {PermissionCount} permission(s) added to role {RoleName} ({RoleId}) by {ActionBy}")]
        public static partial void PermissionsAssigned(
            ILogger logger,
            string RoleName,
            Guid RoleId,
            int PermissionCount,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1070,
            Level = LogLevel.Debug,
            Message = "[Role.PermissionsRevoked]: {PermissionCount} permission(s) removed from role {RoleName} ({RoleId}) by {ActionBy}")]
        public static partial void PermissionsRevoked(
            ILogger logger,
            string RoleName,
            Guid RoleId,
            int PermissionCount,
            string? ActionBy = "System");

        [LoggerMessage(
            EventId = 1071,
            Level = LogLevel.Debug,
            Message = "[Role.PermissionsSynced]: {RoleName} ({RoleId}) by {ActionBy}. Added: {AddedCount}, Removed: {RemovedCount}")]
        public static partial void PermissionsSynced(
            ILogger logger,
            string RoleName,
            Guid RoleId,
            int AddedCount,
            int RemovedCount,
            string? ActionBy = "System");
    }
}
