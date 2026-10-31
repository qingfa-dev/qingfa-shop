using Application.Modules.Payments;
using Application.Modules.Payments.Persistence;

using Core.BuildingBlocks.Functional.Results;

using Infrastructure.Payments.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Infrastructure.Modules.Payments.Retention;
using Microsoft.Extensions.Options;

namespace Infrastructure.Modules.Payments.Retention;

/// <summary>Periodically prunes processed webhook deliveries past their retention window.</summary>
/// <remarks>
/// Webhook rows hold raw provider payloads, so keeping them indefinitely is a privacy and storage
/// cost. Unprocessed deliveries are never pruned: a delivery that failed may still be replayed.
/// </remarks>
public sealed class PaymentWebhookRetentionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PaymentRetentionOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<PaymentWebhookRetentionWorker> _logger;

    /// <summary>Creates the retention worker.</summary>
    /// <param name="scopeFactory">Scope factory for the scoped store.</param>
    /// <param name="options">The retention configuration.</param>
    /// <param name="clock">The clock used to compute the cutoff.</param>
    /// <param name="logger">The logger.</param>
    public PaymentWebhookRetentionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<PaymentRetentionOptions> options,
        TimeProvider clock,
        ILogger<PaymentWebhookRetentionWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.SweepIntervalHours));

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
                PaymentRetentionLoggers.SweepFailed(_logger, exception.Message);
            }
        }
    }

    /// <summary>Runs one retention sweep.</summary>
    /// <param name="cancellationToken">A token that cancels the sweep.</param>
    /// <returns>The number of rows deleted, or the retention failure.</returns>
    public async Task<Result<int>> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return Result<int>.Success(0);

        DateTimeOffset cutoff = _clock.GetUtcNow().AddDays(-_options.WebhookEventRetentionDays);

        using IServiceScope scope = _scopeFactory.CreateScope();

        IWebhookEventStore store = scope.ServiceProvider.GetRequiredService<IWebhookEventStore>();

        Result<int> sweep = await store
            .DeleteProcessedBeforeAsync(cutoff, cancellationToken)
            .ConfigureAwait(false);

        // Handle: report through the monadic switch so success and failure share one path.
        return sweep.Switch(
            count => OnPruned(count, cutoff),
            errors => PaymentRetentionLoggers.SweepFailed(_logger, errors[0].Message));
    }

    /// <summary>Records a successful sweep.</summary>
    private void OnPruned(int count, DateTimeOffset cutoff)
    {
        if (count <= 0)
            return;

        PaymentMetrics.WebhooksPruned.Add(count);
        PaymentRetentionLoggers.Pruned(_logger, count, cutoff);
    }
}
