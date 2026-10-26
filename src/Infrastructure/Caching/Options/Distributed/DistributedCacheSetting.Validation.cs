using Core.Common.Monad.Results;

namespace Infrastructure.Caching.Options.Distributed;

/// <summary>Validates caching settings and returns the monad's <see cref="Result{TValue}"/>.</summary>
/// <remarks>
/// The composition uses this method to fail fast, and <see cref="CachingSettingValidator"/>
/// adapts the same errors to the options pipeline.
/// </remarks>
internal static class DistributedCacheSettingValidation
{

    public static Result ValidateDistributed(DistributedCacheSetting distributed)
    {
        if (!distributed.Enabled)
        {
            return Result.Success();
        }

        if (distributed.DefaultExpirationMinutes is < DistributedCacheSettingConstant.Constraints.ExpirationMinutesMin
            or > DistributedCacheSettingConstant.Constraints.ExpirationMinutesMax)
        {
            return Result.Fail(
                DistributedCacheSettingOutcome.Failure.ExpirationOutOfRange);
        }

        return Result.Success();
    }
}