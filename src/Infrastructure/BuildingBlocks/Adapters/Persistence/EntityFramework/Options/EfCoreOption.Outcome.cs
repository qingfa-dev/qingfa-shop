namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Options;

/// <summary>Failure outcomes for persistence configuration validation.</summary>
public static class PersistenceSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        /// <summary>Connection string name is missing or empty.</summary>
        public static Error ConnectionStringNameRequired => Error.BadRequest(
            code: Codes.ConnectionStringNameRequired,
            message: "EfCore:ConnectionStringName must not be empty.");

        /// <summary>Named connection string not found in ConnectionStrings section.</summary>
        public static Error ConnectionStringNotFound(string name) => Error.BadRequest(
            code: Codes.ConnectionStringNotFound,
            message: $"Connection string '{name}' not found in ConnectionStrings section.");

        /// <summary>Connection string format is invalid.</summary>
        public static Error ConnectionStringInvalid => Error.BadRequest(
            code: Codes.ConnectionStringInvalid,
            message: "Connection string format is invalid.");

        /// <summary>Database connection probe failed.</summary>
        public static Error ConnectionProbeFailed(string reason) => Error.ServiceUnavailable(
            code: Codes.ConnectionProbeFailed,
            message: $"Database connection probe failed: {reason}");

        /// <summary>Provider is not recognized.</summary>
        public static Error ProviderUnsupported(string provider) => Error.BadRequest(
            code: Codes.ProviderUnsupported,
            message: $"The provider '{provider}' is not supported. Use 'PostgreSQL'.");

        /// <summary>Command timeout is outside the allowed range.</summary>
        public static Error CommandTimeoutOutOfRange(int value, int min, int max) => Error.BadRequest(
            code: Codes.CommandTimeoutOutOfRange,
            message: $"EfCore:CommandTimeoutSeconds must be between {min} and {max}, got {value}.");

        /// <summary>Max retry attempts exceeds the limit.</summary>
        public static Error MaxRetryAttemptsExceeded(int value, int max) => Error.BadRequest(
            code: Codes.MaxRetryAttemptsExceeded,
            message: $"EfCore:MaxRetryAttempts must not exceed {max}, got {value}.");

        /// <summary>Retry base delay exceeds the limit.</summary>
        public static Error RetryBaseDelayExceeded(int value, int max) => Error.BadRequest(
            code: Codes.RetryBaseDelayExceeded,
            message: $"EfCore:RetryBaseDelayMs must not exceed {max}, got {value}.");

        /// <summary>Min pool size exceeds max pool size.</summary>
        public static Error PoolSizeInverted => Error.BadRequest(
            code: Codes.PoolSizeInverted,
            message: "EfCore:MinPoolSize must not exceed EfCore:MaxPoolSize.");

        /// <summary>Max pool size exceeds the limit.</summary>
        public static Error MaxPoolSizeExceeded(int value, int max) => Error.BadRequest(
            code: Codes.MaxPoolSizeExceeded,
            message: $"EfCore:MaxPoolSize must not exceed {max}, got {value}.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ConnectionStringNameRequired = "EfCore.ConnectionStringName.Required";
        public const string ConnectionStringNotFound = "EfCore.ConnectionString.NotFound";
        public const string ConnectionStringInvalid = "EfCore.ConnectionString.Invalid";
        public const string ConnectionProbeFailed = "EfCore.Connection.ProbeFailed";
        public const string ProviderUnsupported = "EfCore.Provider.Unsupported";
        public const string CommandTimeoutOutOfRange = "EfCore.CommandTimeout.OutOfRange";
        public const string MaxRetryAttemptsExceeded = "EfCore.MaxRetryAttempts.Exceeded";
        public const string RetryBaseDelayExceeded = "EfCore.RetryBaseDelay.Exceeded";
        public const string PoolSizeInverted = "EfCore.PoolSize.Inverted";
        public const string MaxPoolSizeExceeded = "EfCore.MaxPoolSize.Exceeded";
    }
}
