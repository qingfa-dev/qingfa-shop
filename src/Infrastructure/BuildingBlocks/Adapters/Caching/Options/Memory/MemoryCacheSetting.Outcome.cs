namespace Infrastructure.BuildingBlocks.Adapters.Caching.Options.Memory;

/// <summary>Failure outcomes for the L1 memory cache options.</summary>
public static class MemoryCacheSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        /// <summary>The entry lifetime is outside its accepted range.</summary>
        public static Error ExpirationOutOfRange => Error.BadRequest(
            code: Codes.ExpirationOutOfRange,
            message: $"Caching:Memory:DefaultExpirationMinutes must be between " +
                     $"{MemoryCacheSettingConstant.Constraints.ExpirationMinutesMin} and " +
                     $"{MemoryCacheSettingConstant.Constraints.ExpirationMinutesMax} minutes.");

        /// <summary>The compaction percentage is outside its accepted range.</summary>
        public static Error CompactionOutOfRange => Error.BadRequest(
            code: Codes.CompactionOutOfRange,
            message: $"Caching:Memory:CompactionPercentage must be between " +
                     $"{MemoryCacheSettingConstant.Constraints.CompactionPercentageMin} and " +
                     $"{MemoryCacheSettingConstant.Constraints.CompactionPercentageMax}.");

        /// <summary>The size limit is below one byte.</summary>
        public static Error SizeLimitOutOfRange => Error.BadRequest(
            code: Codes.SizeLimitOutOfRange,
            message: $"Caching:Memory:SizeLimitBytes must be at least " +
                     $"{MemoryCacheSettingConstant.Constraints.SizeLimitBytesMin} byte(s), or null.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ExpirationOutOfRange = "Caching.Memory.Expiration.OutOfRange";
        public const string CompactionOutOfRange = "Caching.Memory.Compaction.OutOfRange";
        public const string SizeLimitOutOfRange = "Caching.Memory.SizeLimit.OutOfRange";
    }
}