using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

namespace Infrastructure.Caching.Options.Memory;

/// <summary>Validates caching settings and returns the monad's <see cref="Result{TValue}"/>.</summary>
/// <remarks>
/// The composition uses this method to fail fast, and <see cref="CachingSettingValidator"/>
/// adapts the same errors to the options pipeline.
/// </remarks>
internal static class MemoryCacheSettingValidation
{
    public static Result ValidateMemory(MemoryCacheSetting memory)
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
}