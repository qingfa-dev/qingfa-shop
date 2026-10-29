namespace Infrastructure.Integrations.Storages.Security.Guard;

public sealed partial class StorageAntiForgeryGuard
{
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 286,
            Level = LogLevel.Warning,
            Message = "Identity '{IdentityKey}' blocked after {FailureCount} consecutive failures")]
        public static partial void LogIdentityBlocked(
            ILogger logger,
            string identityKey,
            int failureCount);

        [LoggerMessage(
            EventId = 287,
            Level = LogLevel.Information,
            Message = "Failure counter reset for identity '{IdentityKey}'")]
        public static partial void LogIdentityReset(
            ILogger logger,
            string identityKey);

        [LoggerMessage(
            EventId = 288,
            Level = LogLevel.Debug,
            Message = "Failure recorded for identity '{IdentityKey}' (count: {FailureCount})")]
        public static partial void LogFailureRecorded(
            ILogger logger,
            string identityKey,
            int failureCount);

        [LoggerMessage(
            EventId = 289,
            Level = LogLevel.Warning,
            Message = "Anti-forgery token invalid for identity '{IdentityKey}' — recording failure")]
        public static partial void LogAntiforgeryTokenInvalid(
            ILogger logger,
            string identityKey);
    }
}
