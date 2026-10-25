namespace Infrastructure.Caching.Options;

/// <summary>Constants for the L1 memory cache options.</summary>
public static class MemoryCacheSettingConstant
{
    /// <summary>Default values.</summary>
    public static class Defaults
    {
        /// <summary>The L1 tier is enabled by default.</summary>
        public const bool Enabled = true;

        /// <summary>Default L1 entry lifetime in minutes.</summary>
        public const int ExpirationMinutes = 30;

        /// <summary>Percentage of the memory cache reclaimed under pressure.</summary>
        public const int CompactionPercentage = 25;

        /// <summary>Default memory cache size limit in bytes (100 MiB).</summary>
        public const long SizeLimitBytes = 100 * 1024 * 1024;
    }

    /// <summary>Accepted ranges.</summary>
    public static class Constraints
    {
        /// <summary>Shortest entry lifetime in minutes.</summary>
        public const int ExpirationMinutesMin = 1;

        /// <summary>Longest entry lifetime in minutes (one day).</summary>
        public const int ExpirationMinutesMax = 1440;

        /// <summary>Smallest compaction percentage.</summary>
        public const int CompactionPercentageMin = 1;

        /// <summary>Largest compaction percentage.</summary>
        public const int CompactionPercentageMax = 100;

        /// <summary>Smallest memory cache size limit in bytes.</summary>
        public const long SizeLimitBytesMin = 1;
    }
}