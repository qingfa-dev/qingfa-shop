using Application.Common.Messaging.Outbox;

using Infrastructure.Persistence.EntityFramework.Outbox.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Persistence.EntityFramework.Outbox;

/// <summary>Polls the outbox while the host is running.</summary>
public sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxSetting _setting;
    private readonly ILogger<OutboxWorker> _logger;

    /// <summary>Creates the outbox worker.</summary>
    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        OutboxSetting setting,
        ILogger<OutboxWorker> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _setting = setting ?? throw new ArgumentNullException(nameof(setting));
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
                using IServiceScope scope = _scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
                Result<int> result = await dispatcher
                    .DispatchBatchAsync(stoppingToken)
                    .ConfigureAwait(false);

                if (result.IsFailure)
                {
                    OutboxWorkerLoggers.DispatchFailed(
                        _logger,
                        result.Errors[0].Code,
                        result.Errors[0].Message);
                }
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
}
