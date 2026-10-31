using Application.BuildingBlocks.Caching;
using Application.BuildingBlocks.Messaging.Inbox;
using Application.Modules.Todos.Events;

using TodoReminderConsumer = Infrastructure.Modules.Todos.Integrations.Messaging.Handlers.TodoReminderChangedIntegrationHandler;

namespace Infrastructure.Unit.Tests.Modules.Todos.Integrations.Messaging.Handlers;

/// <summary>Unit tests for the reminder-changed integration consumer.</summary>
[Trait("Category", "Unit")]
public sealed class TodoReminderChangedIntegrationHandlerTest
{
    private static readonly Guid ReminderId = Guid.Parse("6c3e9a14-7d5b-4e2f-8a1c-9b0d2e4f6a8c");
    private static readonly Guid TodoId = Guid.Parse("5d2f8b03-9e4c-4a7b-8c1d-3f0e9a2b4c6d");

    [Fact(DisplayName = "Handle should evict the reminder and its todo once")]
    public async Task Handle_ShouldEvictBothEntriesOnce()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(service => service.RemoveAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(Result.Ok()));
        var inbox = new RecordingInboxStore();
        var handler = new TodoReminderConsumer(cache.Object, inbox);

        await handler.Handle(NewMessage(), TestContext.Current.CancellationToken);

        cache.Verify(
            service => service.RemoveAsync($"reminder:{ReminderId}", It.IsAny<CancellationToken>()),
            Times.Once);
        cache.Verify(
            service => service.RemoveAsync($"todo:{TodoId}", It.IsAny<CancellationToken>()),
            Times.Once);
        inbox.Calls.Should().ContainSingle();
        inbox.Calls[0].Result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Handle should stop when the reminder eviction fails")]
    public async Task Handle_WhenReminderEvictionFails_ShouldThrow()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(service => service.RemoveAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(Result.Fail(
                Error.InternalServerError("Test.Cache.Failed", "cache unavailable"))));
        var inbox = new RecordingInboxStore();
        var handler = new TodoReminderConsumer(cache.Object, inbox);

        Func<Task> act = () => handler.Handle(
            NewMessage(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ResultException>();
        cache.Verify(
            service => service.RemoveAsync($"reminder:{ReminderId}", It.IsAny<CancellationToken>()),
            Times.Once);
        cache.Verify(
            service => service.RemoveAsync($"todo:{TodoId}", It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Handle should surface a failure from the todo eviction")]
    public async Task Handle_WhenTodoEvictionFails_ShouldThrow()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(service => service.RemoveAsync(
                $"reminder:{ReminderId}", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(Result.Ok()));
        cache.Setup(service => service.RemoveAsync(
                $"todo:{TodoId}", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(Result.Fail(
                Error.InternalServerError("Test.Cache.Failed", "cache unavailable"))));
        var inbox = new RecordingInboxStore();
        var handler = new TodoReminderConsumer(cache.Object, inbox);

        Func<Task> act = () => handler.Handle(
            NewMessage(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ResultException>();
    }

    private static TodoReminderChangedIntegration NewMessage() => new()
    {
        ReminderId = ReminderId,
        TodoId = TodoId,
        TenantId = Guid.Parse("9a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d"),
        Change = TodoReminderChange.Snoozed,
        RemindAtUtc = new DateTimeOffset(2026, 10, 30, 14, 0, 0, TimeSpan.Zero),
        SnoozeCount = 1,
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
                Result failure = Result.Fail(InboxOutcome.Failure.ExecutionFailed(exception));
                Calls.Add(new Call(messageId, consumer, failure));

                return failure;
            }
        }
    }
}
