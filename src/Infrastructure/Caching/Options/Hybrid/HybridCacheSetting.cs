namespace Infrastructure.Caching.Options.Hybrid;

/// <summary>Limits shared by the HybridCache facade across both tiers.</summary>
public sealed class HybridCacheSetting
{
    /// <summary>Largest payload, in bytes, that a single cache entry may hold.</summary>
    public long MaximumPayloadBytes { get; set; } =
        HybridCacheSettingConstant.Defaults.MaximumPayloadBytes;

    /// <summary>Longest cache key, in characters, that the facade accepts.</summary>
    public int MaximumKeyLength { get; set; } =
        HybridCacheSettingConstant.Defaults.MaximumKeyLength;
}