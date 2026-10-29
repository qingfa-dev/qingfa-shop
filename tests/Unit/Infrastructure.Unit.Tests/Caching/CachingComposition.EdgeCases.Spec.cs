using Application.Common.Caching;

using Infrastructure.Caching;
using Infrastructure.Caching.Options;
using Infrastructure.Caching.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Unit.Tests.Caching;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingCompositionEdgeCasesTest
{
    [Fact(DisplayName = "AddCaching with distributed only should register cache stack")]
    public void AddCaching_WithDistributedOnly_ShouldRegisterCacheStack()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddDistributedMemoryCache();

        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Memory:Enabled"] = "false",
            ["Caching:Distributed:Enabled"] = "true"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheService>().Should().BeOfType<CacheService>();
    }

    [Fact(DisplayName = "AddCaching with memory only should register cache stack")]
    public void AddCaching_WithMemoryOnly_ShouldRegisterCacheStack()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Memory:Enabled"] = "true",
            ["Caching:Distributed:Enabled"] = "false"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheService>().Should().BeOfType<CacheService>();
    }

    [Fact(DisplayName = "AddCaching when disabled should not register HybridCache")]
    public void AddCaching_WhenDisabled_ShouldNotRegisterHybridCache()
    {
        var services = new ServiceCollection();
        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Enabled"] = "false"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<ICacheService>().Should().BeOfType<NullCacheService>();
    }

    [Fact(DisplayName = "AddCaching should return the same service collection")]
    public void AddCaching_ShouldReturnSameCollection()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();

        IServiceCollection result = services.AddCaching(Configuration());

        result.Should().BeSameAs(services);
    }

    [Fact(DisplayName = "AddCaching with null services should throw")]
    public void AddCaching_WithNullServices_ShouldThrow()
    {
        Action act = () => ((IServiceCollection)null!).AddCaching(Configuration());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AddCaching with null configuration should throw")]
    public void AddCaching_WithNullConfiguration_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddCaching((IConfiguration)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AddCaching when disabled should allow GetOrCreateAsync")]
    public async Task AddCaching_WhenDisabled_ShouldAllowGetOrCreateAsync()
    {
        var services = new ServiceCollection();
        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Enabled"] = "false"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();
        ICacheService cache = provider.GetRequiredService<ICacheService>();

        Result<string> value = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("factory-value"));

        value.Value.Should().Be("factory-value");
    }

    [Fact(DisplayName = "AddCaching when disabled should allow SetAsync")]
    public async Task AddCaching_WhenDisabled_ShouldAllowSetAsync()
    {
        var services = new ServiceCollection();
        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Enabled"] = "false"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();
        ICacheService cache = provider.GetRequiredService<ICacheService>();

        Result result = await cache.SetAsync("key", "value");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "AddCaching when disabled should allow RemoveAsync")]
    public async Task AddCaching_WhenDisabled_ShouldAllowRemoveAsync()
    {
        var services = new ServiceCollection();
        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Enabled"] = "false"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();
        ICacheService cache = provider.GetRequiredService<ICacheService>();

        Result result = await cache.RemoveAsync("key");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "AddCaching when disabled should allow RemoveByTagAsync")]
    public async Task AddCaching_WhenDisabled_ShouldAllowRemoveByTagAsync()
    {
        var services = new ServiceCollection();
        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Enabled"] = "false"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();
        ICacheService cache = provider.GetRequiredService<ICacheService>();

        Result result = await cache.RemoveByTagAsync("tag");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "AddCaching should apply custom memory settings")]
    public void AddCaching_ShouldApplyCustomMemorySettings()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddDistributedMemoryCache();

        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Distributed:Enabled"] = "false",
            ["Caching:Memory:DefaultExpirationMinutes"] = "15",
            ["Caching:Memory:CompactionPercentage"] = "25",
            ["Caching:Memory:SizeLimitBytes"] = "104857600"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();
        CachingSetting setting = provider.GetRequiredService<CachingSetting>();

        setting.Memory.DefaultExpirationMinutes.Should().Be(15);
        setting.Memory.CompactionPercentage.Should().Be(25);
        setting.Memory.SizeLimitBytes.Should().Be(104857600);
    }

    [Fact(DisplayName = "AddCaching should apply custom hybrid settings")]
    public void AddCaching_ShouldApplyCustomHybridSettings()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddDistributedMemoryCache();

        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Distributed:Enabled"] = "false",
            ["Caching:Hybrid:MaximumPayloadBytes"] = "5242880",
            ["Caching:Hybrid:MaximumKeyLength"] = "512"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();
        CachingSetting setting = provider.GetRequiredService<CachingSetting>();

        setting.Hybrid.MaximumPayloadBytes.Should().Be(5242880);
        setting.Hybrid.MaximumKeyLength.Should().Be(512);
    }

    [Fact(DisplayName = "AddCaching should apply custom distributed settings")]
    public void AddCaching_ShouldApplyCustomDistributedSettings()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddDistributedMemoryCache();

        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Distributed:Enabled"] = "false",
            ["Caching:Distributed:DefaultExpirationMinutes"] = "60"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();
        CachingSetting setting = provider.GetRequiredService<CachingSetting>();

        setting.Distributed.DefaultExpirationMinutes.Should().Be(60);
    }

    private static IConfiguration Configuration(Dictionary<string, string?>? overrides = null)
    {
        Dictionary<string, string?> data = new()
        {
            ["Caching:Enabled"] = "true",
            ["Caching:Memory:Enabled"] = "true",
            ["Caching:Memory:DefaultExpirationMinutes"] = "5",
            ["Caching:Distributed:Enabled"] = "true",
            ["Caching:Distributed:Provider"] = "redis",
            ["Caching:Distributed:ConnectionStringName"] = "Cache",
            ["ConnectionStrings:Cache"] = "localhost:6379",
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
