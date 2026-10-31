namespace Infrastructure.BuildingBlocks.Adapters.Caching.Options.Hybrid;

/// <summary>Failure outcomes for the HybridCache facade limits.</summary>
public static class HybridCacheSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        /// <summary>The payload limit is outside its accepted range.</summary>
        public static Error PayloadOutOfRange => Error.BadRequest(
            code: Codes.PayloadOutOfRange,
            message: $"Caching:Hybrid:MaximumPayloadBytes must be between " +
                     $"{HybridCacheSettingConstant.Constraints.MaximumPayloadBytesMin} and " +
                     $"{HybridCacheSettingConstant.Constraints.MaximumPayloadBytesMax} bytes.");

        /// <summary>The key length limit is outside its accepted range.</summary>
        public static Error KeyLengthOutOfRange => Error.BadRequest(
            code: Codes.KeyLengthOutOfRange,
            message: $"Caching:Hybrid:MaximumKeyLength must be between " +
                     $"{HybridCacheSettingConstant.Constraints.MaximumKeyLengthMin} and " +
                     $"{HybridCacheSettingConstant.Constraints.MaximumKeyLengthMax} characters.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string PayloadOutOfRange = "Caching.Hybrid.Payload.OutOfRange";
        public const string KeyLengthOutOfRange = "Caching.Hybrid.KeyLength.OutOfRange";
    }
}