using Application.BuildingBlocks.Messaging.Bus;
using Application.BuildingBlocks.Messaging.Outbox;
using Application.BuildingBlocks.Messaging.Serialization;
using Application.BuildingBlocks.Persistence.UnitOfWorks;

using Core.BuildingBlocks.Domain.Events;

using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Tests for staging integration events in the outbox.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging")]
public sealed class OutboxIntegrationEventPublisherTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Publisher should stage an integration row and save it")]
    public async Task Publish_WhenStagingSucceeds_ShouldSaveOutbox()
    {
        // Arrange
        var store = new FakeStore();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        OutboxIntegrationEventPublisher publisher = CreatePublisher(store, unitOfWork.Object);
        var integrationEvent = new TestIntegrationEvent();

        // Act
        Result result = await publisher.PublishAsync(integrationEvent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        store.Messages.Should().ContainSingle()
            .Which.Kind.Should().Be(OutboxMessageKind.Integration);
        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Publisher should not save when staging fails")]
    public async Task Publish_WhenStagingFails_ShouldReturnFailure()
    {
        // Arrange
        var store = new FakeStore { EnqueueResult = Result.Fail(Error.InternalServerError("stage", "failed")) };
        var unitOfWork = new Mock<IUnitOfWork>();
        OutboxIntegrationEventPublisher publisher = CreatePublisher(store, unitOfWork.Object);

        // Act
        Result result = await publisher.PublishAsync(new TestIntegrationEvent());

        // Assert
        result.IsFailure.Should().BeTrue();
        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Publisher should surface a save failure")]
    public async Task Publish_WhenSaveFails_ShouldReturnFailure()
    {
        // Arrange
        var store = new FakeStore();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(Error.InternalServerError("save", "failed")));
        OutboxIntegrationEventPublisher publisher = CreatePublisher(store, unitOfWork.Object);

        // Act
        Result result = await publisher.PublishAsync(new TestIntegrationEvent());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    private static OutboxIntegrationEventPublisher CreatePublisher(
        FakeStore store,
        IUnitOfWork unitOfWork)
    {
        var eventTypes = new EventTypeRegistry(typeof(DomainEvent).Assembly);
        eventTypes.Register<TestIntegrationEvent>("test.publisher", 1);
        var serializer = new JsonMessageSerializer(eventTypes);

        return new OutboxIntegrationEventPublisher(
            store,
            serializer,
            unitOfWork,
            NullLogger<OutboxIntegrationEventPublisher>.Instance);
    }

    private sealed record TestIntegrationEvent : IIntegrationEvent
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTimeOffset OccurredOnUtc { get; init; } = Now;
        public string EventName => "test.publisher";
        public int Version => 1;
        public DefaultMetadata Metadata { get; init; } = [];
    }

    private sealed class FakeStore : IOutboxStore
    {
        public List<OutboxMessage> Messages { get; } = [];
        public Result EnqueueResult { get; set; } = Result.Ok();

        public Task<Result> EnqueueAsync(
            IReadOnlyCollection<OutboxMessage> messages,
            CancellationToken cancellationToken = default)
        {
            Messages.AddRange(messages);
            return Task.FromResult(EnqueueResult);
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
