namespace Infrastructure.Caching.Options;

/// <summary>Options for the L2 distributed cache.</summary>
/// <remarks>
/// The provider itself is registered by its own composition (currently
/// <c>AddRedisCache</c>); this type only describes how the hybrid facade uses it.
/// </remarks>
public sealed class DistributedCacheSetting
{
    /// <summary>Enables the L2 tier.</summary>
    public bool Enabled { get; set; } = DistributedCacheSettingConstant.Defaults.Enabled;

    /// <summary>Lifetime of an entry in the L2 cache, in minutes.</summary>
    public int DefaultExpirationMinutes { get; set; } =
        DistributedCacheSettingConstant.Defaults.ExpirationMinutes;

    /// <summary>The configured L2 lifetime as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan DefaultExpiration => TimeSpan.FromMinutes(DefaultExpirationMinutes);
}