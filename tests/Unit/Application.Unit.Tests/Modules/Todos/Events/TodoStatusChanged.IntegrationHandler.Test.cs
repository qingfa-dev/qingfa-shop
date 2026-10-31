using Application.BuildingBlocks.Messaging.Bus;
using Application.Modules.Todos.Events;

using Core.BuildingBlocks.Functional.Errors;
using Core.BuildingBlocks.Modeling.Metadata;
using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Todos.Items;

namespace Application.Unit.Tests.Modules.Todos.Events;

[Trait("Category", "Unit")]
public sealed class TodoStatusChangedIntegrationHandlerTest
{
    private static readonly Guid TodoId = Guid.Parse("2b9d3a41-6b0e-4f8a-9c1d-5f7e8a9b0c1d");
    private static readonly Guid TenantId = Guid.Parse("8f4c1d2e-3a5b-4c6d-8e7f-9a0b1c2d3e4f");
    private static readonly DateTimeOffset OccurredAtUtc = new(2026, 10, 30, 9, 30, 0, TimeSpan.Zero);

    private static TodoStatusChanged StatusChanged(
        TodoStatus from,
        TodoStatus to) =>
        new(TodoId, TenantId, from, to, OccurredAtUtc, "tester");

    #region Publish

    [Fact(DisplayName = "Handle should publish todo.completed when the status becomes completed")]
    public async Task Handle_WhenCompleted_ShouldPublishOneIntegrationEvent()
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new TodoStatusChangedIntegrationHandler(publisher);

        await handler.Handle(
            StatusChanged(TodoStatus.InProgress, TodoStatus.Completed),
            TestContext.Current.CancellationToken);

        publisher.Published.Should().ContainSingle();
        var integration = (TodoCompletedIntegration)publisher.Published[0];
        integration.TodoId.Should().Be(TodoId);
        integration.TenantId.Should().Be(TenantId);
        integration.CompletedAtUtc.Should().Be(OccurredAtUtc);
        integration.EventName.Should().Be("todo.completed");
        integration.Version.Should().Be(1);
    }

    [Theory(DisplayName = "Handle should publish nothing for a non-completion transition")]
    [InlineData(TodoStatus.Pending, TodoStatus.InProgress)]
    [InlineData(TodoStatus.Pending, TodoStatus.Cancelled)]
    [InlineData(TodoStatus.Completed, TodoStatus.Pending)]
    public async Task Handle_WhenNotCompleted_ShouldNotPublish(TodoStatus from, TodoStatus to)
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new TodoStatusChangedIntegrationHandler(publisher);

        await handler.Handle(
            StatusChanged(from, to),
            TestContext.Current.CancellationToken);

        publisher.Published.Should().BeEmpty();
    }

    #endregion

    #region Metadata

    [Fact(DisplayName = "Handle should stamp the tenant metadata")]
    public async Task Handle_ShouldStampTenantMetadata()
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new TodoStatusChangedIntegrationHandler(publisher);
        var domainEvent = StatusChanged(TodoStatus.InProgress, TodoStatus.Completed);

        domainEvent.GetTenantId().Should().BeNull();

        await handler.Handle(domainEvent, TestContext.Current.CancellationToken);

        var integration = (TodoCompletedIntegration)publisher.Published[0];
        integration.GetTenantId().Should().Be(TenantId);
    }

    [Fact(DisplayName = "Handle should copy the metadata instead of aliasing it")]
    public async Task Handle_ShouldNotAliasDomainEventMetadata()
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new TodoStatusChangedIntegrationHandler(publisher);
        var domainEvent = StatusChanged(TodoStatus.InProgress, TodoStatus.Completed);
        domainEvent.Metadata["source"] = "domain";

        await handler.Handle(domainEvent, TestContext.Current.CancellationToken);

        var integration = (TodoCompletedIntegration)publisher.Published[0];

        integration.Metadata.Should().NotBeSameAs(domainEvent.Metadata);

        domainEvent.Metadata["marker"] = "domain-only";

        integration.Metadata.Should().ContainKey("source");
        integration.Metadata.Should().NotContainKey("marker");
    }

    #endregion

    #region Failure

    [Fact(DisplayName = "Handle should propagate a publisher failure as ResultException")]
    public async Task Handle_ShouldPropagatePublisherFailure()
    {
        var publisher = new RecordingIntegrationEventPublisher
        {
            Result = Result.Fail(Error.InternalServerError("test.publish.failed", "publish failed")),
        };
        var handler = new TodoStatusChangedIntegrationHandler(publisher);

        Func<Task> act = () => handler.Handle(
            StatusChanged(TodoStatus.InProgress, TodoStatus.Completed),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ResultException>();
        publisher.Published.Should().ContainSingle();
    }

    #endregion

    #region Fakes

    private sealed class RecordingIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Result Result { get; init; } = Result.Ok();

        public Task<Result> PublishAsync(
            IIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(integrationEvent);

            Published.Add(integrationEvent);

            return Task.FromResult(Result);
        }
    }

    #endregion
}
