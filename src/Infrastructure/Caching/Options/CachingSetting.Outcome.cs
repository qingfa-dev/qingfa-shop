using Core.Common.Monad.Errors;

namespace Infrastructure.Caching;

/// <summary>Failure outcomes for the root caching configuration.</summary>
public static class CachingSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        /// <summary>The facade is enabled but neither tier is.</summary>
        public static Error TierRequired => Error.BadRequest(
            code: "Caching.Tier.Required",
            message: "At least one cache tier (Memory or Distributed) must be enabled while Caching:Enabled is true.");

        /// <summary>The L1 lifetime would outlive the L2 lifetime.</summary>
        public static Error ExpirationInverted => Error.BadRequest(
            code: "Caching.Expiration.Inverted",
            message: "Caching:Memory:DefaultExpirationMinutes must not exceed Caching:Distributed:DefaultExpirationMinutes.");

        /// <summary>The distributed tier is enabled without a registered provider.</summary>
        public static Error DistributedProviderMissing => Error.InternalServerError(
            code: "Caching.Distributed.Provider.Missing",
            message: "Caching:Distributed is enabled but no IDistributedCache is registered. " +
                     "Register the distributed provider (for example AddRedisCache) before AddCaching.");
    }
}