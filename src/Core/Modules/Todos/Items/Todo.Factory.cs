using Core.BuildingBlocks.Domain.EventSourcing;

namespace Core.Modules.Todos.Items;

/// <summary>Creates an empty todo for event-store rehydration.</summary>
public sealed class TodoFactory : IAggregateFactory<Todo>
{
    /// <inheritdoc />
    public Todo Create() => new();
}