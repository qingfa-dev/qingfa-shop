using Microsoft.Extensions.Logging;

namespace Infrastructure.Caching;

public sealed partial class CacheService
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
    }
}