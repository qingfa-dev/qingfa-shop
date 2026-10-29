using Microsoft.Extensions.Logging;

namespace Infrastructure.Security.Authorization.Permissions.Registry;

public sealed partial class PermissionMetadataRegistry
{
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 203,
            Level = LogLevel.Information,
            Message = "Permission metadata refreshed: {Count} permissions ({Added} discovered from claims) at {RefreshedAtUtc}")]
        public static partial void LogRefreshed(
            ILogger logger,
            int count,
            int added,
            DateTimeOffset refreshedAtUtc);

        [LoggerMessage(
            EventId = 204,
            Level = LogLevel.Warning,
            Message = "Skipped non-canonical permission claim value: {Identifier}")]
        public static partial void LogSkippedIdentifier(ILogger logger, string identifier);

        [LoggerMessage(
            EventId = 205,
            Level = LogLevel.Error,
            Message = "Permission metadata refresh failed")]
        public static partial void LogRefreshFailed(ILogger logger, Exception exception);
    }
}
