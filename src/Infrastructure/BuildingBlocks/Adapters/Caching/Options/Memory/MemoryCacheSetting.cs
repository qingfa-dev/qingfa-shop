namespace Infrastructure.BuildingBlocks.Adapters.Caching.Options.Memory;

/// <summary>Options for the L1 in-process memory cache.</summary>
public sealed class MemoryCacheSetting
{
    /// <summary>Enables the L1 tier.</summary>
    public bool Enabled { get; set; } = MemoryCacheSettingConstant.Defaults.Enabled;

    /// <summary>Lifetime of an entry in the L1 cache, in minutes.</summary>
    public int DefaultExpirationMinutes { get; set; } =
        MemoryCacheSettingConstant.Defaults.ExpirationMinutes;

    /// <summary>Percentage of the cache reclaimed when memory pressure is detected.</summary>
    public int CompactionPercentage { get; set; } =
        MemoryCacheSettingConstant.Defaults.CompactionPercentage;

    /// <summary>Maximum size of the memory cache in bytes; <c>null</c> leaves it unbounded.</summary>
    public long? SizeLimitBytes { get; set; } = MemoryCacheSettingConstant.Defaults.SizeLimitBytes;

    /// <summary>The configured L1 lifetime as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan DefaultExpiration => TimeSpan.FromMinutes(DefaultExpirationMinutes);
}