namespace Infrastructure.BuildingBlocks.Adapters.Storage.Security;

public sealed partial class StorageSecurityEnforcer
{
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 280,
            Level = LogLevel.Information,
            Message = "Security check passed for '{Key}'")]
        public static partial void LogSecurityCheckPassed(
            ILogger logger,
            string key);

        [LoggerMessage(
            EventId = 281,
            Level = LogLevel.Warning,
            Message = "Upload blocked for '{Key}': extension '{Extension}' is in the blocked list")]
        public static partial void LogBlockedExtension(
            ILogger logger,
            string key,
            string extension);

        [LoggerMessage(
            EventId = 282,
            Level = LogLevel.Warning,
            Message = "Upload blocked for '{Key}': extension '{Extension}' is not in the allowed list")]
        public static partial void LogForbiddenExtension(
            ILogger logger,
            string key,
            string extension);

        [LoggerMessage(
            EventId = 283,
            Level = LogLevel.Warning,
            Message = "Upload blocked for '{Key}': file size exceeds maximum of {MaxBytes} bytes")]
        public static partial void LogFileSizeExceeded(
            ILogger logger,
            string key,
            long maxBytes);

        [LoggerMessage(
            EventId = 284,
            Level = LogLevel.Warning,
            Message = "Upload blocked for '{Key}': magic bytes mismatch for extension '{Extension}'")]
        public static partial void LogMagicBytesMismatch(
            ILogger logger,
            string key,
            string extension);

        [LoggerMessage(
            EventId = 285,
            Level = LogLevel.Error,
            Message = "Upload blocked for '{Key}': could not determine file size (non-seekable stream)")]
        public static partial void LogFileSizeUnknown(
            ILogger logger,
            string key);
    }
}
