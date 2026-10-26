namespace Infrastructure.Caching.Options.Hybrid;

/// <summary>Failure outcomes for the HybridCache facade limits.</summary>
public static class HybridCacheSettingOutcome
{
    /// <summary>Failure outcomes.</summary>
    public static class Failure
    {
        /// <summary>The payload limit is outside its accepted range.</summary>
        public static Error PayloadOutOfRange => Error.BadRequest(
            code: "Caching.Hybrid.Payload.OutOfRange",
            message: $"Caching:Hybrid:MaximumPayloadBytes must be between " +
                     $"{HybridCacheSettingConstant.Constraints.MaximumPayloadBytesMin} and " +
                     $"{HybridCacheSettingConstant.Constraints.MaximumPayloadBytesMax} bytes.");

        /// <summary>The key length limit is outside its accepted range.</summary>
        public static Error KeyLengthOutOfRange => Error.BadRequest(
            code: "Caching.Hybrid.KeyLength.OutOfRange",
            message: $"Caching:Hybrid:MaximumKeyLength must be between " +
                     $"{HybridCacheSettingConstant.Constraints.MaximumKeyLengthMin} and " +
                     $"{HybridCacheSettingConstant.Constraints.MaximumKeyLengthMax} characters.");
    }
}