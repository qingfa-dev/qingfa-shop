using System.Reflection;

using Aspire.Hosting;

using Testing;

namespace AppHost.Tests;

/// <summary>
/// Starts the AppHost once and shares it with every test in the collection.
/// </summary>
/// <remarks>
/// Each test previously built and started its own <c>DistributedApplication</c>, so a full
/// orchestration (DCP, api, gateway and two Vite dev servers) was paid per test, and xunit's
/// default collection parallelism could run three of them at once. Starting is deferred until
/// the first <see cref="GetAppAsync"/> call, so a suite that skips on a missing toolchain
/// never pays the cost at all.
/// </remarks>
public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(180);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private DistributedApplication? _app;

    /// <summary>Starts the shared AppHost on first use and returns it for every later call.</summary>
    public async Task<DistributedApplication> GetAppAsync()
    {
        if (_app is not null)
        {
            return _app;
        }

        await _gate.WaitAsync();
        try
        {
            // Validate: a second caller may have started the app while this one waited
            // on the gate, so re-check rather than starting a duplicate orchestration.
            if (_app is not null)
            {
                return _app;
            }

            ContainerRuntime.ConfigureProcessEnvironment();

            var appHostAssembly = Assembly.Load("AppHost");
            var appHostType = appHostAssembly.GetType("Projects.AppHost") ?? appHostAssembly.GetType("AppHost.Program");
            var builder = await DistributedApplicationTestingBuilder.CreateAsync(appHostType!);
            var app = builder.Build();

            try
            {
                await app.StartAsync().WaitAsync(StartupTimeout);
            }
            catch
            {
                // Cleanup: a half-started orchestration still owns containers and child
                // processes, so release them before surfacing the failure.
                await app.DisposeAsync();
                throw;
            }

            _app = app;
            return _app;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        _gate.Dispose();
    }
}