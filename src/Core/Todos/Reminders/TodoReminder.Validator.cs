namespace Core.Todos.Reminders;

/// <summary>Validates reminder invariants without mutating the aggregate.</summary>
public static class TodoReminderValidator
{
    public static Result ValidateCreate(
        Guid id,
        Guid todoId,
        Guid tenantId,
        DateTimeOffset remindAtUtc,
        DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
            return Result.Fail(TodoReminderOutcome.Failure.IdRequired);
        if (todoId == Guid.Empty)
            return Result.Fail(TodoReminderOutcome.Failure.TodoIdRequired);
        if (tenantId == Guid.Empty)
            return Result.Fail(TodoReminderOutcome.Failure.TenantIdRequired);

        return remindAtUtc > nowUtc
            ? Result.Ok()
            : Result.Fail(TodoReminderOutcome.Failure.RemindAtInPast);
    }

    public static Result ValidateSnooze(
        ReminderStatus status,
        int snoozeCount,
        DateTimeOffset remindAtUtc,
        DateTimeOffset nowUtc)
    {
        if (status == ReminderStatus.Dismissed)
            return Result.Fail(TodoReminderOutcome.Failure.AlreadyDismissed);
        if (snoozeCount >= TodoReminderConstant.Constraints.MaxSnoozeCount)
            return Result.Fail(TodoReminderOutcome.Failure.SnoozeLimit);

        return remindAtUtc > nowUtc
            ? Result.Ok()
            : Result.Fail(TodoReminderOutcome.Failure.RemindAtInPast);
    }

    public static Result ValidateDismiss(ReminderStatus status)
        => status == ReminderStatus.Dismissed
            ? Result.Fail(TodoReminderOutcome.Failure.AlreadyDismissed)
            : Result.Ok();
}
