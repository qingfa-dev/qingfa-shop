using System.Diagnostics.CodeAnalysis;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Projections;

/// <summary>Polls the event log and feeds registered projections.</summary>
[ExcludeFromCodeCoverage]
public sealed class ProjectionWorker(
    IServiceScopeFactory scopeFactory,
    EventStoreSetting setting,
    ILogger<ProjectionWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!setting.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(setting.PollingIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Projection worker iteration failed.");
            }
        }
    }

    /// <summary>Runs one projection pass in a fresh scope.</summary>
    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<ProjectionRunner>();
        Result result = await runner.RunOnceAsync(cancellationToken);
        if (result.IsFailure)
            logger.LogWarning("Projection pass failed: {Code}", result.Errors[0].Code);
    }
}
