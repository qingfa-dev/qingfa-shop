using Application.Common.Querying.Mapping;
using Application.Features.Todos.Reminders.Models;

namespace Infrastructure.Persistence.Schemas.Todos.Queries;

/// <summary>Pre-configured query map for the reminder read model.</summary>
public static class TodoReminderQueryMap
{
    /// <summary>Creates a query map covering every queryable reminder field.</summary>
    /// <returns>The built query map, or throws when the definition is inconsistent.</returns>
    public static QueryMap<TodoReminderModel> Create()
    {
        var builder = QueryMapBuilder<TodoReminderModel>.Create()
            .Map("id", reminder => reminder.Id, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("todoId", reminder => reminder.TodoId, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("tenantId", reminder => reminder.TenantId, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("remindAtUtc", reminder => reminder.RemindAtUtc, field => field
                .Filterable()
                .Sortable()
                .Selectable())
            .Map("status", reminder => reminder.Status, field => field
                .Filterable()
                .Sortable()
                .Selectable())
            .Map("snoozeCount", reminder => reminder.SnoozeCount, field => field
                .Filterable()
                .Sortable()
                .Selectable())
            .Map("dismissedAtUtc", reminder => reminder.DismissedAtUtc, field => field
                .Filterable()
                .Sortable()
                .Selectable())
            .Map("version", reminder => reminder.Version, field => field
                .Filterable()
                .Sortable()
                .Selectable());

        Result<QueryMap<TodoReminderModel>> result = builder.Build();

        // Boundary: composition-time map definitions are static; an invalid one is a defect.
        return result.IsFailure
            ? throw new InvalidOperationException(
                $"Failed to build TodoReminder query map: {string.Join(", ", result.Errors.Select(error => error.Message))}")
            : result.Value;
    }
}

/// <summary>Registers the reminder query map during composition.</summary>
public sealed class TodoReminderQueryMapDefinition : IQueryMapDefinition
{
    /// <inheritdoc />
    public Result Register(QueryMapRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.Register(TodoReminderQueryMap.Create());
    }
}
