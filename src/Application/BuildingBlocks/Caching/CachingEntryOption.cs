namespace Application.BuildingBlocks.Caching;

/// <summary>Provider-neutral overrides for a single cache entry.</summary>
/// <remarks>
/// The adapter maps these values onto its own provider options, so callers never
/// name HybridCache or Redis types.
/// </remarks>
public sealed class CachingEntryOption
{
    /// <summary>Overall entry lifetime across both tiers.</summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>Lifetime in the local (L1) tier; must not exceed <see cref="Expiration"/>.</summary>
    public TimeSpan? LocalCacheExpiration { get; set; }

    /// <summary>Skips the local tier for this entry.</summary>
    public bool DisableLocalCache { get; set; }

    /// <summary>Skips the distributed tier for this entry.</summary>
    public bool DisableDistributedCache { get; set; }
}