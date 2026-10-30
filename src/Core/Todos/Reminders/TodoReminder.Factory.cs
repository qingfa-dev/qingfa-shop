using Core.Common.Domain.EventSourcing;

namespace Core.Todos.Reminders;

/// <summary>Creates an empty reminder for event-store rehydration.</summary>
public sealed class TodoReminderFactory : IAggregateFactory<TodoReminder>
{
    /// <inheritdoc />
    public TodoReminder Create() => new();
}
