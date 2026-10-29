using Application.Common.Caching;

using Infrastructure.Caching;
using Infrastructure.Caching.Options;
using Infrastructure.Caching.Services;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Unit.Tests.Caching;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CachingCompositionTest
{
    [Fact(DisplayName = "AddCaching when enabled should register the cache stack")]
    public void AddCaching_WhenEnabled_ShouldRegisterTheCacheStack()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        IServiceCollection result = services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Distributed:Enabled"] = "false"
        }));

        result.Should().BeSameAs(services);

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheService>().Should().BeOfType<CacheService>();
        provider.GetService<HybridCache>().Should().NotBeNull();
        provider.GetService<IMemoryCache>().Should().NotBeNull();
    }

    [Fact(DisplayName = "AddCaching should bind the caching section")]
    public void AddCaching_ShouldBindTheCachingSection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Distributed:Enabled"] = "false",
            ["Caching:Memory:DefaultExpirationMinutes"] = "5",
            ["Caching:Hybrid:MaximumKeyLength"] = "256"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();
        CachingSetting setting = provider.GetRequiredService<CachingSetting>();

        setting.Memory.DefaultExpirationMinutes.Should().Be(5);
        setting.Hybrid.MaximumKeyLength.Should().Be(256);
    }

    [Fact(DisplayName = "AddCaching when disabled should register a no-op facade")]
    public async Task AddCaching_WhenDisabled_ShouldRegisterANoOpFacade()
    {
        var services = new ServiceCollection();
        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Enabled"] = "false"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();
        ICacheService cache = provider.GetRequiredService<ICacheService>();

        int calls = 0;
        Result<string> first = await cache.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return ValueTask.FromResult("factory");
        });
        Result<string> second = await cache.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return ValueTask.FromResult("factory");
        });

        first.Value.Should().Be("factory");
        second.Value.Should().Be("factory");
        calls.Should().Be(2);
    }


    [Fact(DisplayName = "AddCaching when distributed is enabled without a provider should throw")]
    public void AddCaching_WhenDistributedEnabledWithoutProvider_ShouldThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        Action act = () => services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Distributed:Provider"] = "unknown"
        }));

        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "AddCaching when memory is disabled should still register HybridCache")]
    public void AddCaching_WhenMemoryDisabled_ShouldStillRegisterHybridCache()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Memory:Enabled"] = "false"
        }));

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<HybridCache>().Should().NotBeNull();
        provider.GetRequiredService<ICacheService>().Should().NotBeNull();
    }

    [Fact(DisplayName = "AddCaching should register ICacheService as a singleton")]
    public void AddCaching_ShouldRegisterICacheServiceAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        services.AddCaching(Configuration(new Dictionary<string, string?>
        {
            ["Caching:Distributed:Enabled"] = "false"
        }));

        ServiceDescriptor? descriptor =
            services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(ICacheService));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
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