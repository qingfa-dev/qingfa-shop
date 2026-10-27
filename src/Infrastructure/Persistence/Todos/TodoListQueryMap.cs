using Application.Common.Querying.Mapping;
using Core.Todos;

namespace Infrastructure.Persistence.Todos;

/// <summary>
/// Pre-configured query map for <see cref="TodoList"/> enabling filtering, sorting, searching, and projection.
/// </summary>
public static class TodoListQueryMap
{
    /// <summary>Creates a query map covering all queryable fields on the TodoList aggregate.</summary>
    public static QueryMap<TodoList> Create()
    {
        var builder = QueryMapBuilder<TodoList>.Create()
            .Map("id", t => t.Id, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("name", t => t.Name, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable()
                .Searchable())
            .Map("description", t => t.Description, field => field
                .Filterable()
                .Selectable()
                .Searchable())
            .Map("createdAtUtc", t => t.CreatedAtUtc, field => field
                .Filterable()
                .Sortable()
                .Selectable())
            .Map("createdBy", t => t.CreatedBy, field => field
                .Filterable()
                .Selectable())
            .Map("tenantId", t => t.TenantId, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("isDeleted", t => t.IsDeleted, field => field
                .Filterable()
                .Selectable());

        var result = builder.Build();
        if (result.IsFailure)
            throw new InvalidOperationException(
                $"Failed to build TodoList query map: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        return result.Value;
    }
}
