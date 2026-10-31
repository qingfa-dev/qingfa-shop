using Application.BuildingBlocks.Messaging.Events;
using Application.BuildingBlocks.Messaging.Bus;
using Application.BuildingBlocks.Messaging.Inbox;
using Application.BuildingBlocks.Messaging.Outbox;
using Application.BuildingBlocks.Messaging.Serialization;

using Core.BuildingBlocks.Domain.Events;
using Core.BuildingBlocks.Modeling.Metadata;
using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Todos.Items;

using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Integration.Tests.BuildingBlocks.Messaging;

/// <summary>End-to-end flow: state → outbox → publisher → inbox dedupe.</summary>
[Collection("messaging")]
public sealed class OutboxFlowIntegrationTest(MessagingFixture fixture) : IAsyncLifetime
{
    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await fixture.CleanAsync();
        fixture.CreateScope();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await fixture.CleanAsync();
    }

    [Fact]
    public async Task Flow_ShouldPublishDomainAndIntegrationEventsAndDedupeTheConsumer()
    {
        // Arrange
        var eventTypes = new EventTypeRegistry(
            typeof(TodoCreated).Assembly,
            typeof(Infrastructure.AssemblyMarker).Assembly);
        eventTypes.Register<TestIntegrationEvent>("test.integration", 1);
        var serializer = new JsonMessageSerializer(eventTypes);
        var publisher = new RecordingDomainPublisher();
        var bus = new RecordingEventBus();
        var dispatcher = new OutboxDispatcher(
            fixture.OutboxStore,
            serializer,
            serializer,
            publisher,
            bus,
            new OutboxSetting(),
            TimeProvider.System,
            NullLogger<OutboxDispatcher>.Instance);

        Todo todo = Todo.Create(
            Guid.NewGuid(),
            MessagingFixture.TenantId,
            "flow",
            DateTimeOffset.UtcNow).Value;

        // Act: the event stream and its outbox row commit together.
        Result saved = await fixture.TodoEvents.SaveAsync(todo);
        saved.IsSuccess.Should().BeTrue();

        // Assert
        (await fixture.DbContext.Set<OutboxMessage>().CountAsync()).Should().Be(1);

        // Act: dispatcher publishes the domain event and marks the row.
        Result<int> domainDispatch = await dispatcher.DispatchBatchAsync();

        // Assert
        domainDispatch.Value.Should().Be(1);
        publisher.Published.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCreated>();
        (await fixture.DbContext.Set<OutboxMessage>()
            .CountAsync(message => message.Status == OutboxMessageStatus.Processed))
            .Should().Be(1);

        // Act: stage and dispatch an integration event.
        var integrationEvent = new TestIntegrationEvent("hello");
        var integrationPublisher = new OutboxIntegrationEventPublisher(
            fixture.OutboxStore,
            serializer,
            fixture.UnitOfWork,
            NullLogger<OutboxIntegrationEventPublisher>.Instance);
        await integrationPublisher.PublishAsync(integrationEvent);
        Result<int> integrationDispatch = await dispatcher.DispatchBatchAsync();

        // Assert
        integrationDispatch.Value.Should().Be(1);
        bus.Published.Should().ContainSingle()
            .Which.Should().BeOfType<TestIntegrationEvent>();

        // Act: deliver the same message twice through the inbox.
        int executions = 0;
        Result first = await fixture.InboxStore.ExecuteOnceAsync(
            integrationEvent.MessageId,
            "flow-test",
            _ =>
            {
                executions++;
                return Task.FromResult(Result.Ok());
            });
        Result second = await fixture.InboxStore.ExecuteOnceAsync(
            integrationEvent.MessageId,
            "flow-test",
            _ =>
            {
                executions++;
                return Task.FromResult(Result.Ok());
            });

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        executions.Should().Be(1);
    }

    private sealed record TestIntegrationEvent(string Name) : IIntegrationEvent
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
        public string EventName => "test.integration";
        public int Version => 1;
        public DefaultMetadata Metadata { get; init; } = [];
    }

    private sealed class RecordingDomainPublisher : IDomainEventPublisher
    {
        public List<IDomainEvent> Published { get; } = [];

        public Task<Result> PublishAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            Published.AddRange(domainEvents);
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class RecordingEventBus : IEventBus
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
}
