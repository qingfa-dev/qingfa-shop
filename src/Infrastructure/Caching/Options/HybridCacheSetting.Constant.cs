using Application.Common.Caching;

namespace Infrastructure.Caching.Options;

/// <summary>Constants for the HybridCache facade limits.</summary>
public static class HybridCacheSettingConstant
{
    /// <summary>Default values.</summary>
    public static class Defaults
    {
        /// <summary>Default largest payload a cache entry may hold (1 MiB).</summary>
        public const long MaximumPayloadBytes = 1024 * 1024;

        /// <summary>Default longest cache key in characters.</summary>
        public const int MaximumKeyLength = CachingConstant.Key.MaxLength;
    }

    /// <summary>Accepted ranges.</summary>
    public static class Constraints
    {
        /// <summary>Smallest accepted payload in bytes.</summary>
        public const long MaximumPayloadBytesMin = 1;

        /// <summary>Largest accepted payload in bytes (10 MiB).</summary>
        public const long MaximumPayloadBytesMax = 10 * 1024 * 1024;

        /// <summary>Shortest accepted cache key in characters.</summary>
        public const int MaximumKeyLengthMin = 1;

        /// <summary>Longest accepted cache key in characters.</summary>
        public const int MaximumKeyLengthMax = 2048;
    }
}