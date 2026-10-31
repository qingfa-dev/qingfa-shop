using Core.Modules.Todos.Reminders;

namespace Core.Unit.Tests.Modules.Todos.Reminders;

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

    [Fact]
    public void Create_WithoutIdentifiers_ShouldFail()
    {
        Result<TodoReminder> result = TodoReminder.Create(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Now.AddHours(1), Now);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoReminderOutcome.Codes.IdRequired);
    }

    [Fact]
    public void Create_WithoutTodoId_ShouldFail()
    {
        Result<TodoReminder> result = TodoReminder.Create(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Now.AddHours(1), Now);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoReminderOutcome.Codes.TodoIdRequired);
    }

    [Fact]
    public void Create_WithoutTenantId_ShouldFail()
    {
        Result<TodoReminder> result = TodoReminder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Now.AddHours(1), Now);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoReminderOutcome.Codes.TenantIdRequired);
    }

    [Fact]
    public void Snooze_WithoutInstant_ShouldFail()
    {
        TodoReminder reminder = Create();

        Result result = reminder.Snooze(default, Now);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoReminderOutcome.Codes.RemindAtRequired);
    }

    [Fact]
    public void Snooze_BeyondLimit_ShouldFail()
    {
        TodoReminder reminder = Create();
        for (int count = 0; count < TodoReminderConstant.Constraints.MaxSnoozeCount; count++)
        {
            reminder.Snooze(Now.AddHours(2), Now).IsSuccess.Should().BeTrue();
        }

        Result result = reminder.Snooze(Now.AddHours(3), Now);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TodoReminderOutcome.Codes.SnoozeLimit);
    }

    [Fact]
    public void Dismiss_ShouldCarryTheReminderIdentity()
    {
        TodoReminder reminder = Create();

        reminder.Dismiss(Now).IsSuccess.Should().BeTrue();

        ReminderDismissed dismissed = reminder.UncommittedEvents
            .OfType<ReminderDismissed>()
            .Should().ContainSingle().Subject;
        dismissed.ReminderId.Should().Be(reminder.Id);
        dismissed.TodoId.Should().Be(reminder.TodoId);
        dismissed.TenantId.Should().Be(reminder.TenantId);
        dismissed.RemindAtUtc.Should().Be(reminder.RemindAtUtc);
        dismissed.DismissedAtUtc.Should().Be(Now);
    }

    private static TodoReminder Create()
        => TodoReminder.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(1),
            Now).Value;
}
