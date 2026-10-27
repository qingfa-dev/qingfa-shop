using Application.Common.Querying;
using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Mapping;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T>
{
    internal static Result<QueryField> Resolve(QueryMap<T> map, string name, bool sorting)
    {
        if (!map.TryGet(name, out QueryField? field))
            return Result<QueryField>.Fail(QueryOutcome.Failure.NotMapped(name));

        if (sorting ? !field.Sortable : !field.Filterable)
        {
            return Result<QueryField>.Fail(
                sorting ? QueryOutcome.Failure.NotSortable(name) : QueryOutcome.Failure.NotFilterable(name));
        }

        return Result<QueryField>.Success(field);
    }

    private static bool IsTextSearchable(QueryField field) =>
        field.Searchable &&
        field.Expression is not null &&
        !field.IsCollection &&
        !field.IsIndexed &&
        field.ComparisonType == typeof(string);

    internal static Result<QueryField> ResolveMapped(string name, QueryMap<T> map)
    {
        if (!map.TryGet(name, out QueryField? field))
            return Result<QueryField>.Fail(QueryOutcome.Failure.NotMapped(name));

        return Result<QueryField>.Success(field);
    }

    internal static Result<QueryField> ResolveSelectable(string name, QueryMap<T> map)
    {
        Result<QueryField> mapped = ResolveMapped(name, map);
        if (mapped.IsFailure)
            return mapped;

        // Validate: projections need a non-indexed, non-collection, single value.
        if (!mapped.Value.Selectable || mapped.Value.IsCollection || mapped.Value.IsIndexed ||
            mapped.Value.Expression is null)
        {
            return Result<QueryField>.Fail(QueryOutcome.Failure.NotSelectable(name));
        }

        return mapped;
    }

    internal static Result<QueryField> ResolveGroupable(string name, QueryMap<T> map)
    {
        Result<QueryField> mapped = ResolveMapped(name, map);
        if (mapped.IsFailure)
            return mapped;

        // Validate: maps are allowlists, so the translator enforces the flag as well
        // as the structural shape of a grouping key.
        if (!mapped.Value.Groupable || mapped.Value.IsCollection || mapped.Value.IsIndexed ||
            mapped.Value.Composite.Count > 0 || mapped.Value.Expression is null)
        {
            return Result<QueryField>.Fail(QueryOutcome.Failure.NotGroupable(name));
        }

        return mapped;
    }

    internal static Result<QueryField> ResolveAggregatable(AggregateSpec spec, QueryMap<T> map)
    {
        Result<QueryField> mapped = ResolveMapped(spec.Field, map);
        if (mapped.IsFailure)
            return mapped;

        // Validate: aggregates project one scalar member and honor the allowlist.
        if (!mapped.Value.Selectable || mapped.Value.IsCollection || mapped.Value.IsIndexed ||
            mapped.Value.Expression is null)
        {
            return Result<QueryField>.Fail(QueryOutcome.Failure.NotSelectable(spec.Field));
        }

        // Validate: reject unsupported function/type pairs before evaluating values.
        if (!Supports(spec.Function, mapped.Value.ComparisonType))
            return Result<QueryField>.Fail(QueryOutcome.Failure.AggregateUnsupported(spec.Field));

        return mapped;
    }
}