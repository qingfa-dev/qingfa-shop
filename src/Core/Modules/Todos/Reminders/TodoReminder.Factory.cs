using Core.BuildingBlocks.Domain.EventSourcing;

namespace Core.Modules.Todos.Reminders;

/// <summary>Creates an empty reminder for event-store rehydration.</summary>
public sealed class TodoReminderFactory : IAggregateFactory<TodoReminder>
{
    /// <inheritdoc />
    public TodoReminder Create() => new();
}
