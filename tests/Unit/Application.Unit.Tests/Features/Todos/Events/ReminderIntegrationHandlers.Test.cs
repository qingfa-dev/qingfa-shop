using Application.Common.Messaging.Bus;
using Application.Features.Todos.Events;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Metadata;
using Core.Common.Monad.Results;
using Core.Todos.Reminders;

namespace Application.Unit.Tests.Features.Todos.Events;

/// <summary>Unit tests for the reminder integration-event bridges.</summary>
[Trait("Category", "Unit")]
public sealed class ReminderIntegrationHandlersTest
{
    private static readonly Guid ReminderId = Guid.Parse("1f2e3d4c-5b6a-4789-8abc-def012345678");
    private static readonly Guid TodoId = Guid.Parse("2b9d3a41-6b0e-4f8a-9c1d-5f7e8a9b0c1d");
    private static readonly Guid TenantId = Guid.Parse("8f4c1d2e-3a5b-4c6d-8e7f-9a0b1c2d3e4f");
    private static readonly DateTimeOffset RemindAtUtc = new(2026, 10, 30, 13, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Scheduled bridge should publish the scheduled change")]
    public async Task Scheduled_ShouldPublishChangedEvent()
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new ReminderScheduledIntegrationHandler(publisher);

        await handler.Handle(
            new ReminderScheduled(ReminderId, TodoId, TenantId, RemindAtUtc),
            TestContext.Current.CancellationToken);

        TodoReminderChangedIntegration integration = publisher.Single<TodoReminderChangedIntegration>();
        integration.ReminderId.Should().Be(ReminderId);
        integration.TodoId.Should().Be(TodoId);
        integration.TenantId.Should().Be(TenantId);
        integration.Change.Should().Be(TodoReminderChange.Scheduled);
        integration.RemindAtUtc.Should().Be(RemindAtUtc);
        integration.EventName.Should().Be("todo.reminder.changed");
        integration.GetTenantId().Should().Be(TenantId);
    }

    [Fact(DisplayName = "Snoozed bridge should publish the snooze count")]
    public async Task Snoozed_ShouldPublishSnoozeCount()
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new ReminderSnoozedIntegrationHandler(publisher);

        await handler.Handle(
            new ReminderSnoozed(ReminderId, TodoId, TenantId, RemindAtUtc, SnoozeCount: 3),
            TestContext.Current.CancellationToken);

        TodoReminderChangedIntegration integration = publisher.Single<TodoReminderChangedIntegration>();
        integration.Change.Should().Be(TodoReminderChange.Snoozed);
        integration.SnoozeCount.Should().Be(3);
        integration.RemindAtUtc.Should().Be(RemindAtUtc);
    }

    [Fact(DisplayName = "Dismissed bridge should publish the dismissal instant")]
    public async Task Dismissed_ShouldPublishDismissedAt()
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new ReminderDismissedIntegrationHandler(publisher);
        DateTimeOffset dismissedAtUtc = RemindAtUtc.AddMinutes(5);

        await handler.Handle(
            new ReminderDismissed(ReminderId, TodoId, TenantId, RemindAtUtc, dismissedAtUtc),
            TestContext.Current.CancellationToken);

        TodoReminderChangedIntegration integration = publisher.Single<TodoReminderChangedIntegration>();
        integration.Change.Should().Be(TodoReminderChange.Dismissed);
        integration.DismissedAtUtc.Should().Be(dismissedAtUtc);
    }

    [Fact(DisplayName = "A publisher failure should surface as ResultException")]
    public async Task Handle_WhenPublishFails_ShouldThrow()
    {
        var publisher = new RecordingIntegrationEventPublisher
        {
            Result = Result.Fail(Error.InternalServerError("test.publish.failed", "publish failed")),
        };
        var handler = new ReminderScheduledIntegrationHandler(publisher);

        Func<Task> act = () => handler.Handle(
            new ReminderScheduled(ReminderId, TodoId, TenantId, RemindAtUtc),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ResultException>();
    }

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

        public T Single<T>()
            where T : class, IIntegrationEvent
            => Published.Should().ContainSingle().Subject.Should().BeOfType<T>().Subject;
    }
}
