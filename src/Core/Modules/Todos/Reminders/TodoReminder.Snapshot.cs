namespace Core.Modules.Todos.Reminders;

/// <summary>Serializable snapshot shape for <see cref="TodoReminder"/>.</summary>
public sealed record TodoReminderSnapshot(
    Guid Id,
    Guid TodoId,
    Guid TenantId,
    DateTimeOffset RemindAtUtc,
    ReminderStatus Status,
    int SnoozeCount,
    DateTimeOffset? DismissedAtUtc,
    long Version);
