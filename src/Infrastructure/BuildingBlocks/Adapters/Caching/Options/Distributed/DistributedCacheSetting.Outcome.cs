namespace Infrastructure.BuildingBlocks.Adapters.Caching.Options.Distributed;

/// <summary>Failure outcomes for the L2 distributed cache options.</summary>
public static class DistributedCacheSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        public static Error TypeRequired => Error.BadRequest(
            code: Codes.TypeRequired,
            message: "Distributed cache provider is required when distributed caching is enabled.");

        public static Error TypeInvalid => Error.BadRequest(
            code: Codes.TypeInvalid,
            message: $"Distributed cache provider must be one of: '{string.Join("', '", DistributedCacheSettingConstant.Patterns.ValidTypes)}'.");

        public static Error ConnectionStringNameRequired => Error.BadRequest(
            code: Codes.ConnectionStringNameRequired,
            message: "Distributed cache ConnectionStringName is required when distributed caching is enabled.");

        public static Error ConnectionStringNotFound(string name) => Error.BadRequest(
            code: Codes.ConnectionStringNotFound,
            message: $"Connection string '{name}' not found in ConnectionStrings section.");

        public static Error ExpirationOutOfRange => Error.BadRequest(
            code: Codes.ExpirationOutOfRange,
            message: $"Caching:Distributed:DefaultExpirationMinutes must be between " +
                     $"{DistributedCacheSettingConstant.Constraints.ExpirationMinutesMin} and " +
                     $"{DistributedCacheSettingConstant.Constraints.ExpirationMinutesMax} minutes.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string TypeRequired = "Caching.Distributed.Type.Required";
        public const string TypeInvalid = "Caching.Distributed.Type.Invalid";
        public const string ConnectionStringNameRequired = "Caching.Distributed.ConnectionStringName.Required";
        public const string ConnectionStringNotFound = "Caching.Distributed.ConnectionString.NotFound";
        public const string ExpirationOutOfRange = "Caching.Distributed.Expiration.OutOfRange";
    }
}