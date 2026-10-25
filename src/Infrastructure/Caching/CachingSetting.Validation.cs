using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Infrastructure.Caching.Options;

namespace Infrastructure.Caching;

/// <summary>Validates caching settings and returns the monad's <see cref="Result{TValue}"/>.</summary>
/// <remarks>
/// The composition uses this method to fail fast, and <see cref="CachingSettingValidator"/>
/// adapts the same errors to the options pipeline.
/// </remarks>
public static class CachingSettingValidation
{
    /// <summary>Validates the complete caching configuration.</summary>
    /// <param name="setting">The settings to validate.</param>
    /// <returns>
    /// The unchanged settings on success; otherwise a failure carrying every broken rule.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <c>null</c>.</exception>
    public static Result<CachingSetting> Validate(CachingSetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        // Guard: a host that turns caching off does not need any tier configured.
        if (!setting.Enabled)
        {
            return Result<CachingSetting>.Success(setting);
        }

        // Compute: validate each tier independently, then the rules that span tiers.
        Result combined = ResultExtension.Combine(
            ValidateMemory(setting.Memory),
            ValidateDistributed(setting.Distributed),
            ValidateHybrid(setting.Hybrid),
            ValidateTiers(setting));

        // Return: preserve every error the combined result collected.
        return combined.IsSuccess
            ? Result<CachingSetting>.Success(setting)
            : Result<CachingSetting>.Fail(combined.Errors, combined.StatusCode);
    }

    /// <summary>Formats a failed result as the messages the options pipeline reports.</summary>
    /// <param name="result">The result to describe.</param>
    /// <returns>One <c>Code: Message</c> entry per error.</returns>
    public static IEnumerable<string> Describe(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Errors.Select(error => $"{error.Code}: {error.Message}");
    }

    private static Result ValidateMemory(MemoryCacheSetting memory)
    {
        if (!memory.Enabled)
        {
            return Result.Success();
        }

        var errors = new List<Error>();

        if (memory.DefaultExpirationMinutes is < MemoryCacheSettingConstant.Constraints.ExpirationMinutesMin
            or > MemoryCacheSettingConstant.Constraints.ExpirationMinutesMax)
        {
            errors.Add(MemoryCacheSettingOutcome.Failure.ExpirationOutOfRange);
        }

        if (memory.CompactionPercentage is < MemoryCacheSettingConstant.Constraints.CompactionPercentageMin
            or > MemoryCacheSettingConstant.Constraints.CompactionPercentageMax)
        {
            errors.Add(MemoryCacheSettingOutcome.Failure.CompactionOutOfRange);
        }

        if (memory.SizeLimitBytes is < MemoryCacheSettingConstant.Constraints.SizeLimitBytesMin)
        {
            errors.Add(MemoryCacheSettingOutcome.Failure.SizeLimitOutOfRange);
        }

        return errors.Count == 0 ? Result.Success() : Result.Fail(errors);
    }

    private static Result ValidateDistributed(DistributedCacheSetting distributed)
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

    private static Result ValidateHybrid(HybridCacheSetting hybrid)
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

    private static Result ValidateTiers(CachingSetting setting)
    {
        var errors = new List<Error>();

        // Validate: the facade needs at least one tier to read from or write to.
        if (!setting.Memory.Enabled && !setting.Distributed.Enabled)
        {
            errors.Add(CachingSettingOutcome.Failure.TierRequired);
        }

        // Validate: an L1 entry that outlives its L2 entry would be resurrected from the
        // stale local copy after the distributed entry has already expired.
        if (setting.Memory.Enabled &&
            setting.Distributed.Enabled &&
            setting.Memory.DefaultExpirationMinutes > setting.Distributed.DefaultExpirationMinutes)
        {
            errors.Add(CachingSettingOutcome.Failure.ExpirationInverted);
        }

        return errors.Count == 0 ? Result.Success() : Result.Fail(errors);
    }
}