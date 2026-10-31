using Application.BuildingBlocks.Messaging.Inbox;

using Core.BuildingBlocks.Functional.Errors;
using Core.BuildingBlocks.Functional.Results;
using Application.Modules.Todos.Items.Models;

using Core.Modules.Todos.Items;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integration.Tests.BuildingBlocks.Messaging;

/// <summary>PostgreSQL integration tests for the EF Core inbox store.</summary>
[Collection("messaging")]
public sealed class InboxStoreIntegrationTest(MessagingFixture fixture) : IAsyncLifetime
{
    private const string Consumer = "todo-projection";

    private readonly MessagingFixture _fixture = fixture;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _fixture.CleanAsync();
        _fixture.CreateScope();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _fixture.CleanAsync();
    }

    [Fact(DisplayName = "First delivery runs the handler once and records the message")]
    public async Task ExecuteOnceAsync_WithNewMessage_ShouldRunHandlerAndInsertRow()
    {
        // Arrange
        Guid messageId = Guid.NewGuid();
        int calls = 0;
        DateTimeOffset before = DateTimeOffset.UtcNow;

        // Act
        Result result = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            _ =>
            {
                calls++;
                return Task.FromResult(Result.Ok());
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        calls.Should().Be(1);

        InboxMessage stored = await ReadAsync(messageId, Consumer);
        stored.MessageId.Should().Be(messageId);
        stored.Consumer.Should().Be(Consumer);
        stored.ProcessedOnUtc.Should().BeOnOrAfter(before);
        (await CountAsync()).Should().Be(1);
    }

    [Fact(DisplayName = "A duplicate delivery succeeds without running the handler again")]
    public async Task ExecuteOnceAsync_WithProcessedMessage_ShouldSkipHandler()
    {
        // Arrange
        Guid messageId = Guid.NewGuid();
        int calls = 0;
        Result first = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            _ =>
            {
                calls++;
                return Task.FromResult(Result.Ok());
            });
        first.IsSuccess.Should().BeTrue();

        // Act
        Result duplicate = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            _ =>
            {
                calls++;
                return Task.FromResult(Result.Ok());
            });

        // Assert
        duplicate.IsSuccess.Should().BeTrue();
        calls.Should().Be(1);
        (await CountAsync()).Should().Be(1);
    }

    [Fact(DisplayName = "A failed handler rolls back and the retry runs again")]
    public async Task ExecuteOnceAsync_WithHandlerFailure_ShouldRollBackAndAllowRetry()
    {
        // Arrange
        Guid messageId = Guid.NewGuid();
        var failure = Error.UnprocessableEntity(
            "Test.Inbox.HandlerFailed",
            "The projection could not be applied.");
        Result failedResult = Result.Fail(failure);
        int failedCalls = 0;

        // Act
        Result failed = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            _ =>
            {
                failedCalls++;
                return Task.FromResult(failedResult);
            });

        // Assert
        failed.Should().BeSameAs(failedResult);
        failedCalls.Should().Be(1);
        (await CountAsync()).Should().Be(0);

        // Act: a retry must see the message as unprocessed.
        int retryCalls = 0;
        Result retried = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            _ =>
            {
                retryCalls++;
                return Task.FromResult(Result.Ok());
            });

        // Assert
        retried.IsSuccess.Should().BeTrue();
        retryCalls.Should().Be(1);
        (await CountAsync()).Should().Be(1);
    }

    [Fact(DisplayName = "A concurrently inserted duplicate is treated as processed")]
    public async Task ExecuteOnceAsync_WithConcurrentDuplicateInsert_ShouldReturnSuccess()
    {
        // Arrange
        Guid messageId = Guid.NewGuid();
        await InsertAsync(messageId, Consumer);
        int calls = 0;

        // Act
        Result result = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            _ =>
            {
                calls++;
                return Task.FromResult(Result.Ok());
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        calls.Should().Be(0);
        (await CountAsync()).Should().Be(1);
    }

    [Fact(DisplayName = "A duplicate-key save from a racing consumer is treated as processed")]
    public async Task ExecuteOnceAsync_WithRacingInsertDuringHandler_ShouldReturnSuccess()
    {
        // Arrange
        Guid messageId = Guid.NewGuid();
        int calls = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act
        Task<Result> execution = _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            async _ =>
            {
                calls++;
                entered.TrySetResult();

                // Wait: a second consumer commits the same inbox row after the existence check.
                await release.Task;
                return Result.Ok();
            });

        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        try
        {
            await InsertAsync(messageId, Consumer);
        }
        finally
        {
            // Release: the handler must never wait for a failed competitor insert.
            release.TrySetResult();
        }

        Result result = await execution.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        calls.Should().Be(1);
        (await CountAsync()).Should().Be(1);
    }

    [Fact(DisplayName = "A thrown handler exception rolls back with a stable failure")]
    public async Task ExecuteOnceAsync_WhenHandlerThrows_ShouldRollBackAndReturnStableFailure()
    {
        // Arrange
        Guid messageId = Guid.NewGuid();

        // Act
        Result result = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            _ => throw new InvalidOperationException("projection exploded"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(InboxOutcome.Codes.ExecutionFailed);
        result.StatusCode.Should().Be(500);
        (await CountAsync()).Should().Be(0);
    }

    [Fact(DisplayName = "A successful handler commits its write with the marker")]
    public async Task ExecuteOnceAsync_WithHandlerWrite_ShouldCommitBoth()
    {
        // Arrange
        Guid messageId = Guid.NewGuid();
        TodoReadModel todo = NewTodo();

        // Act
        Result result = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            _ =>
            {
                // Handle: the write is staged on the same context, inside the store's transaction.
                _fixture.DbContext.Set<TodoReadModel>().Add(todo);
                return Task.FromResult(Result.Ok());
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await CountAsync()).Should().Be(1);
        (await TodoExistsAsync(todo.Id)).Should().BeTrue();
    }

    [Fact(DisplayName = "A failed handler rolls back its write with the marker")]
    public async Task ExecuteOnceAsync_WithHandlerWriteAndFailure_ShouldRollBackBoth()
    {
        // Arrange
        Guid messageId = Guid.NewGuid();
        TodoReadModel todo = NewTodo();
        Result failedResult = Result.Fail(
            Error.UnprocessableEntity("Test.Inbox.HandlerFailed", "write rejected"));

        // Act
        Result result = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            _ =>
            {
                _fixture.DbContext.Set<TodoReadModel>().Add(todo);
                return Task.FromResult(failedResult);
            });

        // Assert
        result.Should().BeSameAs(failedResult);
        (await CountAsync()).Should().Be(0);
        (await TodoExistsAsync(todo.Id)).Should().BeFalse();
    }

    [Fact(DisplayName = "The same message runs once per consumer")]
    public async Task ExecuteOnceAsync_WithDifferentConsumer_ShouldRunAgain()
    {
        // Arrange
        Guid messageId = Guid.NewGuid();
        int calls = 0;

        // Act
        Result first = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            Consumer,
            _ =>
            {
                calls++;
                return Task.FromResult(Result.Ok());
            });
        Result second = await _fixture.InboxStore.ExecuteOnceAsync(
            messageId,
            "todo-notifier",
            _ =>
            {
                calls++;
                return Task.FromResult(Result.Ok());
            });

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        calls.Should().Be(2);
        (await CountAsync()).Should().Be(2);
    }

    private static TodoReadModel NewTodo() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Title = "inbox integration",
        Status = TodoStatus.Pending,
        Priority = TodoPriority.Medium,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    private Task<InboxMessage> ReadAsync(Guid messageId, string consumer) => QueryAsync(context =>
        context.Set<InboxMessage>()
            .AsNoTracking()
            .SingleAsync(message => message.MessageId == messageId && message.Consumer == consumer));

    private Task<int> CountAsync() => QueryAsync(context =>
        context.Set<InboxMessage>().CountAsync());

    private Task<bool> TodoExistsAsync(Guid todoId) => QueryAsync(context =>
        context.Set<TodoReadModel>().AsNoTracking().AnyAsync(todo => todo.Id == todoId));

    private async Task InsertAsync(Guid messageId, string consumer)
    {
        // Insert: a separate scope commits the competing row outside the store's transaction.
        using IServiceScope scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Set<InboxMessage>().Add(new InboxMessage
        {
            MessageId = messageId,
            Consumer = consumer,
            ProcessedOnUtc = DateTimeOffset.UtcNow,
        });

        await context.SaveChangesAsync();
    }

    private async Task<T> QueryAsync<T>(Func<ApplicationDbContext, Task<T>> query)
    {
        using IServiceScope scope = _fixture.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        return await query(context);
    }
}