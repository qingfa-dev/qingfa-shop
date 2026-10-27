namespace Infrastructure.Persistence.EntityFramework.Options;

/// <summary>Constants for EF Core persistence configuration: defaults, limits, and constraints.</summary>
public static class PersistenceConstants
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "EfCore";

    /// <summary>Default values applied when configuration omits a setting.</summary>
    public static class Defaults
    {
        /// <summary>Default database provider.</summary>
        public const string Provider = "PostgreSQL";

        /// <summary>Default command timeout in seconds.</summary>
        public const int CommandTimeoutSeconds = 30;

        /// <summary>Default maximum retry attempts for transient failures.</summary>
        public const int MaxRetryAttempts = 3;

        /// <summary>Default base delay in milliseconds for exponential backoff.</summary>
        public const int RetryBaseDelayMs = 100;

        /// <summary>Default connection pool minimum size.</summary>
        public const int MinPoolSize = 0;

        /// <summary>Default connection pool maximum size.</summary>
        public const int MaxPoolSize = 100;

        /// <summary>Connection probe timeout in seconds.</summary>
        public const int ConnectionProbeTimeoutSeconds = 5;

        /// <summary>Sensitive data logging is off by default.</summary>
        public const bool EnableSensitiveDataLogging = false;

        /// <summary>Detailed errors are off by default.</summary>
        public const bool EnableDetailedErrors = false;

        /// <summary>Connection pooling is on by default.</summary>
        public const bool UsePooling = true;
    }

    /// <summary>Hard limits enforced during validation.</summary>
    public static class Limits
    {
        /// <summary>Maximum command timeout in seconds (1 hour).</summary>
        public const int CommandTimeoutSecondsMax = 3600;

        /// <summary>Maximum connection pool size.</summary>
        public const int MaxPoolSizeLimit = 500;

        /// <summary>Maximum retry attempts.</summary>
        public const int MaxRetryAttemptsLimit = 10;

        /// <summary>Maximum retry base delay in milliseconds (5 seconds).</summary>
        public const int RetryBaseDelayMsLimit = 5000;
    }

    /// <summary>Accepted ranges and patterns for validation.</summary>
    public static class Constraints
    {
        /// <summary>Minimum command timeout in seconds.</summary>
        public const int CommandTimeoutSecondsMin = 1;

        /// <summary>Minimum connection pool size.</summary>
        public const int MinPoolSizeMin = 0;

        /// <summary>Minimum connection pool maximum size.</summary>
        public const int MaxPoolSizeMin = 1;

        /// <summary>Allowed database providers.</summary>
        public static readonly string[] ValidProviders = ["postgresql"];
    }
}