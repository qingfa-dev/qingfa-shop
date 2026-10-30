using Application.Common.EventSourcing;
using Application.Common.Messaging.Outbox;
using Application.Common.Messaging.Serialization;
using Application.Common.Persistence.UnitOfWorks;

using Core.Common.Domain.EventSourcing;
using Core.Common.Monad.Maybes;
using Core.Common.Domain.Events;

using Infrastructure.Integrations.Messaging.Serialization;
using Infrastructure.Persistence.EntityFramework.EventSourcing;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.EventSourcing;

/// <summary>Tests for the event-sourced repository save flow.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "EventSourcing")]
public sealed class EventSourcedRepositoryTest
{
    [Fact(DisplayName = "Save should append the expected version, stage outbox, and clear events")]
    public async Task Save_ShouldAppendStageAndClear()
    {
        // Arrange
        var store = new FakeEventStore();
        var outbox = new FakeOutboxStore();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var serializer = new JsonMessageSerializer(
            new EventTypeRegistry(typeof(DomainEvent).Assembly));
        var snapshots = new Mock<ISnapshotStore>();
        snapshots.Setup(store => store.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<Snapshot>>.Success(Maybe<Snapshot>.Empty()));
        var policy = new Mock<ISnapshotPolicy>();
        policy.Setup(policy => policy.ShouldSnapshot(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(false);
        var repository = new EventSourcedRepository<RepoAggregate, Guid>(
            store,
            snapshots.Object,
            policy.Object,
            new RepoAggregateFactory(),
            serializer,
            outbox,
            unitOfWork.Object,
            TimeProvider.System,
            NullLogger<EventSourcedRepository<RepoAggregate, Guid>>.Instance);
        RepoAggregate aggregate = RepoAggregate.Create(Guid.NewGuid(), "saved");

        // Act
        Result result = await repository.SaveAsync(aggregate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        store.LastExpectedVersion.Should().Be(0);
        store.Appended.Should().ContainSingle();
        outbox.Staged.Should().ContainSingle();
        aggregate.UncommittedEvents.Should().BeEmpty();
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Save should surface an append conflict without staging outbox rows")]
    public async Task Save_WhenAppendConflicts_ShouldReturnFailure()
    {
        // Arrange
        var store = new FakeEventStore
        {
            AppendResult = Result<long>.Fail(
                EventSourcingOutcome.Failure.AppendConflict("stream", 0)),
        };
        var outbox = new FakeOutboxStore();
        var serializer = new JsonMessageSerializer(
            new EventTypeRegistry(typeof(DomainEvent).Assembly));
        var snapshots = new Mock<ISnapshotStore>();
        var policy = new Mock<ISnapshotPolicy>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new EventSourcedRepository<RepoAggregate, Guid>(
            store,
            snapshots.Object,
            policy.Object,
            new RepoAggregateFactory(),
            serializer,
            outbox,
            unitOfWork.Object,
            TimeProvider.System,
            NullLogger<EventSourcedRepository<RepoAggregate, Guid>>.Instance);
        RepoAggregate aggregate = RepoAggregate.Create(Guid.NewGuid(), "conflict");

        // Act
        Result result = await repository.SaveAsync(aggregate);

        // Assert
        result.IsFailure.Should().BeTrue();
        outbox.Staged.Should().BeEmpty();
        aggregate.UncommittedEvents.Should().ContainSingle();
    }

    private sealed record RepoEvent(Guid Id, string Value) : DomainEvent;

    private sealed class RepoAggregate : EventSourcedAggregate<Guid>
    {
        public string Value { get; private set; } = string.Empty;

        public static RepoAggregate Create(Guid id, string value)
        {
            var aggregate = new RepoAggregate();
            aggregate.Raise(new RepoEvent(id, value));
            return aggregate;
        }

        protected override void When(IDomainEvent domainEvent)
        {
            if (domainEvent is RepoEvent created)
            {
                Id = created.Id;
                Value = created.Value;
                return;
            }

            throw new InvalidOperationException($"Unsupported event {domainEvent.GetType().Name}.");
        }
    }

    private sealed class RepoAggregateFactory : IAggregateFactory<RepoAggregate>
    {
        public RepoAggregate Create() => new();
    }

    private sealed class FakeEventStore : IEventStore
    {
        public List<IDomainEvent> Appended { get; } = [];
        public long LastExpectedVersion { get; private set; }
        public Result<long> AppendResult { get; set; } = Result<long>.Success(1);

        public Task<Result<long>> AppendAsync(
            string streamId,
            long expectedVersion,
            IReadOnlyCollection<IDomainEvent> events,
            CancellationToken cancellationToken = default)
        {
            LastExpectedVersion = expectedVersion;
            Appended.AddRange(events);
            return Task.FromResult(AppendResult);
        }

        public Task<Result<IReadOnlyList<StoredEvent>>> ReadStreamAsync(
            string streamId,
            long fromVersion,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<IReadOnlyList<StoredEvent>>.Success([]));

        public Task<Result<IReadOnlyList<StoredEvent>>> ReadAllAsync(
            long fromPosition,
            int batchSize,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<IReadOnlyList<StoredEvent>>.Success([]));
    }

    private sealed class FakeOutboxStore : IOutboxStore
    {
        public List<OutboxMessage> Staged { get; } = [];

        public Task<Result> EnqueueAsync(
            IReadOnlyCollection<OutboxMessage> messages,
            CancellationToken cancellationToken = default)
        {
            Staged.AddRange(messages);
            return Task.FromResult(Result.Ok());
        }

        public Task<Result<IReadOnlyList<OutboxMessage>>> ClaimAsync(int batchSize, DateTimeOffset nowUtc, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<IReadOnlyList<OutboxMessage>>.Success([]));

        public Task<Result> MarkProcessedAsync(Guid id, Guid leaseId, DateTimeOffset processedOnUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> MarkFailedAsync(Guid id, Guid leaseId, string error, DateTimeOffset nextAttemptUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> DeadLetterAsync(Guid id, Guid leaseId, string error, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result<int>> CountPendingAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<int>.Success(0));

        public Task<Result<DateTimeOffset?>> OldestPendingOnUtcAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<DateTimeOffset?>.Success(null));

        public Task<Result<int>> DeleteProcessedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<int>.Success(0));
    }
}
