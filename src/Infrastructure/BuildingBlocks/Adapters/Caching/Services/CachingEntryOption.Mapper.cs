using Application.BuildingBlocks.Caching;

using Microsoft.Extensions.Caching.Hybrid;

namespace Infrastructure.BuildingBlocks.Adapters.Caching.Services;

/// <summary>Maps the application's provider-neutral cache options onto HybridCache options.</summary>
internal static class CachingEntryOptionMapper
{
    /// <summary>Converts an application entry option into a HybridCache entry option.</summary>
    /// <param name="option">The application option, or <c>null</c> for the configured defaults.</param>
    /// <returns>The HybridCache option, or <c>null</c> when no override was supplied.</returns>
    public static HybridCacheEntryOptions? ToHybridCacheEntryOptions(
        this CachingEntryOption? option)
    {
        if (option is null)
        {
            return null;
        }

        HybridCacheEntryFlags flags = HybridCacheEntryFlags.None;
        if (option.DisableLocalCache)
        {
            flags |= HybridCacheEntryFlags.DisableLocalCache;
        }

        if (option.DisableDistributedCache)
        {
            flags |= HybridCacheEntryFlags.DisableDistributedCache;
        }

        // Map: the application never sees HybridCacheEntryOptions or its flags enum.
        return new HybridCacheEntryOptions
        {
            Expiration = option.Expiration,
            LocalCacheExpiration = option.LocalCacheExpiration,
            Flags = flags == HybridCacheEntryFlags.None ? null : flags
        };
    }
}