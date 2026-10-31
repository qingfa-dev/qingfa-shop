using Application.Modules.Payments;
using Application.Modules.Payments.Persistence;

using Infrastructure.Payments.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    /// <returns>The number of rows deleted, or -1 when the sweep failed.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return 0;

        DateTimeOffset cutoff = _clock.GetUtcNow().AddDays(-_options.WebhookEventRetentionDays);

        using IServiceScope scope = _scopeFactory.CreateScope();
        IWebhookEventStore store = scope.ServiceProvider.GetRequiredService<IWebhookEventStore>();

        Result<int> deleted = await store.DeleteProcessedBeforeAsync(cutoff, cancellationToken)
            .ConfigureAwait(false);

        if (deleted.IsFailure)
        {
            PaymentRetentionLoggers.SweepFailed(_logger, deleted.Errors[0].Message);
            return -1;
        }

        if (deleted.Value > 0)
        {
            PaymentMetrics.WebhooksPruned.Add(deleted.Value);
            PaymentRetentionLoggers.Pruned(_logger, deleted.Value, cutoff);
        }

        return deleted.Value;
    }
}

/// <summary>Source-generated log messages for the retention worker.</summary>
internal static partial class PaymentRetentionLoggers
{
    /// <summary>Logs a completed sweep that removed rows.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">Rows removed.</param>
    /// <param name="cutoff">The cutoff used.</param>
    [LoggerMessage(
        EventId = 5101,
        Level = LogLevel.Information,
        Message = "Pruned {Count} processed webhook deliveries received before {Cutoff}.")]
    public static partial void Pruned(ILogger logger, int count, DateTimeOffset cutoff);

    /// <summary>Logs a failed sweep.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">Why the sweep failed.</param>
    [LoggerMessage(
        EventId = 5102,
        Level = LogLevel.Warning,
        Message = "Webhook retention sweep failed: {Reason}")]
    public static partial void SweepFailed(ILogger logger, string reason);
}
