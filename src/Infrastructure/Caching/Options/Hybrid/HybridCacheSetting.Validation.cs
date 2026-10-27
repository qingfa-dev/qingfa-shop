namespace Infrastructure.Caching.Options.Hybrid;

/// <summary>Validates hybrid cache settings and returns the monad's <see cref="Result{TValue}"/>.</summary>
/// <remarks>
/// The composition uses this method to fail fast at startup.
/// </remarks>
internal static class HybridCacheSettingValidation
{
    public static Result ValidateHybrid(HybridCacheSetting hybrid)
    {
        var errors = new List<Error>();

        if (hybrid.MaximumPayloadBytes is < HybridCacheSettingConstant.Constraints.MaximumPayloadBytesMin
            or > HybridCacheSettingConstant.Constraints.MaximumPayloadBytesMax)
        {
            errors.Add(HybridCacheSettingOutcome.Failure.PayloadOutOfRange);
        }

        if (hybrid.MaximumKeyLength is < HybridCacheSettingConstant.Constraints.MaximumKeyLengthMin
            or > HybridCacheSettingConstant.Constraints.MaximumKeyLengthMax)
        {
            errors.Add(HybridCacheSettingOutcome.Failure.KeyLengthOutOfRange);
        }

        return errors.Count == 0 ? Result.Success() : Result.Fail(errors);
    }
}