using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace Testing.Fixtures;

/// <summary>
/// Boots the real <c>Api</c> pipeline in-process with the clock faked.
/// </summary>
/// <remarks>
/// Substitutes <see cref="TimeProvider"/> via
/// <see cref="RemoveAll{T}(IServiceCollection)"/> before registering
/// <see cref="Clock"/>. A plain <c>AddSingleton</c> would also win because MS DI
/// resolves the last registration, but removing first keeps exactly one
/// registration so the intent is explicit rather than order-dependent.
/// </remarks>
public class ShopApiFactory : WebApplicationFactory<Web.Api.AssemblyMarker>
{
    /// <summary>The fake clock handed to the host; tests advance it then assert on responses.</summary>
    public FakeTimeProvider Clock { get; } = new();

    /// <summary>
    /// Connection string published as <c>ConnectionStrings:db</c>. It defaults to a
    /// closed port: the fast suites never resolve the context, multiplexer, or health
    /// checks, so they stay container-free. Container tests override both properties.
    /// </summary>
    public string DatabaseConnectionString { get; init; } =
        "Host=127.0.0.1;Port=1;Database=qingfa;Username=qingfa;Password=qingfa;Timeout=1";

    /// <summary>Endpoint published as <c>ConnectionStrings:redis</c>; defaults to a closed port.</summary>
    public string RedisConnectionString { get; init; } = "127.0.0.1:1";

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Initialize: Program reads the connection strings during composition, before
        // ConfigureAppConfiguration callbacks run. Environment variables are visible to
        // WebApplication.CreateBuilder, so the test host sees them in time.
        Environment.SetEnvironmentVariable("ConnectionStrings__db", DatabaseConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__redis", RedisConnectionString);
        Environment.SetEnvironmentVariable("Outbox__Enabled", "false");
        Environment.SetEnvironmentVariable("Messaging__Enabled", "false");

        return base.CreateHost(builder);
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}