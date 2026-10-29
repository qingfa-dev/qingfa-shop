using Application.Features.Todos.Lists.Models;

using Core.Todos.Lists;

namespace Application.Features.Todos.Mapping;

/// <summary>Maps a todo list onto a slice response model.</summary>
public static class TodoListMapper
{
    /// <summary>Maps the list into a new response of the requested type.</summary>
    /// <typeparam name="T">The response model type.</typeparam>
    /// <param name="list">The list to map.</param>
    /// <returns>The mapped response.</returns>
    public static T ToModel<T>(this TodoList list)
        where T : TodoListResponseModel, new()
    {
        ArgumentNullException.ThrowIfNull(list);

        return new T
        {
            Id = list.Id,
            TenantId = list.TenantId,
            Name = list.Name,
            Description = list.Description,
            CreatedAtUtc = list.CreatedAtUtc,
            CreatedBy = list.CreatedBy,
            ModifiedAtUtc = list.ModifiedAtUtc,
            ModifiedBy = list.ModifiedBy,
            IsDeleted = list.IsDeleted,
            DeletedAtUtc = list.DeletedAtUtc,
            DeletedBy = list.DeletedBy,
        };
    }
}
