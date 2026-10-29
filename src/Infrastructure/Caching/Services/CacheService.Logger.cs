namespace Infrastructure.Caching.Services;

internal sealed partial class CacheService
{
    /// <summary>Structured log events for the cache lifecycle.</summary>
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 194,
            Level = LogLevel.Debug,
            Message = "Cache hit for key: {Key}")]
        public static partial void CacheHit(ILogger logger, string key);

        [LoggerMessage(
            EventId = 195,
            Level = LogLevel.Debug,
            Message = "Cache miss for key: {Key}")]
        public static partial void CacheMiss(ILogger logger, string key);

        [LoggerMessage(
            EventId = 196,
            Level = LogLevel.Debug,
            Message = "Cache set for key: {Key}")]
        public static partial void CacheSet(ILogger logger, string key);

        [LoggerMessage(
            EventId = 197,
            Level = LogLevel.Debug,
            Message = "Cache removed for key: {Key}")]
        public static partial void CacheRemoved(ILogger logger, string key);

        [LoggerMessage(
            EventId = 198,
            Level = LogLevel.Debug,
            Message = "Cache removed by tag: {Tag}")]
        public static partial void CacheRemovedByTag(ILogger logger, string tag);

        [LoggerMessage(
            EventId = 199,
            Level = LogLevel.Error,
            Message = "Cache read failed for key: {Key}")]
        public static partial void CacheReadFailed(ILogger logger, string key, Exception exception);

        [LoggerMessage(
            EventId = 200,
            Level = LogLevel.Error,
            Message = "Cache write failed for key: {Key}")]
        public static partial void CacheWriteFailed(ILogger logger, string key, Exception exception);

        [LoggerMessage(
            EventId = 201,
            Level = LogLevel.Error,
            Message = "Cache remove failed for key: {Key}")]
        public static partial void CacheRemoveFailed(ILogger logger, string key, Exception exception);

        [LoggerMessage(
            EventId = 202,
            Level = LogLevel.Error,
            Message = "Cache eviction failed for tag(s): {Tag}")]
        public static partial void CacheEvictFailed(ILogger logger, string tag, Exception exception);
    }
}