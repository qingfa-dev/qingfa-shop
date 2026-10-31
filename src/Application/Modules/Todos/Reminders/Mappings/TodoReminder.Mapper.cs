using Application.Modules.Todos.Reminders.Models;

using Core.Modules.Todos.Reminders;

namespace Application.Modules.Todos.Reminders.Mappings;

/// <summary>Maps reminder aggregates and read models onto response models.</summary>
public static class TodoReminderMapper
{
    /// <summary>Maps a reminder aggregate into a full response.</summary>
    /// <typeparam name="T">The response model type.</typeparam>
    /// <param name="reminder">The reminder to map.</param>
    /// <returns>The mapped response.</returns>
    public static T ToModel<T>(this TodoReminder reminder)
        where T : TodoReminderResponseModel, new()
    {
        ArgumentNullException.ThrowIfNull(reminder);

        return new T
        {
            Id = reminder.Id,
            TodoId = reminder.TodoId,
            TenantId = reminder.TenantId,
            RemindAtUtc = reminder.RemindAtUtc,
            Status = reminder.Status,
            SnoozeCount = reminder.SnoozeCount,
            DismissedAtUtc = reminder.DismissedAtUtc,
            Version = reminder.Version,
        };
    }

    /// <summary>Maps a reminder read model into a full response.</summary>
    /// <typeparam name="T">The response model type.</typeparam>
    /// <param name="reminder">The read model to map.</param>
    /// <returns>The mapped response.</returns>
    public static T ToModel<T>(this TodoReminderModel reminder)
        where T : TodoReminderResponseModel, new()
    {
        ArgumentNullException.ThrowIfNull(reminder);

        return new T
        {
            Id = reminder.Id,
            TodoId = reminder.TodoId,
            TenantId = reminder.TenantId,
            RemindAtUtc = reminder.RemindAtUtc,
            Status = reminder.Status,
            SnoozeCount = reminder.SnoozeCount,
            DismissedAtUtc = reminder.DismissedAtUtc,
            Version = reminder.Version,
        };
    }

    /// <summary>Maps a reminder read model into a compact search response.</summary>
    /// <typeparam name="T">The summary response type.</typeparam>
    /// <param name="reminder">The read model to map.</param>
    /// <returns>The mapped summary.</returns>
    public static T ToSummary<T>(this TodoReminderModel reminder)
        where T : TodoReminderSummaryResponseModel, new()
    {
        ArgumentNullException.ThrowIfNull(reminder);

        return new T
        {
            Id = reminder.Id,
            TodoId = reminder.TodoId,
            RemindAtUtc = reminder.RemindAtUtc,
            Status = reminder.Status,
            SnoozeCount = reminder.SnoozeCount,
            DismissedAtUtc = reminder.DismissedAtUtc,
            Version = reminder.Version,
        };
    }
}
