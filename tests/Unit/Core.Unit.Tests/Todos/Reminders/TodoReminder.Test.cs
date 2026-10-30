using Core.Todos.Reminders;

namespace Core.Unit.Tests.Todos.Reminders;

/// <summary>Tests for the event-sourced reminder aggregate.</summary>
[Trait("Category", "Unit")]
public sealed class TodoReminderTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithFutureTime_ShouldSchedule()
    {
        Result<TodoReminder> result = TodoReminder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now.AddHours(1), Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReminderStatus.Scheduled);
        result.Value.SnoozeCount.Should().Be(0);
        result.Value.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ReminderScheduled>();
    }

    [Fact]
    public void Create_WithPastTime_ShouldFail()
    {
        Result<TodoReminder> result = TodoReminder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now.AddMinutes(-1), Now);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoReminderOutcome.Codes.RemindAtInPast);
    }

    [Fact]
    public void Snooze_ShouldMoveForwardAndIncrement()
    {
        TodoReminder reminder = Create();

        Result result = reminder.Snooze(Now.AddHours(2), Now);

        result.IsSuccess.Should().BeTrue();
        reminder.Status.Should().Be(ReminderStatus.Snoozed);
        reminder.SnoozeCount.Should().Be(1);
    }

    [Fact]
    public void Dismiss_IsTerminal()
    {
        TodoReminder reminder = Create();
        reminder.Dismiss(Now);

        Result second = reminder.Dismiss(Now.AddMinutes(1));

        second.IsFailure.Should().BeTrue();
        second.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoReminderOutcome.Codes.AlreadyDismissed);
    }

    [Fact]
    public void Snapshot_ShouldRoundTrip()
    {
        TodoReminder reminder = Create();
        reminder.Snooze(Now.AddHours(2), Now);

        string state = reminder.CaptureSnapshot();
        var restored = new TodoReminderFactory().Create();
        restored.RestoreSnapshot(state);

        restored.Id.Should().Be(reminder.Id);
        restored.TodoId.Should().Be(reminder.TodoId);
        restored.RemindAtUtc.Should().Be(reminder.RemindAtUtc);
        restored.Status.Should().Be(ReminderStatus.Snoozed);
        restored.SnoozeCount.Should().Be(1);
    }

    [Fact]
    public void Rehydration_ShouldMatchLiveState()
    {
        TodoReminder source = Create();
        source.Snooze(Now.AddHours(2), Now);
        var rehydrated = new TodoReminderFactory().Create();

        Result result = rehydrated.TryLoadFromHistory(2, source.UncommittedEvents);

        result.IsSuccess.Should().BeTrue();
        rehydrated.Id.Should().Be(source.Id);
        rehydrated.RemindAtUtc.Should().Be(source.RemindAtUtc);
        rehydrated.Status.Should().Be(ReminderStatus.Snoozed);
        rehydrated.SnoozeCount.Should().Be(1);
        rehydrated.Version.Should().Be(2);
    }

    private static TodoReminder Create()
        => TodoReminder.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(1),
            Now).Value;
}
