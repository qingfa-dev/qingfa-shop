using Application.Common.Caching;

using Infrastructure.Caching.Options;

namespace Infrastructure.Caching;

/// <summary>Options for the L1/L2 caching stack exposed through <see cref="ICacheService"/>.</summary>
public sealed class CachingSetting
{
    /// <summary>Turns the cache facade on or off for the host.</summary>
    public bool Enabled { get; set; } = CachingSettingConstant.Defaults.Enabled;

    /// <summary>The L1 (in-process memory) tier.</summary>
    public MemoryCacheSetting Memory { get; set; } = new();

    /// <summary>The L2 (distributed) tier.</summary>
    public DistributedCacheSetting Distributed { get; set; } = new();

    /// <summary>The HybridCache limits shared by both tiers.</summary>
    public HybridCacheSetting Hybrid { get; set; } = new();
}