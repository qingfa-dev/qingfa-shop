using Application.Common.Caching;

using Infrastructure.Caching;
using Infrastructure.Caching.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration.Tests.Caching;

/// <summary>
/// Boots a real DI container with the full caching stack (HybridCache + MemoryCache +
/// DistributedMemoryCache) so integration tests exercise the same pipeline as production.
/// </summary>
public sealed class CachingFixture : IAsyncLifetime
{
    /// <summary>The service provider with the full cache stack registered.</summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>Cache service resolved from the container.</summary>
    public ICacheService Cache { get; private set; } = null!;

    /// <summary>Bound caching settings resolved from the container.</summary>
    public CachingSetting Settings { get; private set; } = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IServiceCollection services = builder.Services;
        services.AddLogging();
        services.AddDistributedMemoryCache();
        builder.AddCaching(BuildConfiguration());

        Services = services.BuildServiceProvider();

        // Validate: resolve the cache service to confirm the container is healthy.
        Cache = Services.GetRequiredService<ICacheService>();
        Settings = Services.GetRequiredService<IOptions<CachingSetting>>().Value;

        // The fixture is synchronous; await to satisfy IAsyncLifetime.
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
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
            ["Caching:Distributed:Provider"] = "memory",
            ["Caching:Distributed:ConnectionStringName"] = "Cache",
            ["Caching:Distributed:DefaultExpirationMinutes"] = "5",
            ["ConnectionStrings:Cache"] = "memory-cache"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }
}
