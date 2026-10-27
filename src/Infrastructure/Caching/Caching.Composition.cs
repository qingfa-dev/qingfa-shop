using Application.Common.Caching;

using Infrastructure.Caching.Options;
using Infrastructure.Caching.Services;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Caching;

/// <summary>Registers the HybridCache facade over the configured L1 and L2 tiers.</summary>
public static class CachingComposition
{
    /// <summary>Adds the caching facade to the service collection.</summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configuration">Configuration holding the <c>Caching</c> section.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// The configured settings are invalid or the distributed tier is enabled without a provider.
    /// </exception>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        CachingSetting setting = configuration
            .GetSection(CachingSettingConstant.SectionName)
            .Get<CachingSetting>() ?? new CachingSetting();

        // Validate: fail fast if settings are invalid (including connection string resolution)
        Result<CachingSetting> validationResult = CachingSettingValidation
            .ValidateWithConnectionString(setting, configuration);
        if (validationResult.IsFailure)
        {
            throw new InvalidOperationException(
                string.Join("; ", CachingSettingValidation.Describe(validationResult)));
        }

        // Register the setting as both a singleton and via IOptions for flexibility
        services.TryAddSingleton(setting);
        services.Configure<CachingSetting>(options =>
        {
            options.Enabled = setting.Enabled;
            options.Memory = setting.Memory;
            options.Distributed = setting.Distributed;
            options.Hybrid = setting.Hybrid;
        });

        // Guard: a disabled facade must still be resolvable, but it must not pay for a cache.
        if (!setting.Enabled)
        {
            services.TryAddSingleton<ICacheService, NullCacheService>();
            return services;
        }

        // Validate: enabling L2 without a provider would silently collapse the facade to L1.
        if (setting.Distributed.Enabled)
        {
            // Register the distributed cache provider based on configuration
            string? connectionString = configuration.GetConnectionString(setting.Distributed.ConnectionStringName!);
            switch (setting.Distributed.Provider?.ToLowerInvariant())
            {
                case "redis":
                    // Check if IDistributedCache is already registered (e.g., by test fixtures)
                    if (!services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedCache)))
                    {
                        services.AddStackExchangeRedisCache(options =>
                        {
                            options.Configuration = connectionString;
                        });
                    }
                    break;

                case "memory":
                    // IDistributedMemoryCache should be pre-registered by the host (e.g., tests)
                    if (!services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedCache)))
                    {
                        services.AddDistributedMemoryCache();
                    }
                    break;

                default:
                    // Check if IDistributedCache is already registered
                    if (!services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedCache)))
                    {
                        throw new InvalidOperationException(
                            $"{CachingSettingOutcome.Failure.DistributedProviderMissing.Code}: " +
                            CachingSettingOutcome.Failure.DistributedProviderMissing.Message);
                    }
                    break;
            }
        }

        // Initialize: the L1 cache is registered here because its limits belong to the cache stack.
        if (setting.Memory.Enabled)
        {
            services.AddMemoryCache(options =>
            {
                options.CompactionPercentage = setting.Memory.CompactionPercentage / 100.0;
                options.SizeLimit = setting.Memory.SizeLimitBytes;
            });
        }

        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = setting.Hybrid.MaximumPayloadBytes;
            options.MaximumKeyLength = setting.Hybrid.MaximumKeyLength;

            HybridCacheEntryFlags flags = HybridCacheEntryFlags.None;
            if (!setting.Memory.Enabled)
            {
                flags |= HybridCacheEntryFlags.DisableLocalCache;
            }

            if (!setting.Distributed.Enabled)
            {
                flags |= HybridCacheEntryFlags.DisableDistributedCache;
            }

            // Configure: the overall entry lifetime follows L2, while the local lifetime follows L1.
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = setting.Distributed.Enabled
                    ? setting.Distributed.DefaultExpiration
                    : setting.Memory.DefaultExpiration,
                LocalCacheExpiration = setting.Memory.Enabled
                    ? setting.Memory.DefaultExpiration
                    : null,
                Flags = flags == HybridCacheEntryFlags.None ? null : flags
            };
        });

        services.TryAddSingleton<ICacheService, CacheService>();

        return services;
    }
}