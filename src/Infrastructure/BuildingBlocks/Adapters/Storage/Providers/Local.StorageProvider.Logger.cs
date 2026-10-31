namespace Infrastructure.BuildingBlocks.Adapters.Storage.Providers;

public sealed partial class LocalStorageProvider
{
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 310,
            Level = LogLevel.Information,
            Message = "LocalStorage: uploaded '{Key}' ({Size} bytes)")]
        public static partial void LogUploadSuccess(
            ILogger logger,
            string key,
            long size);

        [LoggerMessage(
            EventId = 311,
            Level = LogLevel.Error,
            Message = "LocalStorage: upload failed for '{Key}'")]
        public static partial void LogUploadFailed(
            ILogger logger,
            string key,
            Exception? exception = null);

        [LoggerMessage(
            EventId = 312,
            Level = LogLevel.Error,
            Message = "LocalStorage: download failed for '{Key}'")]
        public static partial void LogDownloadFailed(
            ILogger logger,
            string key,
            Exception? exception = null);

        [LoggerMessage(
            EventId = 313,
            Level = LogLevel.Information,
            Message = "LocalStorage: deleted '{Key}'")]
        public static partial void LogDeleteSuccess(
            ILogger logger,
            string key);

        [LoggerMessage(
            EventId = 314,
            Level = LogLevel.Error,
            Message = "LocalStorage: delete failed for '{Key}'")]
        public static partial void LogDeleteFailed(
            ILogger logger,
            string key,
            Exception? exception = null);

        [LoggerMessage(
            EventId = 315,
            Level = LogLevel.Error,
            Message = "LocalStorage: stat failed for '{Key}'")]
        public static partial void LogStatFailed(
            ILogger logger,
            string key,
            Exception? exception = null);

        [LoggerMessage(
            EventId = 316,
            Level = LogLevel.Error,
            Message = "LocalStorage: list failed")]
        public static partial void LogListFailed(
            ILogger logger,
            Exception? exception = null);
    }
}
