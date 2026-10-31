using Application.Modules.Todos.Items.Models;

using Core.Modules.Todos.Items;

namespace Application.Modules.Todos.Items.Mappings;

/// <summary>Maps a todo onto slice response models.</summary>
public static class TodoMapper
{
    /// <summary>Maps the todo into a new full response of the requested type.</summary>
    /// <typeparam name="T">The response model type.</typeparam>
    /// <param name="todo">The todo to map.</param>
    /// <returns>The mapped response.</returns>
    public static T ToModel<T>(this Todo todo)
        where T : TodoResponseModel, new()
    {
        ArgumentNullException.ThrowIfNull(todo);

        return new T
        {
            Id = todo.Id,
            TenantId = todo.TenantId,
            Title = todo.Title,
            Description = todo.Description,
            Status = todo.Status,
            Priority = todo.Priority,
            DueAtUtc = todo.DueAtUtc,
            CompletedAtUtc = todo.CompletedAtUtc,
            ListId = todo.ListId,
            CategoryIds = [.. todo.CategoryIds],
            IsCompleted = todo.IsCompleted,
            CreatedAtUtc = todo.CreatedAtUtc,
            CreatedBy = todo.CreatedBy,
            ModifiedAtUtc = todo.ModifiedAtUtc,
            ModifiedBy = todo.ModifiedBy,
            IsDeleted = todo.IsDeleted,
            DeletedAtUtc = todo.DeletedAtUtc,
            DeletedBy = todo.DeletedBy,
        };
    }

    /// <summary>Maps the todo into a new summary response of the requested type.</summary>
    /// <typeparam name="T">The summary response type.</typeparam>
    /// <param name="todo">The todo to map.</param>
    /// <returns>The mapped summary.</returns>
    public static T ToSummary<T>(this Todo todo)
        where T : TodoSummaryResponseModel, new()
    {
        ArgumentNullException.ThrowIfNull(todo);

        return new T
        {
            Id = todo.Id,
            Title = todo.Title,
            Status = todo.Status,
            Priority = todo.Priority,
            DueAtUtc = todo.DueAtUtc,
            ListId = todo.ListId,
            CategoryIds = [.. todo.CategoryIds],
            IsCompleted = todo.IsCompleted,
            CreatedAtUtc = todo.CreatedAtUtc,
        };
    }

    /// <summary>Maps the projected todo into a new full response of the requested type.</summary>
    /// <typeparam name="T">The response model type.</typeparam>
    /// <param name="readModel">The projected todo to map.</param>
    /// <returns>The mapped response.</returns>
    public static T ToModel<T>(this TodoReadModel readModel)
        where T : TodoResponseModel, new()
    {
        ArgumentNullException.ThrowIfNull(readModel);

        return new T
        {
            Id = readModel.Id,
            TenantId = readModel.TenantId,
            Title = readModel.Title,
            Description = readModel.Description,
            Status = readModel.Status,
            Priority = readModel.Priority,
            DueAtUtc = readModel.DueAtUtc,
            CompletedAtUtc = readModel.CompletedAtUtc,
            ListId = readModel.ListId,
            CategoryIds = [.. readModel.CategoryIds],
            IsCompleted = readModel.IsCompleted,
            CreatedAtUtc = readModel.CreatedAtUtc,
            CreatedBy = readModel.CreatedBy,
            ModifiedAtUtc = readModel.ModifiedAtUtc,
            ModifiedBy = readModel.ModifiedBy,
            IsDeleted = readModel.IsDeleted,
            DeletedAtUtc = readModel.DeletedAtUtc,
            DeletedBy = readModel.DeletedBy,
        };
    }

    /// <summary>Maps the projected todo into a new summary response of the requested type.</summary>
    /// <typeparam name="T">The summary response type.</typeparam>
    /// <param name="readModel">The projected todo to map.</param>
    /// <returns>The mapped summary.</returns>
    public static T ToSummary<T>(this TodoReadModel readModel)
        where T : TodoSummaryResponseModel, new()
    {
        ArgumentNullException.ThrowIfNull(readModel);

        return new T
        {
            Id = readModel.Id,
            Title = readModel.Title,
            Status = readModel.Status,
            Priority = readModel.Priority,
            DueAtUtc = readModel.DueAtUtc,
            ListId = readModel.ListId,
            CategoryIds = [.. readModel.CategoryIds],
            IsCompleted = readModel.IsCompleted,
            CreatedAtUtc = readModel.CreatedAtUtc,
        };
    }
}