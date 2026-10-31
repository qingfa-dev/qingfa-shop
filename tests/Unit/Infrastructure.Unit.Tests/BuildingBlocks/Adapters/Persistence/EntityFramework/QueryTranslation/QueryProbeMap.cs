using Application.BuildingBlocks.Querying.Mapping;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

/// <summary>Pre-configured query map for <see cref="QueryProbe"/> used in tests.</summary>
public static class QueryProbeMap
{
    /// <summary>Creates a query map with typical field configurations for testing.</summary>
    public static QueryMap<QueryProbe> Create()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("id", probe => probe.Id, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("name", probe => probe.Name, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable()
                .Searchable())
            .Map("score", probe => probe.Score, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("createdAt", probe => probe.CreatedAt, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable())
            .Map("tags", probe => probe.Tags, field => field
                .Filterable()
                .Selectable()
                .Searchable())
            .Map("priority", probe => probe.Priority, field => field
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable());

        var result = builder.Build();
        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to build query map: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        return result.Value;
    }
}