namespace Core.Todos.Reminders;

/// <summary>Validates reminder invariants without mutating the aggregate.</summary>
public static class TodoReminderValidator
{
    /// <summary>Validates the state required to schedule a reminder.</summary>
    public static Result ValidateCreate(
        Guid id,
        Guid todoId,
        Guid tenantId,
        DateTimeOffset remindAtUtc,
        DateTimeOffset nowUtc)
    {
        Result identity = ValidateIdentity(id, todoId, tenantId);

        return identity.IsFailure
            ? identity
            : ValidateRemindAt(remindAtUtc, nowUtc);
    }

    /// <summary>Validates the identifiers that identify a reminder.</summary>
    public static Result ValidateIdentity(Guid id, Guid todoId, Guid tenantId)
    {
        if (id == Guid.Empty)
            return Result.Fail(TodoReminderOutcome.Failure.IdRequired);
        if (todoId == Guid.Empty)
            return Result.Fail(TodoReminderOutcome.Failure.TodoIdRequired);
        if (tenantId == Guid.Empty)
            return Result.Fail(TodoReminderOutcome.Failure.TenantIdRequired);

        return Result.Ok();
    }

    /// <summary>Validates a reminder instant against the current instant.</summary>
    public static Result ValidateRemindAt(DateTimeOffset remindAtUtc, DateTimeOffset nowUtc)
    {
        if (remindAtUtc == default)
            return Result.Fail(TodoReminderOutcome.Failure.RemindAtRequired);

        return remindAtUtc > nowUtc
            ? Result.Ok()
            : Result.Fail(TodoReminderOutcome.Failure.RemindAtInPast);
    }

    /// <summary>Validates that a request supplied a reminder instant.</summary>
    public static Result ValidateRemindAtSpecified(DateTimeOffset remindAtUtc)
        => remindAtUtc == default
            ? Result.Fail(TodoReminderOutcome.Failure.RemindAtRequired)
            : Result.Ok();

    /// <summary>Validates a snooze transition against the current state.</summary>
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

        return ValidateRemindAt(remindAtUtc, nowUtc);
    }

    /// <summary>Validates a dismiss transition against the current state.</summary>
    public static Result ValidateDismiss(ReminderStatus status)
        => status == ReminderStatus.Dismissed
            ? Result.Fail(TodoReminderOutcome.Failure.AlreadyDismissed)
            : Result.Ok();
}
