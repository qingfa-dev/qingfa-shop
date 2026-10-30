using Application.Common.Events;
using Application.Common.Mediators.Commands;
using Application.Common.Messaging.Bus;
using Application.Common.Messaging.Outbox;
using Application.Common.Messaging.Serialization;
using Application.Common.Persistence.UnitOfWorks;

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
        var publisher = new RecordingPublisher();
        var dispatcher = new OutboxDispatcher(
            store,
            serializer,
            integrationSerializer,
            publisher,
            new SilentBus(),
            new OutboxSetting(),
            TimeProvider.System,
            NullLogger<OutboxDispatcher>.Instance);

        // Act
        Result<Guid> result = await mediator.Send(
            new CreateProbeTodo("pipeline"),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await context.Set<Todo>().CountAsync()).Should().Be(1);
        (await context.Set<OutboxMessage>().CountAsync()).Should().Be(1);

        // Act
        Result<int> dispatch = await dispatcher.DispatchBatchAsync();

        // Assert
        dispatch.Value.Should().Be(1);
        publisher.Published.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCreated>();
    }

    /// <summary>A command that writes a Todo through the unit of work.</summary>
    /// <param name="Title">The todo title.</param>
    public sealed record CreateProbeTodo(string Title) : ICommand<Guid>;

    /// <summary>Handles <see cref="CreateProbeTodo"/>.</summary>
    public sealed class CreateProbeTodoHandler(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork)
        : ICommandHandler<CreateProbeTodo, Guid>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> Handle(
            CreateProbeTodo command,
            CancellationToken cancellationToken)
        {
            Todo todo = Todo.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                command.Title,
                DateTimeOffset.UtcNow).Value;

            context.Add(todo);
            Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);

            return saved.IsFailure
                ? Result<Guid>.Fail(saved.Errors)
                : Result<Guid>.Success(todo.Id);
        }
    }

    private sealed class RecordingPublisher : IDomainEventPublisher
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

    private sealed class SilentBus : IEventBus
    {
        public Task<Result> PublishAsync<T>(
            T integrationEvent,
            CancellationToken cancellationToken = default)
            where T : class, IIntegrationEvent
            => Task.FromResult(Result.Ok());
    }
}
