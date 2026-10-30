using Application.Common.Messaging.Bus;
using Application.Features.Todos.Events;

using Infrastructure.Integrations.Messaging;

using Wolverine;

namespace Infrastructure.Unit.Tests.Integrations.Messaging;

/// <summary>Unit tests for <see cref="WolverineEventBus"/>.</summary>
/// <remarks>
/// The bus only forwards to <see cref="IMessageBus"/>; durability belongs to the outbox
/// dispatcher, so the adapter mirrors the transport outcome as a <see cref="Result"/>.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Messaging")]
public sealed class WolverineEventBusTest
{
    private static readonly Guid TodoId = Guid.Parse("2f6c1a4e-8b3d-4f5a-9c7e-1d0b2a3c4e5f");

    #region Success

    [Fact(DisplayName = "PublishAsync should forward once and return success")]
    public async Task PublishAsync_WhenTransportAccepts_ShouldForwardOnceAndSucceed()
    {
        // Arrange
        var bus = new Mock<IMessageBus>();
        bus.Setup(instance => instance.PublishAsync(
                It.IsAny<TodoCompletedIntegration>(),
                It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);
        var sut = new WolverineEventBus(bus.Object);
        TodoCompletedIntegration message = NewMessage();

        // Act
        Result result = await sut.PublishAsync(message, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        bus.Verify(
            instance => instance.PublishAsync(
                It.Is<TodoCompletedIntegration>(published => published.MessageId == message.MessageId),
                It.IsAny<DeliveryOptions?>()),
            Times.Once);
    }

    #endregion

    #region Failure

    [Fact(DisplayName = "PublishAsync should map a transport exception to EventBus.Publish.Failed")]
    public async Task PublishAsync_WhenTransportThrows_ShouldReturnPublishFailed()
    {
        // Arrange
        var exception = new InvalidOperationException("broker is unavailable");
        var bus = new Mock<IMessageBus>();
        bus.Setup(instance => instance.PublishAsync(
                It.IsAny<TodoCompletedIntegration>(),
                It.IsAny<DeliveryOptions?>()))
            .Returns(new ValueTask(Task.FromException(exception)));
        var sut = new WolverineEventBus(bus.Object);
        TodoCompletedIntegration message = NewMessage();

        // Act
        Result result = await sut.PublishAsync(message, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(EventBusOutcome.Codes.PublishFailed);
        result.Errors[0].Metadata.Should().Contain(
            pair => Equals(pair.Key, "event.name") && Equals(pair.Value, message.EventName));
    }

    [Fact(DisplayName = "PublishAsync should rethrow cancellation instead of mapping it")]
    public async Task PublishAsync_WhenCancelled_ShouldRethrowOperationCanceled()
    {
        // Arrange
        var bus = new Mock<IMessageBus>(MockBehavior.Strict);
        var sut = new WolverineEventBus(bus.Object);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        Func<Task> act = () => sut.PublishAsync(NewMessage(), cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        bus.VerifyNoOtherCalls();
    }

    #endregion

    #region Fixtures

    private static TodoCompletedIntegration NewMessage() => new()
    {
        TodoId = TodoId,
        TenantId = Guid.Parse("7c8d9e0f-1a2b-4c3d-8e4f-5a6b7c8d9e0f"),
        CompletedAtUtc = new DateTimeOffset(2026, 10, 30, 10, 0, 0, TimeSpan.Zero),
    };

    #endregion
}
