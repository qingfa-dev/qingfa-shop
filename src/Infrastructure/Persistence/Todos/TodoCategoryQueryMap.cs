using Application.Common.Querying.Mapping;
using Core.Todos;

namespace Infrastructure.Persistence.Todos;

/// <summary>
/// Pre-configured query map for <see cref="TodoCategory"/> enabling filtering, sorting, searching, and projection.
/// </summary>
public static class TodoCategoryQueryMap
{
    /// <summary>Creates a query map covering all queryable fields on the TodoCategory aggregate.</summary>
    public static QueryMap<TodoCategory> Create()
    {
        var builder = QueryMapBuilder<TodoCategory>.Create()
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
                $"Failed to build TodoCategory query map: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        return result.Value;
    }
}

/// <summary>Registers the TodoCategory query map during composition.</summary>
public sealed class TodoCategoryQueryMapDefinition : IQueryMapDefinition
{
    /// <inheritdoc />
    public Result Register(QueryMapRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.Register(TodoCategoryQueryMap.Create());
    }
}
