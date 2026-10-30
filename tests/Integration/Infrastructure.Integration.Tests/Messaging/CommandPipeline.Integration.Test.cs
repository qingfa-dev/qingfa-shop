using Application.Common.Events;
using Application.Common.EventSourcing;
using Application.Common.Messaging.Bus;
using Application.Common.Messaging.Outbox;
using Application.Common.Messaging.Serialization;
using Application.Features.Todos.Events;
using Application.Features.Todos.Items.Commands;

using Core.Common.Domain.Events;
using Core.Common.Monad.Results;
using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Outbox;
using Infrastructure.Persistence.EntityFramework.Outbox.Options;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Integration.Tests.Messaging;

/// <summary>Proves a command runs through MediatR, the transaction, and the outbox.</summary>
[Collection("messaging")]
public sealed class CommandPipelineIntegrationTest(MessagingFixture fixture) : IAsyncLifetime
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
    public async Task Command_ThroughMediatR_ShouldCommitStateAndDispatchTheOutboxRow()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var serializer = scope.ServiceProvider.GetRequiredService<IDomainEventSerializer>();
        var integrationSerializer = scope.ServiceProvider.GetRequiredService<IIntegrationEventSerializer>();
        var events = scope.ServiceProvider.GetRequiredService<IEventSourcedRepository<Todo, Guid>>();
        var publisher = new MediatRDomainPublisher(mediator);
        var dispatcher = new OutboxDispatcher(
            store,
            serializer,
            integrationSerializer,
            publisher,
            new SilentBus(),
            new OutboxSetting(),
            TimeProvider.System,
            NullLogger<OutboxDispatcher>.Instance);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid tenantId = MessagingFixture.TenantId;
        Todo todo = Todo.Create(Guid.NewGuid(), tenantId, "pipeline", now).Value;
        todo.Start(now, "arrange").IsSuccess.Should().BeTrue();
        (await events.SaveAsync(todo)).IsSuccess.Should().BeTrue();

        // Reset: the arrange step also staged TodoCreated and its start status change.
        await context.Set<OutboxMessage>().ExecuteDeleteAsync();

        // Act
        Result<CompleteTodo.Response> result = await mediator.Send(
            new CompleteTodo.UseCase(new CompleteTodo.Request(todo.Id)),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue(
            string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Message}")));
        OutboxMessage staged = await context.Set<OutboxMessage>().SingleAsync();
        staged.Kind.Should().Be(OutboxMessageKind.Domain);
        staged.Type.Should().Contain(nameof(TodoStatusChanged));

        // Act
        Result<int> dispatch = await dispatcher.DispatchBatchAsync();

        // Assert
        dispatch.Value.Should().Be(1);
        publisher.Published.Should().ContainSingle()
            .Which.Should().BeOfType<TodoStatusChanged>();

        OutboxMessage integration = await context.Set<OutboxMessage>()
            .SingleAsync(message => message.Kind == OutboxMessageKind.Integration);
        integration.Type.Should().Be("todo.completed");

        TodoCompletedIntegration completed = (TodoCompletedIntegration)integrationSerializer
            .Deserialize(integration.Type, integration.Version, integration.Payload);
        completed.TodoId.Should().Be(todo.Id);
        completed.TenantId.Should().Be(tenantId);
    }

    private sealed class MediatRDomainPublisher(IMediator mediator) : IDomainEventPublisher
    {
        public List<IDomainEvent> Published { get; } = [];

        public async Task<Result> PublishAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            foreach (IDomainEvent domainEvent in domainEvents)
            {
                Published.Add(domainEvent);
                await mediator.Publish(domainEvent, cancellationToken);
            }

            return Result.Ok();
        }
    }

    private sealed class SilentBus : IEventBus
    {
        public Task<Result> PublishAsync<T>(
            T integrationEvent,
            CancellationToken cancellationToken = default)
            where T : class, IIntegrationEvent
            => Task.FromResult(Result.Ok());
    }
}
