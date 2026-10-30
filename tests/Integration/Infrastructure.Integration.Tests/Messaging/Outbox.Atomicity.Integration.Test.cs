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
using Microsoft.Extensions.DependencyInjection;
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
    public async Task FailedOutboxStage_ShouldRollBackTheAppendAndOutboxTogether()
    {
        // Arrange: the first save appends events, the second save stages the outbox.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql =>
                npgsql.UsePersistenceMigrationHistory())
            .AddInterceptors(new ThrowingSaveInterceptor(throwOnSave: 2))
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

        Result<ITransaction> begin = await unitOfWork.BeginTransactionAsync();
        begin.IsSuccess.Should().BeTrue();
        await using ITransaction transaction = begin.Value;

        // Act
        Result save = await repository.SaveAsync(todo);

        // Assert: the append reached the ambient transaction and the outbox rows were staged.
        save.IsFailure.Should().BeTrue();
        (await context.Set<StoredEvent>().CountAsync()).Should().Be(1);
        context.ChangeTracker.Entries<OutboxMessage>().Should().ContainSingle();
        todo.UncommittedEvents.Should().ContainSingle();

        // Act: roll back the transaction that owned both writes.
        (await transaction.RollbackAsync()).IsSuccess.Should().BeTrue();
        context.ChangeTracker.Clear();

        // Assert: a fresh scope sees neither the event nor the outbox row.
        using IServiceScope verificationScope = fixture.Services.CreateScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await verification.Set<StoredEvent>().CountAsync()).Should().Be(0);
        (await verification.Set<OutboxMessage>().CountAsync()).Should().Be(0);
        (await verification.Set<TodoReadModel>().CountAsync()).Should().Be(0);
    }

    private static Todo CreateTodo(string title)
        => Todo.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            title,
            DateTimeOffset.UtcNow).Value;

    /// <summary>Fails one selected save so the outbox stage can be interrupted.</summary>
    private sealed class ThrowingSaveInterceptor(int throwOnSave) : SaveChangesInterceptor
    {
        private int _saveCount;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            int saveCount = Interlocked.Increment(ref _saveCount);
            return saveCount == throwOnSave
                ? ValueTask.FromException<InterceptionResult<int>>(
                    new InvalidOperationException("The save was rejected."))
                : ValueTask.FromResult(result);
        }
    }
}
