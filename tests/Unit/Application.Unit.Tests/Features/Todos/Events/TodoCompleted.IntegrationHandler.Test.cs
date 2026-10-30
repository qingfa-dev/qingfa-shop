using Application.Common.Messaging.Bus;
using Application.Features.Todos.Events;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Metadata;
using Core.Common.Monad.Results;
using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Events;

[Trait("Category", "Unit")]
public sealed class TodoCompletedIntegrationHandlerTest
{
    private static readonly Guid TodoId = Guid.Parse("2b9d3a41-6b0e-4f8a-9c1d-5f7e8a9b0c1d");
    private static readonly Guid TenantId = Guid.Parse("8f4c1d2e-3a5b-4c6d-8e7f-9a0b1c2d3e4f");
    private static readonly DateTimeOffset CompletedAtUtc = new(2026, 10, 30, 9, 30, 0, TimeSpan.Zero);

    #region Publish

    [Fact(DisplayName = "Handle should publish the mapped integration event")]
    public async Task Handle_ShouldPublishMappedIntegrationEvent()
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new TodoCompletedIntegrationHandler(publisher);
        var domainEvent = new TodoCompleted(TodoId, TenantId, CompletedAtUtc);

        await handler.Handle(domainEvent, TestContext.Current.CancellationToken);

        publisher.Published.Should().ContainSingle();
        publisher.Published[0].Should().BeOfType<TodoCompletedIntegration>();

        var integration = (TodoCompletedIntegration)publisher.Published[0];
        integration.TodoId.Should().Be(TodoId);
        integration.TenantId.Should().Be(TenantId);
        integration.CompletedAtUtc.Should().Be(CompletedAtUtc);
        integration.EventName.Should().Be("todo.completed");
        integration.Version.Should().Be(1);
    }

    #endregion

    #region Metadata

    [Fact(DisplayName = "Handle should stamp the tenant metadata when the source carries none")]
    public async Task Handle_ShouldStampTenantMetadata()
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new TodoCompletedIntegrationHandler(publisher);
        var domainEvent = new TodoCompleted(TodoId, TenantId, CompletedAtUtc);

        domainEvent.GetTenantId().Should().BeNull();

        await handler.Handle(domainEvent, TestContext.Current.CancellationToken);

        var integration = (TodoCompletedIntegration)publisher.Published[0];
        integration.GetTenantId().Should().Be(TenantId);
    }

    [Fact(DisplayName = "Handle should copy the metadata instead of aliasing it")]
    public async Task Handle_ShouldNotAliasDomainEventMetadata()
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new TodoCompletedIntegrationHandler(publisher);
        var domainEvent = new TodoCompleted(TodoId, TenantId, CompletedAtUtc);
        domainEvent.Metadata["source"] = "domain";

        await handler.Handle(domainEvent, TestContext.Current.CancellationToken);

        var integration = (TodoCompletedIntegration)publisher.Published[0];

        integration.Metadata.Should().NotBeSameAs(domainEvent.Metadata);

        domainEvent.Metadata["marker"] = "domain-only";

        integration.Metadata.Should().ContainKey("source");
        integration.Metadata.Should().NotContainKey("marker");
        domainEvent.Metadata.Should().NotContainKey(MetadataConstant.Keyword.TenantId);
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
        var handler = new TodoCompletedIntegrationHandler(publisher);

        Func<Task> act = () => handler.Handle(
            new TodoCompleted(TodoId, TenantId, CompletedAtUtc),
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