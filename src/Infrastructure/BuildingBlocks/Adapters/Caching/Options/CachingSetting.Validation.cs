using Infrastructure.BuildingBlocks.Adapters.Caching.Options.Distributed;
using Infrastructure.BuildingBlocks.Adapters.Caching.Options.Hybrid;
using Infrastructure.BuildingBlocks.Adapters.Caching.Options.Memory;

using Microsoft.Extensions.Configuration;

namespace Infrastructure.BuildingBlocks.Adapters.Caching.Options;

/// <summary>Validates caching settings and returns the monad's <see cref="Result{TValue}"/>.</summary>
/// <remarks>
/// The composition uses this method to fail fast at startup.
/// </remarks>
public static class CachingSettingValidation
{
    /// <summary>Validates the complete caching configuration (static rules only).</summary>
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
            MemoryCacheSettingValidation.ValidateMemory(setting.Memory),
            DistributedCacheSettingValidation.ValidateDistributed(setting.Distributed),
            HybridCacheSettingValidation.ValidateHybrid(setting.Hybrid),
            ValidateTiers(setting));

        // Return: preserve every error the combined result collected.
        return combined.IsSuccess
            ? Result<CachingSetting>.Success(setting)
            : Result<CachingSetting>.Fail(combined.Errors, combined.StatusCode);
    }

    /// <summary>
    /// Validates the complete caching configuration including connection string resolution.
    /// </summary>
    /// <param name="setting">The settings to validate.</param>
    /// <param name="configuration">The configuration to resolve connection strings from.</param>
    /// <returns>
    /// The unchanged settings on success; otherwise a failure carrying every broken rule.
    /// </returns>
    public static Result<CachingSetting> ValidateWithConnectionString(
        CachingSetting setting, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // First validate static rules
        Result<CachingSetting> staticResult = Validate(setting);
        if (staticResult.IsFailure)
        {
            return staticResult;
        }

        // Validate distributed cache connection string resolution
        Result distributedResult = DistributedCacheSettingValidation
            .ValidateDistributedWithConnectionString(setting.Distributed, configuration);
        if (distributedResult.IsFailure)
        {
            return Result<CachingSetting>.Fail(distributedResult.Errors, distributedResult.StatusCode);
        }

        return Result<CachingSetting>.Success(setting);
    }

    /// <summary>Formats a failed result as the messages the options pipeline reports.</summary>
    /// <param name="result">The result to describe.</param>
    /// <returns>One <c>Code: Message</c> entry per error.</returns>
    public static IEnumerable<string> Describe(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Errors.Select(error => $"{error.Code}: {error.Message}");
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