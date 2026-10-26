using Application.Common.Caching;

using Infrastructure.Caching;
using Infrastructure.Caching.Options;
using Infrastructure.Caching.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration.Tests.Caching;

/// <summary>
/// Integration tests for <see cref="CachingComposition.AddCaching"/> that verify
/// the full DI pipeline: configuration binding, validation, and service resolution.
/// </summary>
[Collection(CachingCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingCompositionIntegrationTest
{
    readonly CachingFixture _fixture;

    public CachingCompositionIntegrationTest(CachingFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "ICacheService should resolve as CacheService when enabled")]
    public void ICacheService_ShouldResolveAsCacheService()
    {
        var cache = _fixture.Services.GetRequiredService<ICacheService>();

        cache.Should().BeOfType<CacheService>();
    }

    [Fact(DisplayName = "CachingSetting should bind correctly from configuration")]
    public void CachingSetting_ShouldBindCorrectly()
    {
        var settings = _fixture.Settings;

        settings.Enabled.Should().BeTrue();
        settings.Memory.Enabled.Should().BeTrue();
        settings.Distributed.Enabled.Should().BeTrue();
        settings.Memory.DefaultExpirationMinutes.Should().Be(1);
        settings.Distributed.DefaultExpirationMinutes.Should().Be(5);
    }

    [Fact(DisplayName = "HybridCache should be registered when both tiers are enabled")]
    public void HybridCache_ShouldBeRegistered()
    {
        var hybridCache = _fixture.Services.GetService<Microsoft.Extensions.Caching.Hybrid.HybridCache>();

        hybridCache.Should().NotBeNull();
    }

    [Fact(DisplayName = "IMemoryCache should be registered when memory tier is enabled")]
    public void IMemoryCache_ShouldBeRegistered()
    {
        var memoryCache = _fixture.Services.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();

        memoryCache.Should().NotBeNull();
    }

    [Fact(DisplayName = "IDistributedCache should be registered")]
    public void IDistributedCache_ShouldBeRegistered()
    {
        var distributedCache = _fixture.Services.GetService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();

        distributedCache.Should().NotBeNull();
    }

    [Fact(DisplayName = "ICacheService should be a singleton")]
    public void ICacheService_ShouldBeSingleton()
    {
        var cache1 = _fixture.Services.GetRequiredService<ICacheService>();
        var cache2 = _fixture.Services.GetRequiredService<ICacheService>();

        cache1.Should().BeSameAs(cache2);
    }

    [Fact(DisplayName = "Disabled caching should register NullCacheService")]
    public void DisabledCaching_ShouldRegisterNullCacheService()
    {
        var services = new ServiceCollection();
        services.AddCaching(BuildConfig(new Dictionary<string, string?>
        {
            ["Caching:Enabled"] = "false"
        }));

        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();

        cache.Should().BeOfType<NullCacheService>();
    }

    [Fact(DisplayName = "Disabled caching should still allow GetOrCreateAsync")]
    public async Task DisabledCaching_ShouldAllowGetOrCreateAsync()
    {
        var services = new ServiceCollection();
        services.AddCaching(BuildConfig(new Dictionary<string, string?>
        {
            ["Caching:Enabled"] = "false"
        }));

        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();

        int calls = 0;
        string first = await cache.GetOrCreateAsync("disabled:key", _ =>
        {
            calls++;
            return ValueTask.FromResult("value");
        });
        string second = await cache.GetOrCreateAsync("disabled:key", _ =>
        {
            calls++;
            return ValueTask.FromResult("value");
        });

        first.Should().Be("value");
        second.Should().Be("value");
        calls.Should().Be(2, "NullCacheService always invokes the factory");
    }

    [Fact(DisplayName = "Distributed enabled without provider should throw")]
    public void DistributedEnabledWithoutProvider_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddCaching(BuildConfig());

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*IDistributedCache*");
    }

    [Fact(DisplayName = "Memory-only mode should register CacheService")]
    public void MemoryOnlyMode_ShouldRegisterCacheService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCaching(BuildConfig(new Dictionary<string, string?>
        {
            ["Caching:Memory:Enabled"] = "true",
            ["Caching:Distributed:Enabled"] = "false"
        }));

        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();

        cache.Should().BeOfType<CacheService>();
    }

    [Fact(DisplayName = "Distributed-only mode should register CacheService")]
    public void DistributedOnlyMode_ShouldRegisterCacheService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddCaching(BuildConfig(new Dictionary<string, string?>
        {
            ["Caching:Memory:Enabled"] = "false",
            ["Caching:Distributed:Enabled"] = "true"
        }));

        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();

        cache.Should().BeOfType<CacheService>();
    }

    [Fact(DisplayName = "Custom settings should bind correctly")]
    public void CustomSettings_ShouldBindCorrectly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddCaching(BuildConfig(new Dictionary<string, string?>
        {
            ["Caching:Memory:DefaultExpirationMinutes"] = "15",
            ["Caching:Memory:CompactionPercentage"] = "25",
            ["Caching:Memory:SizeLimitBytes"] = "104857600",
            ["Caching:Distributed:DefaultExpirationMinutes"] = "60",
            ["Caching:Hybrid:MaximumPayloadBytes"] = "5242880",
            ["Caching:Hybrid:MaximumKeyLength"] = "512"
        }));

        using var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<IOptions<CachingSetting>>().Value;

        settings.Memory.DefaultExpirationMinutes.Should().Be(15);
        settings.Memory.CompactionPercentage.Should().Be(25);
        settings.Memory.SizeLimitBytes.Should().Be(104857600);
        settings.Distributed.DefaultExpirationMinutes.Should().Be(60);
        settings.Hybrid.MaximumPayloadBytes.Should().Be(5242880);
        settings.Hybrid.MaximumKeyLength.Should().Be(512);
    }

    [Fact(DisplayName = "AddCaching should return the same service collection")]
    public void AddCaching_ShouldReturnSameCollection()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();

        IServiceCollection result = services.AddCaching(BuildConfig());

        result.Should().BeSameAs(services);
    }

    [Fact(DisplayName = "AddCaching with null services should throw")]
    public void AddCaching_WithNullServices_ShouldThrow()
    {
        Action act = () => ((IServiceCollection)null!).AddCaching(BuildConfig());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AddCaching with null configuration should throw")]
    public void AddCaching_WithNullConfiguration_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddCaching((IConfiguration)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Validation should require at least one tier")]
    public void Validation_ShouldRequireAtLeastOneTier()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddCaching(BuildConfig(new Dictionary<string, string?>
        {
            ["Caching:Memory:Enabled"] = "false",
            ["Caching:Distributed:Enabled"] = "false"
        }));

        using var provider = services.BuildServiceProvider();
        var optionsService = provider.GetRequiredService<IOptions<CachingSetting>>();

        // ValidateOnStart fires when accessing .Value
        Action act = () => { var _ = optionsService.Value; };

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Caching.Tier.Required*");
    }

    [Fact(DisplayName = "Validation should reject inverted expiration")]
    public void Validation_ShouldRejectInvertedExpiration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddCaching(BuildConfig(new Dictionary<string, string?>
        {
            ["Caching:Memory:DefaultExpirationMinutes"] = "60",
            ["Caching:Distributed:DefaultExpirationMinutes"] = "5"
        }));

        using var provider = services.BuildServiceProvider();
        var optionsService = provider.GetRequiredService<IOptions<CachingSetting>>();

        // ValidateOnStart fires when accessing .Value
        Action act = () => { var _ = optionsService.Value; };

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Caching.Expiration.Inverted*");
    }

    static IConfiguration BuildConfig(Dictionary<string, string?>? overrides = null)
    {
        var data = new Dictionary<string, string?>
        {
            ["Caching:Enabled"] = "true",
            ["Caching:Memory:Enabled"] = "true",
            ["Caching:Memory:DefaultExpirationMinutes"] = "5",
            ["Caching:Distributed:Enabled"] = "true",
            ["Caching:Distributed:DefaultExpirationMinutes"] = "30"
        };

        if (overrides is not null)
        {
            foreach ((string key, string? value) in overrides)
            {
                data[key] = value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }
}
