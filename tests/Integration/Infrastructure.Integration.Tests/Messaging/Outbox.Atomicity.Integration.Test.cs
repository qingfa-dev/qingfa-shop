using Application.Common.EventSourcing;
using Application.Common.Messaging.Outbox;
using Application.Common.Messaging.Serialization;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Items.Models;

using Core.Common.Domain.Events;
using Core.Common.Monad.Results;
using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.EventSourcing;
using Infrastructure.Persistence.EntityFramework.Options;
using Infrastructure.Persistence.EntityFramework.Outbox;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Integration.Tests.Messaging;

/// <summary>Proves that event-stream state and outbox rows share one save boundary.</summary>
[Collection("messaging")]
public sealed class OutboxAtomicityIntegrationTest(MessagingFixture fixture) : IAsyncLifetime
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
    public async Task SavingAggregate_ShouldWriteStateAndOutboxRowTogether()
    {
        // Arrange
        ApplicationDbContext context = fixture.DbContext;
        Todo todo = CreateTodo("atomic");

        // Act
        Result saved = await fixture.TodoEvents.SaveAsync(todo);
        Result projected = await fixture.Runner.RunOnceAsync();

        // Assert
        saved.IsSuccess.Should().BeTrue();
        projected.IsSuccess.Should().BeTrue();
        (await context.Set<OutboxMessage>().CountAsync()).Should().Be(1);
        (await context.Set<StoredEvent>().CountAsync()).Should().Be(1);
        (await context.Set<TodoReadModel>().CountAsync()).Should().Be(1);
        todo.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task FailedSave_ShouldLeaveNoStateOrOutboxRowAndKeepTheEvent()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql =>
                npgsql.UsePersistenceMigrationHistory())
            .AddInterceptors(new ThrowingSaveInterceptor())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var unitOfWork = new EfCoreUnitOfWork(context);
        var outbox = new EfCoreOutboxStore(context, TimeProvider.System);
        var eventStore = new PostgresEventStore(context, fixture.DomainSerializer, TimeProvider.System);
        var repository = new EventSourcedRepository<Todo, Guid>(
            eventStore,
            new EfCoreSnapshotStore(context),
            new IntervalSnapshotPolicy(10),
            new TodoFactory(),
            fixture.DomainSerializer,
            outbox,
            unitOfWork,
            TimeProvider.System,
            NullLogger<EventSourcedRepository<Todo, Guid>>.Instance);
        Todo todo = CreateTodo("rejected");

        // Act
        Result save = await repository.SaveAsync(todo);

        // Assert
        save.IsFailure.Should().BeTrue();
        (await context.Set<OutboxMessage>().CountAsync()).Should().Be(0);
        (await context.Set<StoredEvent>().CountAsync()).Should().Be(0);
        (await context.Set<TodoReadModel>().CountAsync()).Should().Be(0);
        todo.UncommittedEvents.Should().ContainSingle();
    }

    private static Todo CreateTodo(string title)
        => Todo.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            title,
            DateTimeOffset.UtcNow).Value;

    /// <summary>Fails the save after the outbox interceptor has staged rows.</summary>
    private sealed class ThrowingSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromException<InterceptionResult<int>>(
                new InvalidOperationException("The save was rejected."));
    }
}
