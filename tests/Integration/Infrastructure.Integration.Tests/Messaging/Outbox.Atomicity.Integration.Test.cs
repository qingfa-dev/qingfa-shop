using Application.Common.Messaging.Outbox;

using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Options;
using Infrastructure.Persistence.EntityFramework.Outbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Integration.Tests.Messaging;

/// <summary>Proves that state and outbox rows share one save boundary.</summary>
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
        context.Add(todo);
        await context.SaveChangesAsync();

        // Assert
        (await context.Set<OutboxMessage>().CountAsync()).Should().Be(1);
        (await context.Set<Todo>().CountAsync()).Should().Be(1);
        todo.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task FailedSave_ShouldLeaveNoStateOrOutboxRowAndKeepTheEvent()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql =>
                npgsql.UsePersistenceMigrationHistory())
            .AddInterceptors(
                new OutboxInterceptor(fixture.DomainSerializer, TimeProvider.System),
                new ThrowingSaveInterceptor())
            .Options;

        await using var context = new ApplicationDbContext(options);
        Todo todo = CreateTodo("rejected");
        context.Add(todo);

        // Act
        Func<Task> act = () => context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        (await context.Set<OutboxMessage>().CountAsync()).Should().Be(0);
        (await context.Set<Todo>().CountAsync()).Should().Be(0);
        todo.Events.Should().ContainSingle();
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
