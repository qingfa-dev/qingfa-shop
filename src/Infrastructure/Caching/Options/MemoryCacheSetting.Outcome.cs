using Core.Common.Monad.Errors;

namespace Infrastructure.Caching.Options;

/// <summary>Failure outcomes for the L1 memory cache options.</summary>
public static class MemoryCacheSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        /// <summary>The entry lifetime is outside its accepted range.</summary>
        public static Error ExpirationOutOfRange => Error.BadRequest(
            code: "Caching.Memory.Expiration.OutOfRange",
            message: $"Caching:Memory:DefaultExpirationMinutes must be between " +
                     $"{MemoryCacheSettingConstant.Constraints.ExpirationMinutesMin} and " +
                     $"{MemoryCacheSettingConstant.Constraints.ExpirationMinutesMax} minutes.");

        /// <summary>The compaction percentage is outside its accepted range.</summary>
        public static Error CompactionOutOfRange => Error.BadRequest(
            code: "Caching.Memory.Compaction.OutOfRange",
            message: $"Caching:Memory:CompactionPercentage must be between " +
                     $"{MemoryCacheSettingConstant.Constraints.CompactionPercentageMin} and " +
                     $"{MemoryCacheSettingConstant.Constraints.CompactionPercentageMax}.");

        /// <summary>The size limit is below one byte.</summary>
        public static Error SizeLimitOutOfRange => Error.BadRequest(
            code: "Caching.Memory.SizeLimit.OutOfRange",
            message: $"Caching:Memory:SizeLimitBytes must be at least " +
                     $"{MemoryCacheSettingConstant.Constraints.SizeLimitBytesMin} byte(s), or null.");
    }
}