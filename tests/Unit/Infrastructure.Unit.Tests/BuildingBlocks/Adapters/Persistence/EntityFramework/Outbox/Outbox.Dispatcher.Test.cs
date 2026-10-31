using Application.BuildingBlocks.Messaging.Events;
using Application.BuildingBlocks.Messaging.Bus;
using Application.BuildingBlocks.Messaging.Outbox;
using Application.BuildingBlocks.Messaging.Serialization;

using Core.BuildingBlocks.Domain.Events;

using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox.Options;

using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Tests for the outbox dispatcher's publish and retry policy.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging")]
public sealed class OutboxDispatcherTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Dispatcher should publish a domain row and mark it processed")]
    public async Task DispatchBatch_WithDomainRow_ShouldPublishAndMarkProcessed()
    {
        // Arrange
        var eventTypes = new EventTypeRegistry(typeof(DomainEvent).Assembly);
        var serializer = new JsonMessageSerializer(eventTypes);
        var domainEvent = new TestOutboxEvent("domain");
        var store = new FakeOutboxStore();
        store.Claimed.Add(CreateMessage(domainEvent, serializer, OutboxMessageKind.Domain));
        var publisher = new FakeDomainPublisher();
        OutboxDispatcher dispatcher = CreateDispatcher(store, serializer, publisher: publisher);

        // Act
        Result<int> result = await dispatcher.DispatchBatchAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        publisher.Published.Should().ContainSingle().Which.Should().BeOfType<TestOutboxEvent>();
        store.Processed.Should().ContainSingle();
    }

    [Fact(DisplayName = "Dispatcher should publish an integration row through the bus")]
    public async Task DispatchBatch_WithIntegrationRow_ShouldPublishToBus()
    {
        // Arrange
        var eventTypes = new EventTypeRegistry(typeof(DomainEvent).Assembly);
        eventTypes.Register<TestIntegrationEvent>("test.integration", 1);
        var serializer = new JsonMessageSerializer(eventTypes);
        var integrationEvent = new TestIntegrationEvent("integration");
        var store = new FakeOutboxStore();
        OutboxMessage message = CreateMessage(integrationEvent, serializer, OutboxMessageKind.Integration);
        message = new OutboxMessage
        {
            Id = message.Id,
            Kind = message.Kind,
            Type = "test.integration",
            Version = 1,
            Payload = serializer.Serialize(integrationEvent),
            Metadata = serializer.Serialize(integrationEvent.Metadata),
            LeaseId = message.LeaseId,
            OccurredOnUtc = Now,
            AvailableOnUtc = Now,
        };
        store.Claimed.Add(message);
        var bus = new FakeEventBus();
        OutboxDispatcher dispatcher = CreateDispatcher(store, serializer, bus: bus);

        // Act
        Result<int> result = await dispatcher.DispatchBatchAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        bus.Published.Should().ContainSingle().Which.Should().BeOfType<TestIntegrationEvent>();
        store.Processed.Should().ContainSingle();
    }

    [Fact(DisplayName = "Dispatcher should retry a failed publication with backoff")]
    public async Task DispatchBatch_WhenPublishFails_ShouldReschedule()
    {
        // Arrange
        var serializer = new JsonMessageSerializer(new EventTypeRegistry(typeof(DomainEvent).Assembly));
        var store = new FakeOutboxStore();
        store.Claimed.Add(CreateMessage(new TestOutboxEvent("retry"), serializer, OutboxMessageKind.Domain));
        var publisher = new FakeDomainPublisher
        {
            NextResult = Result.Fail(Error.InternalServerError("handler.failed", "boom")),
        };
        OutboxDispatcher dispatcher = CreateDispatcher(store, serializer, publisher: publisher);

        // Act
        await dispatcher.DispatchBatchAsync();

        // Assert
        store.Failed.Should().ContainSingle();
        store.Failed[0].NextAttemptUtc.Should().BeAfter(Now);
        store.Processed.Should().BeEmpty();
    }

    [Fact(DisplayName = "Dispatcher should dead-letter when the attempt limit is reached")]
    public async Task DispatchBatch_WhenMaxAttemptsReached_ShouldDeadLetter()
    {
        // Arrange
        var serializer = new JsonMessageSerializer(new EventTypeRegistry(typeof(DomainEvent).Assembly));
        var store = new FakeOutboxStore();
        OutboxMessage message = CreateMessage(
            new TestOutboxEvent("dead"),
            serializer,
            OutboxMessageKind.Domain);
        message.Attempts = OutboxSettingConstant.Defaults.MaxAttempts - 1;
        store.Claimed.Add(message);
        var publisher = new FakeDomainPublisher
        {
            NextResult = Result.Fail(Error.InternalServerError("handler.failed", "boom")),
        };
        OutboxDispatcher dispatcher = CreateDispatcher(store, serializer, publisher: publisher);

        // Act
        await dispatcher.DispatchBatchAsync();

        // Assert
        store.DeadLettered.Should().ContainSingle(id => id == message.Id);
        store.Failed.Should().BeEmpty();
    }

    [Fact(DisplayName = "Dispatcher should mark a deserialization failure instead of crashing")]
    public async Task DispatchBatch_WhenPayloadIsInvalid_ShouldMarkFailed()
    {
        // Arrange
        var serializer = new JsonMessageSerializer(new EventTypeRegistry(typeof(DomainEvent).Assembly));
        var store = new FakeOutboxStore();
        store.Claimed.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Kind = OutboxMessageKind.Domain,
            Type = "not-a-real-type",
            Version = 1,
            Payload = "{}",
            LeaseId = Guid.NewGuid(),
            OccurredOnUtc = Now,
            AvailableOnUtc = Now,
        });
        OutboxDispatcher dispatcher = CreateDispatcher(store, serializer);

        // Act
        Result<int> result = await dispatcher.DispatchBatchAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        store.Failed.Should().ContainSingle();
        store.Processed.Should().BeEmpty();
    }

    [Fact(DisplayName = "Dispatcher should return zero when there is no work")]
    public async Task DispatchBatch_WhenEmpty_ShouldReturnZero()
    {
        // Arrange
        var serializer = new JsonMessageSerializer(new EventTypeRegistry(typeof(DomainEvent).Assembly));
        OutboxDispatcher dispatcher = CreateDispatcher(new FakeOutboxStore(), serializer);

        // Act
        Result<int> result = await dispatcher.DispatchBatchAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact(DisplayName = "Dispatcher should claim nothing and fail when the claim fails")]
    public async Task DispatchBatch_WhenClaimFails_ShouldReturnFailure()
    {
        // Arrange
        var serializer = new JsonMessageSerializer(new EventTypeRegistry(typeof(DomainEvent).Assembly));
        var store = new FakeOutboxStore
        {
            ClaimResult = Result<IReadOnlyList<OutboxMessage>>.Fail(
                OutboxOutcome.Failure.ClaimFailed(new InvalidOperationException("claim failed"))),
        };
        OutboxDispatcher dispatcher = CreateDispatcher(store, serializer);

        // Act
        Result<int> result = await dispatcher.DispatchBatchAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        store.Processed.Should().BeEmpty();
    }

    [Fact(DisplayName = "Dispatcher should not publish a row without a lease")]
    public async Task DispatchBatch_WhenLeaseIsMissing_ShouldNotPublish()
    {
        // Arrange
        var serializer = new JsonMessageSerializer(new EventTypeRegistry(typeof(DomainEvent).Assembly));
        var store = new FakeOutboxStore();
        OutboxMessage message = CreateMessage(
            new TestOutboxEvent("no-lease"),
            serializer,
            OutboxMessageKind.Domain);
        message.LeaseId = null;
        store.Claimed.Add(message);
        var publisher = new FakeDomainPublisher();
        OutboxDispatcher dispatcher = CreateDispatcher(store, serializer, publisher: publisher);

        // Act
        Result<int> result = await dispatcher.DispatchBatchAsync();

        // Assert
        result.Value.Should().Be(1);
        publisher.Published.Should().BeEmpty();
    }

    [Fact(DisplayName = "Dispatcher should continue after a stale mark loses the lease")]
    public async Task DispatchBatch_WhenMarkLosesLease_ShouldContinue()
    {
        // Arrange
        var serializer = new JsonMessageSerializer(new EventTypeRegistry(typeof(DomainEvent).Assembly));
        var store = new FakeOutboxStore
        {
            ProcessedResult = Result.Fail(OutboxOutcome.Failure.LeaseLost(Guid.NewGuid())),
        };
        store.Claimed.Add(CreateMessage(new TestOutboxEvent("first"), serializer, OutboxMessageKind.Domain));
        store.Claimed.Add(CreateMessage(new TestOutboxEvent("second"), serializer, OutboxMessageKind.Domain));
        var publisher = new FakeDomainPublisher();
        OutboxDispatcher dispatcher = CreateDispatcher(store, serializer, publisher: publisher);

        // Act
        Result<int> result = await dispatcher.DispatchBatchAsync();

        // Assert
        result.Value.Should().Be(2);
        publisher.Published.Should().HaveCount(2);
    }

    private static OutboxDispatcher CreateDispatcher(
        FakeOutboxStore store,
        JsonMessageSerializer serializer,
        FakeDomainPublisher? publisher = null,
        FakeEventBus? bus = null)
        => new(
            store,
            serializer,
            serializer,
            publisher ?? new FakeDomainPublisher(),
            bus ?? new FakeEventBus(),
            new OutboxSetting(),
            new FixedTimeProvider(Now),
            NullLogger<OutboxDispatcher>.Instance);

    private static OutboxMessage CreateMessage(
        IDomainEvent domainEvent,
        JsonMessageSerializer serializer,
        OutboxMessageKind kind)
        => new()
        {
            Id = domainEvent.GetEventId()!.Value,
            Kind = kind,
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Version = domainEvent.EventVersion,
            Payload = serializer.Serialize(domainEvent),
            Metadata = serializer.Serialize(domainEvent.Metadata),
            LeaseId = Guid.NewGuid(),
            OccurredOnUtc = Now,
            AvailableOnUtc = Now,
        };

    private static OutboxMessage CreateMessage(
        IIntegrationEvent integrationEvent,
        JsonMessageSerializer serializer,
        OutboxMessageKind kind)
        => new()
        {
            Id = integrationEvent.MessageId,
            Kind = kind,
            Type = integrationEvent.EventName,
            Version = integrationEvent.Version,
            Payload = serializer.Serialize(integrationEvent),
            Metadata = serializer.Serialize(integrationEvent.Metadata),
            LeaseId = Guid.NewGuid(),
            OccurredOnUtc = Now,
            AvailableOnUtc = Now,
        };

    private sealed record TestOutboxEvent(string Name) : DomainEvent;

    private sealed record TestIntegrationEvent(string Name) : IIntegrationEvent
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTimeOffset OccurredOnUtc { get; init; } = Now;
        public string EventName => "test.integration";
        public int Version => 1;
        public DefaultMetadata Metadata { get; init; } = [];
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeDomainPublisher : IDomainEventPublisher
    {
        public List<IDomainEvent> Published { get; } = [];
        public Result NextResult { get; set; } = Result.Ok();

        public Task<Result> PublishAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            Published.AddRange(domainEvents);
            return Task.FromResult(NextResult);
        }
    }

    private sealed class FakeEventBus : IEventBus
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task<Result> PublishAsync<T>(
            T integrationEvent,
            CancellationToken cancellationToken = default)
            where T : class, IIntegrationEvent
        {
            Published.Add(integrationEvent);
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class FakeOutboxStore : IOutboxStore
    {
        public List<OutboxMessage> Claimed { get; } = [];
        public Result<IReadOnlyList<OutboxMessage>>? ClaimResult { get; set; }
        public Result? ProcessedResult { get; set; }
        public List<Guid> Processed { get; } = [];
        public List<(Guid Id, string Error, DateTimeOffset NextAttemptUtc)> Failed { get; } = [];
        public List<Guid> DeadLettered { get; } = [];

        public Task<Result> EnqueueAsync(
            IReadOnlyCollection<OutboxMessage> messages,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result<IReadOnlyList<OutboxMessage>>> ClaimAsync(
            int batchSize,
            DateTimeOffset nowUtc,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ClaimResult ?? Result<IReadOnlyList<OutboxMessage>>.Success(Claimed));

        public Task<Result> MarkProcessedAsync(
            Guid id,
            Guid leaseId,
            DateTimeOffset processedOnUtc,
            CancellationToken cancellationToken = default)
        {
            Processed.Add(id);
            return Task.FromResult(ProcessedResult ?? Result.Ok());
        }

        public Task<Result> MarkFailedAsync(
            Guid id,
            Guid leaseId,
            string error,
            DateTimeOffset nextAttemptUtc,
            CancellationToken cancellationToken = default)
        {
            Failed.Add((id, error, nextAttemptUtc));
            return Task.FromResult(Result.Ok());
        }

        public Task<Result> DeadLetterAsync(
            Guid id,
            Guid leaseId,
            string error,
            CancellationToken cancellationToken = default)
        {
            DeadLettered.Add(id);
            return Task.FromResult(Result.Ok());
        }

        public Task<Result<int>> CountPendingAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<int>.Success(0));

        public Task<Result<DateTimeOffset?>> OldestPendingOnUtcAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<DateTimeOffset?>.Success(null));

        public Task<Result<int>> DeleteProcessedBeforeAsync(
            DateTimeOffset cutoffUtc,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<int>.Success(0));
    }
}
