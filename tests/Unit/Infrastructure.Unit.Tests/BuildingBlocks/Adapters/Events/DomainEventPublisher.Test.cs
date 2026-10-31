using Application.BuildingBlocks.Messaging.Events;

using Core.BuildingBlocks.Domain.Events;

using Infrastructure.BuildingBlocks.Adapters.Events;

using MediatR;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Events;

/// <summary>
/// Unit tests for the MediatR-backed <see cref="DomainEventPublisher"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.DomainEvents")]
public sealed class DomainEventPublisherTest
{
    [Fact(DisplayName = "PublishAsync should dispatch every event in order")]
    public async Task PublishAsync_WithEvents_ShouldDispatchEveryEventInOrder()
    {
        // Arrange
        var publisher = new RecordingPublisher();
        var sut = new DomainEventPublisher(publisher);
        var first = new TestDomainEvent("first");
        var second = new TestDomainEvent("second");

        // Act
        Result result = await sut.PublishAsync([first, second]);

        // Assert
        result.IsSuccess.Should().BeTrue();
        publisher.Published.Should().Equal(first, second);
    }

    [Fact(DisplayName = "PublishAsync should dispatch by runtime type rather than IDomainEvent")]
    public async Task PublishAsync_WithEvents_ShouldDispatchByRuntimeType()
    {
        // Arrange
        var publisher = new RecordingPublisher();
        var sut = new DomainEventPublisher(publisher);
        var domainEvent = new TestDomainEvent("typed");

        // Act
        await sut.PublishAsync([domainEvent]);

        // Assert
        publisher.NonGenericCalls.Should().Be(1);
        publisher.PublishedTypes.Should().ContainSingle().Which.Should().Be(typeof(TestDomainEvent));
    }

    [Fact(DisplayName = "PublishAsync with no events should succeed without dispatching")]
    public async Task PublishAsync_WithNoEvents_ShouldNotDispatch()
    {
        // Arrange
        var publisher = new RecordingPublisher();
        var sut = new DomainEventPublisher(publisher);

        // Act
        Result result = await sut.PublishAsync([]);

        // Assert
        result.IsSuccess.Should().BeTrue();
        publisher.Calls.Should().Be(0);
    }

    [Fact(DisplayName = "PublishAsync with a null sequence should throw")]
    public async Task PublishAsync_WhenSequenceIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var sut = new DomainEventPublisher(new RecordingPublisher());

        // Act
        Func<Task> act = () => sut.PublishAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "PublishAsync with a null event should throw")]
    public async Task PublishAsync_WhenEventIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var sut = new DomainEventPublisher(new RecordingPublisher());

        // Act
        Func<Task> act = () => sut.PublishAsync([null!]);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "PublishAsync should return the owned failure with exception metadata")]
    public async Task PublishAsync_WhenHandlerFails_ShouldReturnDomainEventFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("handler failed");
        var publisher = new RecordingPublisher { Failure = exception };
        var sut = new DomainEventPublisher(publisher);

        // Act
        Result result = await sut.PublishAsync([new TestDomainEvent("failing")]);

        // Assert
        result.IsFailure.Should().BeTrue();
        Error error = result.Errors.Should().ContainSingle().Subject;
        error.Code.Should().Be(DomainEventOutcome.Codes.PublishFailed);
        error.Metadata[ErrorConstant.Metadata.ExceptionType].Should().Be(typeof(InvalidOperationException).FullName);
        error.Metadata[ErrorConstant.Metadata.ExceptionMessage].Should().Be(exception.Message);
        publisher.Calls.Should().Be(1);
    }

    [Fact(DisplayName = "PublishAsync should stop after the first failing event")]
    public async Task PublishAsync_WhenHandlerFails_ShouldStopDispatching()
    {
        // Arrange
        var publisher = new RecordingPublisher { Failure = new InvalidOperationException("handler failed") };
        var sut = new DomainEventPublisher(publisher);

        // Act
        Result result = await sut.PublishAsync(
        [
            new TestDomainEvent("first"),
            new TestDomainEvent("second"),
        ]);

        // Assert
        result.IsFailure.Should().BeTrue();
        publisher.Calls.Should().Be(1);
    }

    [Fact(DisplayName = "PublishAsync should propagate caller cancellation")]
    public async Task PublishAsync_WhenCallerCancels_ShouldPropagateCancellation()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var publisher = new RecordingPublisher { Cancellation = cancellation.Token };
        var sut = new DomainEventPublisher(publisher);

        // Act
        Func<Task> act = () => sut.PublishAsync([new TestDomainEvent("cancelled")], cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>Records mediator publications and can simulate handler failures.</summary>
    private sealed class RecordingPublisher : IPublisher
    {
        public List<IDomainEvent> Published { get; } = [];

        public List<Type> PublishedTypes { get; } = [];

        public int Calls { get; private set; }

        public int NonGenericCalls { get; private set; }

        public Exception? Failure { get; init; }

        public CancellationToken Cancellation { get; init; }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            NonGenericCalls++;
            return Record(notification);
        }

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return Record(notification!);
        }

        private Task Record(object notification)
        {
            Calls++;
            PublishedTypes.Add(notification.GetType());

            if (notification is IDomainEvent domainEvent)
                Published.Add(domainEvent);

            if (Failure is not null)
                throw Failure;

            if (Cancellation.IsCancellationRequested)
                throw new OperationCanceledException(Cancellation);

            return Task.CompletedTask;
        }
    }
}

/// <summary>Domain event used by the publisher tests.</summary>
public sealed record TestDomainEvent(string Name) : DomainEvent;
