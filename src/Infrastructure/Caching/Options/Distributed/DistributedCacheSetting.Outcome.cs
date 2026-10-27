namespace Infrastructure.Caching.Options.Distributed;

/// <summary>Failure outcomes for the L2 distributed cache options.</summary>
public static class DistributedCacheSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        public static Error TypeRequired => Error.BadRequest(
            code: "Caching.Distributed.Type.Required",
            message: "Distributed cache provider is required when distributed caching is enabled.");

        public static Error TypeInvalid => Error.BadRequest(
            code: "Caching.Distributed.Type.Invalid",
            message: $"Distributed cache provider must be one of: '{string.Join("', '", DistributedCacheSettingConstant.Patterns.ValidTypes)}'.");

        public static Error ConnectionStringNameRequired => Error.BadRequest(
            code: "Caching.Distributed.ConnectionStringName.Required",
            message: "Distributed cache ConnectionStringName is required when distributed caching is enabled.");

        public static Error ConnectionStringNotFound(string name) => Error.BadRequest(
            code: "Caching.Distributed.ConnectionString.NotFound",
            message: $"Connection string '{name}' not found in ConnectionStrings section.");

        public static Error ExpirationOutOfRange => Error.BadRequest(
            code: "Caching.Distributed.Expiration.OutOfRange",
            message: $"Caching:Distributed:DefaultExpirationMinutes must be between " +
                     $"{DistributedCacheSettingConstant.Constraints.ExpirationMinutesMin} and " +
                     $"{DistributedCacheSettingConstant.Constraints.ExpirationMinutesMax} minutes.");
    }
}