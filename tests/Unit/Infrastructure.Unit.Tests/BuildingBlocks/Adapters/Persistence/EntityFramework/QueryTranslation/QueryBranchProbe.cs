using Application.BuildingBlocks.Querying.Mapping;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

/// <summary>Entity with nullable, nested, collection, and numeric members for edge tests.</summary>
public sealed record BranchProbe
{
    public Guid Id { get; init; }
    public int? NullableScore { get; init; }
    public string? Name { get; init; }
    public Priority Priority { get; init; }
    public Priority? NullablePriority { get; init; }
    public BranchChild? Child { get; init; }
    public List<BranchChild> Children { get; init; } = [];
    public string[] Tags { get; init; } = [];
    public int[] Values { get; init; } = [];
    public int?[] Scores { get; init; } = [];
    public double Number { get; init; }
}

/// <summary>Nested child used by the sort and guard tests.</summary>
public sealed record BranchChild
{
    public string? Name { get; init; }
    public BranchParent? Parent { get; init; }
}

/// <summary>Second-level nested parent used by guard-chain tests.</summary>
public sealed record BranchParent
{
    public string? Name { get; init; }
}

/// <summary>Maps for the branch tests.</summary>
public static class BranchProbeMap
{
    /// <summary>Maps every scalar, nested, and collection member used by edge tests.</summary>
    public static QueryMap<BranchProbe> Create()
    {
        var builder = QueryMapBuilder<BranchProbe>.Create()
            .Map("id", probe => probe.Id, field => field
                .Filterable().Sortable().Selectable().Groupable())
            .Map("name", probe => probe.Name, field => field
                .Filterable().Sortable().Selectable().Groupable().Searchable())
            .Map("nullableScore", probe => probe.NullableScore, field => field
                .Filterable().Sortable().Selectable().Groupable())
            .Map("priority", probe => probe.Priority, field => field
                .Filterable().Sortable().Selectable().Groupable())
            .Map("nullablePriority", probe => probe.NullablePriority, field => field
                .Filterable().Sortable().Selectable().Groupable())
            .Map("number", probe => probe.Number, field => field
                .Filterable().Sortable().Selectable().Groupable())
            .Map("child.name", probe => probe.Child!.Name, field => field
                .Sortable()
                .MemberPath("child.name"))
            .Map("children", probe => probe.Children, field => field
                .Filterable()
                .IsCollection(true)
                .ElementType(typeof(BranchChild)))
            .Map("tags", probe => probe.Tags, field => field
                .Filterable()
                .IsCollection(true)
                .ElementType(typeof(string)))
            .Map("values", probe => probe.Values, field => field
                .Filterable()
                .IsCollection(true)
                .ElementType(typeof(int)))
            .Map("scores", probe => probe.Scores, field => field
                .Filterable()
                .IsCollection(true)
                .ElementType(typeof(int?)))
            .MapIndexed<int>("tag", (probe, index) => probe.Tags[index], field => field
                .Filterable());

        var result = builder.Build();
        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to build branch map: {string.Join(", ", result.Errors.Select(error => error.Message))}");

        return result.Value;
    }
}
