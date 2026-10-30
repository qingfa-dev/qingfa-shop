using Application.Common.Messaging.Outbox;

using Infrastructure.Persistence.EntityFramework.Outbox.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Persistence.EntityFramework.Outbox;

/// <summary>Polls the outbox and periodically prunes processed rows.</summary>
public sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxSetting _setting;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxWorker> _logger;
    private DateTimeOffset _nextRetentionUtc = DateTimeOffset.MinValue;

    /// <summary>Creates the outbox worker.</summary>
    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        OutboxSetting setting,
        TimeProvider timeProvider,
        ILogger<OutboxWorker> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _setting = setting ?? throw new ArgumentNullException(nameof(setting));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_setting.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_setting.PollingIntervalSeconds));

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
                OutboxWorkerLoggers.Unhandled(_logger, exception.Message);
            }
        }
    }

    /// <summary>Runs one dispatch tick and, when due, the retention sweep.</summary>
    /// <param name="cancellationToken">A token that cancels the tick.</param>
    /// <returns>A task that completes when the tick finishes.</returns>
    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!_setting.Enabled)
            return;

        using IServiceScope scope = _scopeFactory.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
        Result<int> dispatched = await dispatcher
            .DispatchBatchAsync(cancellationToken)
            .ConfigureAwait(false);

        if (dispatched.IsFailure)
        {
            OutboxWorkerLoggers.DispatchFailed(
                _logger,
                dispatched.Errors[0].Code,
                dispatched.Errors[0].Message);
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        if (_setting.RetentionDays > 0 && nowUtc >= _nextRetentionUtc)
        {
            var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            Result<int> pruned = await store
                .DeleteProcessedBeforeAsync(nowUtc.AddDays(-_setting.RetentionDays), cancellationToken)
                .ConfigureAwait(false);

            if (pruned.IsFailure)
            {
                OutboxWorkerLoggers.RetentionFailed(
                    _logger,
                    pruned.Errors[0].Code,
                    pruned.Errors[0].Message);
            }

            _nextRetentionUtc = nowUtc.AddHours(1);
        }
    }
}

/// <summary>Structured log events for the outbox worker.</summary>
internal static partial class OutboxWorkerLoggers
{
    [LoggerMessage(
        EventId = 234,
        Level = LogLevel.Warning,
        Message = "Outbox dispatch failed: {ErrorCode} - {ErrorMessage}.")]
    public static partial void DispatchFailed(ILogger logger, string errorCode, string errorMessage);

    [LoggerMessage(
        EventId = 235,
        Level = LogLevel.Error,
        Message = "Outbox worker iteration failed: {Reason}.")]
    public static partial void Unhandled(ILogger logger, string reason);

    [LoggerMessage(
        EventId = 236,
        Level = LogLevel.Warning,
        Message = "Outbox retention failed: {ErrorCode} - {ErrorMessage}.")]
    public static partial void RetentionFailed(ILogger logger, string errorCode, string errorMessage);
}
