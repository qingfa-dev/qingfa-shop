namespace Infrastructure.Integrations.Storages.Services;

public sealed partial class StorageService
{
    /// <summary>Structured log event definitions for storage upload/scan/encryption operations.</summary>
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 290,
            Level = LogLevel.Warning,
            Message = "Upload rejected for key '{Key}': {Reason}")]
        public static partial void LogUploadBlocked(
            ILogger logger,
            string key,
            string reason);

        [LoggerMessage(
            EventId = 291,
            Level = LogLevel.Warning,
            Message = "Security check failed for '{Key}': {Code}")]
        public static partial void LogSecurityCheckFailed(
            ILogger logger,
            string key,
            string? code);

        [LoggerMessage(
            EventId = 292,
            Level = LogLevel.Information,
            Message = "[{Provider}] Upload '{Key}' succeeded in {Elapsed}ms")]
        public static partial void LogUploadSuccess(
            ILogger logger,
            string provider,
            string key,
            long elapsed);

        [LoggerMessage(
            EventId = 293,
            Level = LogLevel.Warning,
            Message = "[{Provider}] Upload '{Key}' failed in {Elapsed}ms: {Code}")]
        public static partial void LogUploadFailure(
            ILogger logger,
            string provider,
            string key,
            long elapsed,
            string? code);

        [LoggerMessage(
            EventId = 294,
            Level = LogLevel.Information,
            Message = "[{Provider}] {Op} '{Key}' succeeded in {Elapsed}ms")]
        public static partial void LogOperationSuccess(
            ILogger logger,
            string provider,
            string op,
            string key,
            long elapsed);

        [LoggerMessage(
            EventId = 295,
            Level = LogLevel.Warning,
            Message = "[{Provider}] {Op} '{Key}' failed in {Elapsed}ms: {Code}")]
        public static partial void LogOperationFailure(
            ILogger logger,
            string provider,
            string op,
            string key,
            long elapsed,
            string? code);

        [LoggerMessage(
            EventId = 296,
            Level = LogLevel.Error,
            Message = "Provider '{Provider}' is not registered")]
        public static partial void LogProviderNotFound(
            ILogger logger,
            string provider);

        [LoggerMessage(
            EventId = 297,
            Level = LogLevel.Warning,
            Message = "Malware scan failed for '{Key}': {Reason}")]
        public static partial void LogMalwareScanFailed(
            ILogger logger,
            string key,
            string? reason);

        [LoggerMessage(
            EventId = 298,
            Level = LogLevel.Warning,
            Message = "Upload rejected for '{Key}' — malware detected: {Threat}")]
        public static partial void LogMalwareRejected(
            ILogger logger,
            string key,
            string threat);

        [LoggerMessage(
            EventId = 299,
            Level = LogLevel.Warning,
            Message = "File '{Key}' quarantined — threat: {Threat}")]
        public static partial void LogMalwareQuarantined(
            ILogger logger,
            string key,
            string threat);

        [LoggerMessage(
            EventId = 300,
            Level = LogLevel.Warning,
            Message = "File '{Key}' accepted with warning — threat: {Threat}")]
        public static partial void LogMalwareWarning(
            ILogger logger,
            string key,
            string threat);

        [LoggerMessage(
            EventId = 301,
            Level = LogLevel.Error,
            Message = "Image processing failed for '{Key}': {Reason}")]
        public static partial void LogImageProcessingFailed(
            ILogger logger,
            string key,
            string? reason);

        [LoggerMessage(
            EventId = 302,
            Level = LogLevel.Debug,
            Message = "Image processing completed for '{Key}'")]
        public static partial void LogImageProcessingCompleted(
            ILogger logger,
            string key);

        [LoggerMessage(
            EventId = 303,
            Level = LogLevel.Debug,
            Message = "Hash computed for '{Key}': {Hash}")]
        public static partial void LogHashComputed(
            ILogger logger,
            string key,
            string hash);

        [LoggerMessage(
            EventId = 304,
            Level = LogLevel.Error,
            Message = "Hash computation failed for '{Key}': {Reason}")]
        public static partial void LogHashFailed(
            ILogger logger,
            string key,
            string? reason);

        [LoggerMessage(
            EventId = 305,
            Level = LogLevel.Debug,
            Message = "Encryption applied to content for '{Key}'")]
        public static partial void LogEncryptionApplied(
            ILogger logger,
            string key);

        [LoggerMessage(
            EventId = 306,
            Level = LogLevel.Error,
            Message = "Encryption failed for '{Key}': {Reason}")]
        public static partial void LogEncryptionFailed(
            ILogger logger,
            string key,
            string? reason);

        [LoggerMessage(
            EventId = 307,
            Level = LogLevel.Debug,
            Message = "Encryption skipped for '{Key}' — encryption key not configured")]
        public static partial void LogEncryptionSkipped(
            ILogger logger,
            string key);
    }
}
