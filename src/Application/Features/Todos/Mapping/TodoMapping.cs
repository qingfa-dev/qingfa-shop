using Contract.Todos.Models;

using Core.Todos.Items;

namespace Application.Features.Todos.Mapping;

/// <summary>Hand-written mapping from the Todo aggregate to its read models.</summary>
public static class TodoMapping
{
    /// <summary>Maps a todo to its full read model.</summary>
    /// <param name="todo">The todo to map.</param>
    /// <returns>The mapped model.</returns>
    public static TodoModel ToModel(this Todo todo)
    {
        ArgumentNullException.ThrowIfNull(todo);

        return new TodoModel(
            todo.Id,
            todo.TenantId,
            todo.Title,
            todo.Description,
            todo.Status,
            todo.Priority,
            todo.DueAtUtc,
            todo.CompletedAtUtc,
            todo.ListId,
            [.. todo.CategoryIds],
            todo.IsCompleted,
            todo.CreatedAtUtc,
            todo.CreatedBy,
            todo.ModifiedAtUtc,
            todo.ModifiedBy,
            todo.IsDeleted,
            todo.DeletedAtUtc,
            todo.DeletedBy);
    }

    /// <summary>Maps a todo to its compact list model.</summary>
    /// <param name="todo">The todo to map.</param>
    /// <returns>The mapped model.</returns>
    public static TodoSummaryModel ToSummary(this Todo todo)
    {
        ArgumentNullException.ThrowIfNull(todo);

        return new TodoSummaryModel(
            todo.Id,
            todo.Title,
            todo.Status,
            todo.Priority,
            todo.DueAtUtc,
            todo.ListId,
            [.. todo.CategoryIds],
            todo.IsCompleted,
            todo.CreatedAtUtc);
    }
}
