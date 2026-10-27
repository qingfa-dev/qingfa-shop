namespace Infrastructure.Caching.Options.Distributed;

/// <summary>Constants for the L2 distributed cache options.</summary>
public static class DistributedCacheSettingConstant
{
    /// <summary>Default values.</summary>
    public static class Defaults
    {
        /// <summary>The L2 tier is disabled by default (requires explicit provider config).</summary>
        public const bool Enabled = false;

        /// <summary>Default L2 entry lifetime in minutes.</summary>
        public const int ExpirationMinutes = 60;
    }

    /// <summary>Accepted ranges.</summary>
    public static class Constraints
    {
        /// <summary>Shortest entry lifetime in minutes.</summary>
        public const int ExpirationMinutesMin = 1;

        /// <summary>Longest entry lifetime in minutes (one day).</summary>
        public const int ExpirationMinutesMax = 1440;
    }

    public static class Patterns
    {
        public static readonly string[] ValidTypes = ["memory", "redis", "sqlserver"];
    }
}