using Application.Common.Caching;
using Application.Common.Messaging.Inbox;
using Application.Features.Todos.Events;

// Map: the Application bridge handler shares the name, so the consumer is aliased explicitly.
using TodoConsumer = Infrastructure.Integrations.Messaging.Handlers.TodoCompletedIntegrationHandler;

namespace Infrastructure.Unit.Tests.Integrations.Messaging.Handlers;

/// <summary>Unit tests for the example todo-completed integration consumer.</summary>
[Trait("Category", "Unit")]
public sealed class TodoCompletedIntegrationHandlerTest
{
    private static readonly Guid TodoId = Guid.Parse("5d2f8b03-9e4c-4a7b-8c1d-3f0e9a2b4c6d");

    #region Invalidation

    [Fact(DisplayName = "Handle should evict the cached todo once")]
    public async Task Handle_ShouldEvictCachedTodoOnce()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(service => service.RemoveAsync(
                $"todo:{TodoId}", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(Result.Ok()));
        var inbox = new RecordingInboxStore();
        var handler = new TodoConsumer(cache.Object, inbox);
        var message = NewMessage();

        await handler.Handle(message, TestContext.Current.CancellationToken);

        cache.Verify(
            service => service.RemoveAsync($"todo:{TodoId}", It.IsAny<CancellationToken>()),
            Times.Once);
        inbox.Calls.Should().ContainSingle();
        inbox.Calls[0].Result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Failure

    [Fact(DisplayName = "Handle should throw ResultException when the cache eviction fails")]
    public async Task Handle_WhenEvictionFails_ShouldThrowResultException()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(service => service.RemoveAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(Result.Fail(
                Error.InternalServerError("Test.Cache.Failed", "cache unavailable"))));
        var inbox = new RecordingInboxStore();
        var handler = new TodoConsumer(cache.Object, inbox);

        Func<Task> act = () => handler.Handle(
            NewMessage(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ResultException>();
        cache.Verify(
            service => service.RemoveAsync($"todo:{TodoId}", It.IsAny<CancellationToken>()),
            Times.Once);
        inbox.Calls.Should().ContainSingle();
        inbox.Calls[0].Result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Fixtures

    private static TodoCompletedIntegration NewMessage() => new()
    {
        TodoId = TodoId,
        TenantId = Guid.Parse("9a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d"),
        CompletedAtUtc = new DateTimeOffset(2026, 10, 30, 10, 0, 0, TimeSpan.Zero),
    };

    private sealed record Call(Guid MessageId, string Consumer, Result Result);

    private sealed class RecordingInboxStore : IInboxStore
    {
        public List<Call> Calls { get; } = [];

        public async Task<Result> ExecuteOnceAsync(
            Guid messageId,
            string consumer,
            Func<CancellationToken, Task<Result>> handler,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Result result = await handler(cancellationToken);
                Calls.Add(new Call(messageId, consumer, result));

                return result;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Compensate: mimic the store, which rolls back and reports a thrown consumer.
                Result failure = Result.Fail(InboxOutcome.Failure.ExecutionFailed(exception));
                Calls.Add(new Call(messageId, consumer, failure));

                return failure;
            }
        }
    }

    #endregion
}