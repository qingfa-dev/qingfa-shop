using System.Text.Json;

using Core.BuildingBlocks.Domain.EventSourcing;
using Core.BuildingBlocks.Domain.Events;

namespace Core.Modules.Todos.Reminders;

/// <summary>An event-sourced reminder attached to a todo.</summary>
public sealed class TodoReminder : EventSourcedAggregate<Guid>, ISnapshotable
{
    internal TodoReminder()
    {
    }

    public Guid TodoId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTimeOffset RemindAtUtc { get; private set; }
    public ReminderStatus Status { get; private set; } = TodoReminderConstant.Defaults.Status;
    public int SnoozeCount { get; private set; } = TodoReminderConstant.Defaults.SnoozeCount;
    public DateTimeOffset? DismissedAtUtc { get; private set; }

    /// <summary>Schedules a new reminder.</summary>
    public static Result<TodoReminder> Create(
        Guid id,
        Guid todoId,
        Guid tenantId,
        DateTimeOffset remindAtUtc,
        DateTimeOffset nowUtc)
    {
        Result validation = TodoReminderValidator.ValidateCreate(
            id,
            todoId,
            tenantId,
            remindAtUtc,
            nowUtc);

        if (validation.IsFailure)
            return Result<TodoReminder>.Fail(validation.Errors);

        var reminder = new TodoReminder();
        reminder.Raise(new ReminderScheduled(id, todoId, tenantId, remindAtUtc));
        return Result<TodoReminder>.Success(reminder);
    }

    /// <summary>Moves a pending reminder forward.</summary>
    public Result Snooze(DateTimeOffset remindAtUtc, DateTimeOffset nowUtc)
    {
        Result validation = TodoReminderValidator.ValidateSnooze(
            Status,
            SnoozeCount,
            remindAtUtc,
            nowUtc);

        if (validation.IsFailure)
            return validation;

        Raise(new ReminderSnoozed(Id, TodoId, TenantId, remindAtUtc, SnoozeCount + 1));
        return Result.Ok();
    }

    /// <summary>Dismisses a reminder permanently.</summary>
    public Result Dismiss(DateTimeOffset nowUtc)
    {
        Result validation = TodoReminderValidator.ValidateDismiss(Status);
        if (validation.IsFailure)
            return validation;

        Raise(new ReminderDismissed(Id, TodoId, TenantId, RemindAtUtc, nowUtc));
        return Result.Ok();
    }

    /// <inheritdoc />
    public string CaptureSnapshot()
        => JsonSerializer.Serialize(new TodoReminderSnapshot(
            Id,
            TodoId,
            TenantId,
            RemindAtUtc,
            Status,
            SnoozeCount,
            DismissedAtUtc,
            Version));

    /// <inheritdoc />
    public void RestoreSnapshot(string state)
    {
        TodoReminderSnapshot? snapshot = JsonSerializer.Deserialize<TodoReminderSnapshot>(state);

        // Boundary: a corrupt snapshot is a storage invariant failure; the repository falls back to replay.
        if (snapshot is null)
            throw new InvalidOperationException(TodoReminderOutcome.Failure.SnapshotInvalid.Message);

        Id = snapshot.Id;
        TodoId = snapshot.TodoId;
        TenantId = snapshot.TenantId;
        RemindAtUtc = snapshot.RemindAtUtc;
        Status = snapshot.Status;
        SnoozeCount = snapshot.SnoozeCount;
        DismissedAtUtc = snapshot.DismissedAtUtc;
    }

    /// <inheritdoc />
    protected override void When(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case ReminderScheduled scheduled:
                Id = scheduled.ReminderId;
                TodoId = scheduled.TodoId;
                TenantId = scheduled.TenantId;
                RemindAtUtc = scheduled.RemindAtUtc;
                Status = ReminderStatus.Scheduled;
                SnoozeCount = 0;
                DismissedAtUtc = null;
                break;

            case ReminderSnoozed snoozed:
                RemindAtUtc = snoozed.RemindAtUtc;
                SnoozeCount = snoozed.SnoozeCount;
                Status = ReminderStatus.Snoozed;
                break;

            case ReminderDismissed dismissed:
                Status = ReminderStatus.Dismissed;
                DismissedAtUtc = dismissed.DismissedAtUtc;
                break;

            default:
                throw new InvalidOperationException(
                    $"Event '{domainEvent.GetType().Name}' is not supported by TodoReminder.");
        }
    }
}
