using Core.Common.Domain.EventSourcing;

namespace Core.Todos.Items;

/// <summary>Creates an empty todo for event-store rehydration.</summary>
public sealed class TodoFactory : IAggregateFactory<Todo>
{
    /// <inheritdoc />
    public Todo Create() => new();
}