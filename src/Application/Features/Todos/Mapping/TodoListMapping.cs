using Contract.Todos.Models;

using Core.Todos;

namespace Application.Features.Todos.Mapping;

/// <summary>Hand-written mapping from the TodoList aggregate to its read model.</summary>
public static class TodoListMapping
{
    /// <summary>Maps a todo list to its read model.</summary>
    /// <param name="list">The list to map.</param>
    /// <returns>The mapped model.</returns>
    public static TodoListModel ToModel(this TodoList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        return new TodoListModel(
            list.Id,
            list.TenantId,
            list.Name,
            list.Description,
            list.CreatedAtUtc,
            list.CreatedBy,
            list.ModifiedAtUtc,
            list.ModifiedBy,
            list.IsDeleted,
            list.DeletedAtUtc,
            list.DeletedBy);
    }
}
