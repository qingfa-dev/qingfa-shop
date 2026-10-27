namespace Infrastructure.Persistence.EntityFramework.Options;

/// <summary>Failure outcomes for persistence configuration validation.</summary>
public static class PersistenceSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        /// <summary>Connection string name is missing or empty.</summary>
        public static Error ConnectionStringNameRequired => Error.BadRequest(
            code: "EfCore.ConnectionStringName.Required",
            message: "EfCore:ConnectionStringName must not be empty.");

        /// <summary>Named connection string not found in ConnectionStrings section.</summary>
        public static Error ConnectionStringNotFound(string name) => Error.BadRequest(
            code: "EfCore.ConnectionString.NotFound",
            message: $"Connection string '{name}' not found in ConnectionStrings section.");

        /// <summary>Connection string format is invalid.</summary>
        public static Error ConnectionStringInvalid => Error.BadRequest(
            code: "EfCore.ConnectionString.Invalid",
            message: "Connection string format is invalid.");

        /// <summary>Database connection probe failed.</summary>
        public static Error ConnectionProbeFailed(string reason) => Error.ServiceUnavailable(
            code: "EfCore.Connection.ProbeFailed",
            message: $"Database connection probe failed: {reason}");

        /// <summary>Provider is not recognized.</summary>
        public static Error ProviderUnsupported(string provider) => Error.BadRequest(
            code: "EfCore.Provider.Unsupported",
            message: $"The provider '{provider}' is not supported. Use 'PostgreSQL'.");

        /// <summary>Command timeout is outside the allowed range.</summary>
        public static Error CommandTimeoutOutOfRange(int value, int min, int max) => Error.BadRequest(
            code: "EfCore.CommandTimeout.OutOfRange",
            message: $"EfCore:CommandTimeoutSeconds must be between {min} and {max}, got {value}.");

        /// <summary>Max retry attempts exceeds the limit.</summary>
        public static Error MaxRetryAttemptsExceeded(int value, int max) => Error.BadRequest(
            code: "EfCore.MaxRetryAttempts.Exceeded",
            message: $"EfCore:MaxRetryAttempts must not exceed {max}, got {value}.");

        /// <summary>Retry base delay exceeds the limit.</summary>
        public static Error RetryBaseDelayExceeded(int value, int max) => Error.BadRequest(
            code: "EfCore.RetryBaseDelay.Exceeded",
            message: $"EfCore:RetryBaseDelayMs must not exceed {max}, got {value}.");

        /// <summary>Min pool size exceeds max pool size.</summary>
        public static Error PoolSizeInverted => Error.BadRequest(
            code: "EfCore.PoolSize.Inverted",
            message: "EfCore:MinPoolSize must not exceed EfCore:MaxPoolSize.");

        /// <summary>Max pool size exceeds the limit.</summary>
        public static Error MaxPoolSizeExceeded(int value, int max) => Error.BadRequest(
            code: "EfCore.MaxPoolSize.Exceeded",
            message: $"EfCore:MaxPoolSize must not exceed {max}, got {value}.");
    }
}
