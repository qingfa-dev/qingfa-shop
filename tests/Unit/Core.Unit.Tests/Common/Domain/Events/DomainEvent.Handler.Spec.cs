using Core.Common.Domain.Events;

using MediatR;

namespace Core.Unit.Tests.Common.Domain.Events;

/// <summary>
/// Tests for the <see cref="IDomainEventHandler"/> contract.
/// </summary>
[Trait("Category", "Unit")]
public class DomainEventHandlerSpec
{
    private sealed record TestEvent : DomainEvent;

    private sealed class RecordingHandler : IDomainEventHandler
    {
        public List<IDomainEvent> Received { get; } = [];

        public Task Handle(
            IDomainEvent notification,
            CancellationToken cancellationToken)
        {
            Received.Add(notification);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void Handler_ShouldImplementNotificationHandlerContract()
    {
        // Arrange
        IDomainEventHandler handler = new RecordingHandler();

        // Act
        INotificationHandler<IDomainEvent> asNotificationHandler = handler;

        // Assert
        asNotificationHandler.Should().BeSameAs(handler);
    }

    [Fact]
    public async Task Handle_ThroughNotificationHandler_ShouldReceiveEvent()
    {
        // Arrange
        INotificationHandler<IDomainEvent> handler = new RecordingHandler();
        IDomainEvent evt = new TestEvent();

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert
        var recording = (RecordingHandler)handler;
        recording.Received.Should().ContainSingle()
            .Which.Should().BeSameAs(evt);
    }
}