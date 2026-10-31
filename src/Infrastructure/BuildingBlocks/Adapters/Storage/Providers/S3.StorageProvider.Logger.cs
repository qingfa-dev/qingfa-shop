namespace Infrastructure.BuildingBlocks.Adapters.Storage.Providers;

public sealed partial class S3StorageProvider
{
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 320,
            Level = LogLevel.Information,
            Message = "S3Storage: uploading '{Key}' to bucket '{Bucket}'")]
        public static partial void LogUploadStart(
            ILogger logger,
            string key,
            string bucket);

        [LoggerMessage(
            EventId = 321,
            Level = LogLevel.Information,
            Message = "S3Storage: download stub for '{Key}'")]
        public static partial void LogDownloadStub(
            ILogger logger,
            string key);

        [LoggerMessage(
            EventId = 322,
            Level = LogLevel.Information,
            Message = "S3Storage: delete stub for '{Key}'")]
        public static partial void LogDeleteStub(
            ILogger logger,
            string key);

        [LoggerMessage(
            EventId = 323,
            Level = LogLevel.Information,
            Message = "S3Storage: stat stub for '{Key}'")]
        public static partial void LogStatStub(
            ILogger logger,
            string key);

        [LoggerMessage(
            EventId = 324,
            Level = LogLevel.Information,
            Message = "S3Storage: list stub (prefix='{Prefix}')")]
        public static partial void LogListStub(
            ILogger logger,
            string? prefix);
    }
}
