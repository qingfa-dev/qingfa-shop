using Application.Common.Caching;

using Core.Common.Monad.Results;

using Infrastructure.Caching.Options;
using Infrastructure.Caching.Services;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Caching;

/// <summary>Registers the HybridCache facade over the configured L1 and L2 tiers.</summary>
public static class CachingComposition
{
    /// <summary>Adds the caching facade to the service collection.</summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configuration">Configuration holding the <c>Caching</c> section.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="OptionsValidationException">The configured settings are invalid.</exception>
    /// <exception cref="InvalidOperationException">
    /// The distributed tier is enabled but no <see cref="IDistributedCache"/> is registered.
    /// </exception>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CachingSetting>()
            .Configure(options => configuration
                .GetSection(CachingSettingConstant.SectionName)
                .Bind(options))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<CachingSetting>, CachingSettingValidator>());

        CachingSetting setting = configuration
            .GetSection(CachingSettingConstant.SectionName)
            .Get<CachingSetting>() ?? new CachingSetting();

        // Guard: a disabled facade must still be resolvable, but it must not pay for a cache.
        if (!setting.Enabled)
        {
            services.TryAddSingleton<ICacheService, NullCacheService>();
            return services;
        }

        // Validate: enabling L2 without a provider would silently collapse the facade to L1.
        if (setting.Distributed.Enabled &&
            !services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedCache)))
        {
            throw new InvalidOperationException(
                $"{CachingSettingOutcome.Failure.DistributedProviderMissing.Code}: " +
                CachingSettingOutcome.Failure.DistributedProviderMissing.Message);
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