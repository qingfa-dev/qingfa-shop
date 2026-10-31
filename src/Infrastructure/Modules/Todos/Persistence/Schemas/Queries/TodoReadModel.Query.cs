using Application.BuildingBlocks.Querying.Mapping;
using Application.Modules.Todos.Items.Models;

namespace Infrastructure.Modules.Todos.Persistence.Schemas.Queries;

/// <summary>
/// Pre-configured query map for <see cref="TodoReadModel"/> enabling filtering, sorting, searching, and projection.
/// </summary>
public static class TodoReadModelQueryMap
{
    /// <summary>Creates a query map covering all queryable fields on the todo read model.</summary>
    public static QueryMap<TodoReadModel> Create()
    {
        var builder = QueryMapBuilder<TodoReadModel>.Create()
            .Map("id", t => t.Id, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("title", t => t.Title, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable()
                .Searchable())
            .Map("description", t => t.Description, field => field
                .Filterable()
                .Selectable()
                .Searchable())
            .Map("status", t => t.Status, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("priority", t => t.Priority, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("dueAtUtc", t => t.DueAtUtc, field => field
                .Filterable()
                .Sortable()
                .Selectable())
            .Map("completedAtUtc", t => t.CompletedAtUtc, field => field
                .Filterable()
                .Sortable()
                .Selectable())
            .Map("listId", t => t.ListId, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("isCompleted", t => t.IsCompleted, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
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
                $"Failed to build TodoReadModel query map: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        return result.Value;
    }
}

/// <summary>Registers the todo read-model query map during composition.</summary>
public sealed class TodoReadModelQueryMapDefinition : IQueryMapDefinition
{
    /// <inheritdoc />
    public Result Register(QueryMapRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.Register(TodoReadModelQueryMap.Create());
    }
}