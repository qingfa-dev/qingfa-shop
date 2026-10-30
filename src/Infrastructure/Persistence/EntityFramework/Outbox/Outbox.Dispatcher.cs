using Core.Common.Domain.Events;

using Application.Common.Events;
using Application.Common.Messaging.Bus;
using Application.Common.Messaging.Outbox;
using Application.Common.Messaging.Serialization;

using Infrastructure.Persistence.EntityFramework.Outbox.Options;

namespace Infrastructure.Persistence.EntityFramework.Outbox;

/// <summary>Publishes one claimed batch of outbox rows.</summary>
public sealed class OutboxDispatcher : IOutboxDispatcher
{
    private readonly IOutboxStore _store;
    private readonly IDomainEventSerializer _domainSerializer;
    private readonly IIntegrationEventSerializer _integrationSerializer;
    private readonly IDomainEventPublisher _domainPublisher;
    private readonly IEventBus _eventBus;
    private readonly OutboxSetting _setting;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxDispatcher> _logger;

    /// <summary>Creates a dispatcher.</summary>
    public OutboxDispatcher(
        IOutboxStore store,
        IDomainEventSerializer domainSerializer,
        IIntegrationEventSerializer integrationSerializer,
        IDomainEventPublisher domainPublisher,
        IEventBus eventBus,
        OutboxSetting setting,
        TimeProvider timeProvider,
        ILogger<OutboxDispatcher> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _domainSerializer = domainSerializer ?? throw new ArgumentNullException(nameof(domainSerializer));
        _integrationSerializer = integrationSerializer ?? throw new ArgumentNullException(nameof(integrationSerializer));
        _domainPublisher = domainPublisher ?? throw new ArgumentNullException(nameof(domainPublisher));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _setting = setting ?? throw new ArgumentNullException(nameof(setting));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<int>> DispatchBatchAsync(CancellationToken cancellationToken = default)
    {
        if (!_setting.Enabled)
            return Result<int>.Success(0);

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        Result<IReadOnlyList<OutboxMessage>> claim = await _store
            .ClaimAsync(
                _setting.BatchSize,
                nowUtc,
                TimeSpan.FromSeconds(_setting.LeaseSeconds),
                cancellationToken)
            .ConfigureAwait(false);

        if (claim.IsFailure)
            return Result<int>.Fail(claim.Errors);

        int handled = 0;

        foreach (OutboxMessage message in claim.Value)
        {
            // Guard: a row without a lease is not ours to publish; another worker owns it.
            if (message.LeaseId is null)
            {
                OutboxDispatcherLoggers.MarkFailed(_logger, message.Id, OutboxOutcome.Codes.LeaseLost);
                handled++;
                continue;
            }

            Result publish = await PublishAsync(message, cancellationToken).ConfigureAwait(false);
            Result mark = publish.IsSuccess
                ? await MarkProcessedAsync(message, cancellationToken).ConfigureAwait(false)
                : await MarkFailedAsync(message, publish, cancellationToken).ConfigureAwait(false);

            if (mark.IsFailure)
                OutboxDispatcherLoggers.MarkFailed(_logger, message.Id, mark.Errors[0].Code);

            handled++;
        }

        return Result<int>.Success(handled);
    }

    /// <summary>Publishes one message through its owning pipeline.</summary>
    private async Task<Result> PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            if (message.Kind == OutboxMessageKind.Domain)
            {
                IDomainEvent domainEvent = _domainSerializer.Deserialize(
                    message.Type,
                    message.Version,
                    message.Payload);

                return await _domainPublisher
                    .PublishAsync([domainEvent], cancellationToken)
                    .ConfigureAwait(false);
            }

            IIntegrationEvent integrationEvent = _integrationSerializer.Deserialize(
                message.Type,
                message.Version,
                message.Payload);

            return await _eventBus
                .PublishAsync(integrationEvent, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            OutboxDispatcherLoggers.PublishFailed(_logger, message.Id, message.Kind, message.Type, message.Attempts, exception.Message);
            return Result.Fail(OutboxOutcome.Failure.PublishFailed(message, exception.Message));
        }
    }

    /// <summary>Marks a successfully published row.</summary>
    private Task<Result> MarkProcessedAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
        => message.LeaseId is Guid leaseId
            ? _store.MarkProcessedAsync(message.Id, leaseId, _timeProvider.GetUtcNow(), cancellationToken)
            : Task.FromResult(Result.Fail(OutboxOutcome.Failure.LeaseLost(message.Id)));

    /// <summary>Reschedules or dead-letters a failed row.</summary>
    private Task<Result> MarkFailedAsync(
        OutboxMessage message,
        Result publish,
        CancellationToken cancellationToken)
    {
        string error = publish.Errors.Count > 0
            ? string.Join("; ", publish.Errors.Select(failure => $"{failure.Code}: {failure.Message}"))
            : "Outbox publication failed.";

        if (message.LeaseId is not Guid leaseId)
            return Task.FromResult(Result.Fail(OutboxOutcome.Failure.LeaseLost(message.Id)));

        int attempts = message.Attempts + 1;

        if (attempts >= _setting.MaxAttempts)
        {
            OutboxDispatcherLoggers.DeadLettered(_logger, message.Id, message.Kind, message.Type, attempts);
            return _store.DeadLetterAsync(message.Id, leaseId, error, cancellationToken);
        }

        TimeSpan delay = TimeSpan.FromSeconds(
            Math.Min(
                _setting.MaxBackoffSeconds,
                _setting.BackoffSeconds * Math.Pow(2, attempts - 1)));

        OutboxDispatcherLoggers.Retrying(_logger, message.Id, message.Kind, message.Type, attempts, delay.TotalSeconds);
        return _store.MarkFailedAsync(
            message.Id,
            leaseId,
            error,
            _timeProvider.GetUtcNow().Add(delay),
            cancellationToken);
    }
}
