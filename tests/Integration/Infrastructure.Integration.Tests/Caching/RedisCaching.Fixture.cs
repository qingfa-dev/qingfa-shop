using Application.Common.Caching;

using Infrastructure.Caching;
using Infrastructure.Caching.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Testing;

namespace Infrastructure.Integration.Tests.Caching;

/// <summary>
/// Boots a real DI container with the full caching stack backed by a Redis container
/// (HybridCache + MemoryCache + StackExchange.Redis) so integration tests exercise the
/// production-like pipeline.
/// </summary>
public sealed class RedisCachingFixture : IAsyncLifetime
{
    private const string Image = "redis:7-alpine";

    /// <summary>The service provider with the full cache stack registered.</summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>Cache service resolved from the container.</summary>
    public ICacheService Cache { get; private set; } = null!;

    /// <summary>Bound caching settings resolved from the container.</summary>
    public CachingSetting Settings { get; private set; } = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        // Skip if no container runtime is available
        if (!ContainerRuntime.IsAvailable)
        {
            return;
        }

        ContainerRuntime.ConfigureProcessEnvironment();

        // Start Redis container
        var redisContainer = new Testcontainers.Redis.RedisBuilder(Image).Build();
        await redisContainer.StartAsync();
        string redisConnectionString = redisContainer.GetConnectionString();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });
        services.AddCaching(BuildConfiguration());

        Services = services.BuildServiceProvider();

        // Validate: resolve the cache service to confirm the container is healthy.
        Cache = Services.GetRequiredService<ICacheService>();
        Settings = Services.GetRequiredService<IOptions<CachingSetting>>().Value;

        // Store the container for disposal
        _redisContainer = redisContainer;

        await Task.CompletedTask;
    }

    private Testcontainers.Redis.RedisContainer? _redisContainer;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_redisContainer is not null)
        {
            await _redisContainer.DisposeAsync();
        }

        if (Services is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await Task.CompletedTask;
    }

    /// <summary>True when the fixture initialised (container runtime was available).</summary>
    public bool IsInitialized => Services is not null;

    /// <summary>
    /// Creates an in-memory configuration with both cache tiers enabled and short
    /// expirations so tests complete quickly.
    /// </summary>
    static IConfiguration BuildConfiguration()
    {
        var data = new Dictionary<string, string?>
        {
            ["Caching:Enabled"] = "true",
            ["Caching:Memory:Enabled"] = "true",
            ["Caching:Memory:DefaultExpirationMinutes"] = "1",
            ["Caching:Distributed:Enabled"] = "true",
            ["Caching:Distributed:DefaultExpirationMinutes"] = "5"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }
}
