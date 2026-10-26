using Core.Common.Monad.Errors;

using Infrastructure.Caching.Options.Distributed;

namespace Infrastructure.Caching.Options;

/// <summary>Failure outcomes for the L2 distributed cache options.</summary>
public static class DistributedCacheSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        /// <summary>The entry lifetime is outside its accepted range.</summary>
        public static Error ExpirationOutOfRange => Error.BadRequest(
            code: "Caching.Distributed.Expiration.OutOfRange",
            message: $"Caching:Distributed:DefaultExpirationMinutes must be between " +
                     $"{DistributedCacheSettingConstant.Constraints.ExpirationMinutesMin} and " +
                     $"{DistributedCacheSettingConstant.Constraints.ExpirationMinutesMax} minutes.");
    }
}