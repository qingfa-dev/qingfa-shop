namespace Infrastructure.BuildingBlocks.Adapters.Caching.Options;

/// <summary>Failure outcomes for the root caching configuration.</summary>
public static class CachingSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        /// <summary>The facade is enabled but neither tier is.</summary>
        public static Error TierRequired => Error.BadRequest(
            code: Codes.TierRequired,
            message: "At least one cache tier (Memory or Distributed) must be enabled while Caching:Enabled is true.");

        /// <summary>The L1 lifetime would outlive the L2 lifetime.</summary>
        public static Error ExpirationInverted => Error.BadRequest(
            code: Codes.ExpirationInverted,
            message: "Caching:Memory:DefaultExpirationMinutes must not exceed Caching:Distributed:DefaultExpirationMinutes.");

        /// <summary>The distributed tier is enabled without a registered provider.</summary>
        public static Error DistributedProviderMissing => Error.InternalServerError(
            code: Codes.DistributedProviderMissing,
            message: "Caching:Distributed is enabled but no IDistributedCache is registered. " +
                     "Register the distributed provider (for example AddRedisCache) before AddCaching.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string TierRequired = "Caching.Tier.Required";
        public const string ExpirationInverted = "Caching.Expiration.Inverted";
        public const string DistributedProviderMissing = "Caching.Distributed.Provider.Missing";
    }
}