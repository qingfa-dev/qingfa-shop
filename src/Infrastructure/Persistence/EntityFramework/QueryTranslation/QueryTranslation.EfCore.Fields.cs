using Application.Common.Querying;
using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Mapping;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T>
{
    /// <summary>
    /// Resolves a field name to a QueryField, checking whether it is filterable or sortable.
    /// </summary>
    internal static Result<QueryField> Resolve(QueryMap<T> map, string name, bool sorting)
    {
        // Lookup: find the field in the map.
        if (!map.TryGet(name, out QueryField? field))
            return Result<QueryField>.Fail(QueryOutcome.Failure.NotMapped(name));

        // Check: filterable for filters, sortable for sort — the map controls access.
        if (sorting ? !field.Sortable : !field.Filterable)
        {
            return Result<QueryField>.Fail(
                sorting ? QueryOutcome.Failure.NotSortable(name) : QueryOutcome.Failure.NotFilterable(name));
        }

        // Check: collections have no single sort key; reject before building an order expression.
        if (sorting && field.IsCollection)
            return Result<QueryField>.Fail(QueryOutcome.Failure.NotSortable(name));

        return Result<QueryField>.Success(field);
    }

    /// <summary>
    /// True if the field is searchable: it must be a single string value, not a collection or indexed field.
    /// </summary>
    // Filter: only single string-valued, non-collection, non-indexed fields can be searched.
    private static bool IsTextSearchable(QueryField field) =>
        field.Searchable &&
        field.Expression is not null &&
        !field.IsCollection &&
        !field.IsIndexed &&
        field.ComparisonType == typeof(string);

    /// <summary>Resolves a field name to a QueryField without checking filterable or sortable flags.</summary>
    internal static Result<QueryField> ResolveMapped(string name, QueryMap<T> map)
    {
        // Lookup: find the field without checking capabilities — the caller validates.
        if (!map.TryGet(name, out QueryField? field))
            return Result<QueryField>.Fail(QueryOutcome.Failure.NotMapped(name));

        return Result<QueryField>.Success(field);
    }

    /// <summary>
    /// Resolves a field for select projection. Rejects collection and indexed fields
    /// because projections need single scalar values that can be mapped to a column.
    /// </summary>
    internal static Result<QueryField> ResolveSelectable(string name, QueryMap<T> map)
    {
        // Lookup: find the field first.
        Result<QueryField> mapped = ResolveMapped(name, map);
        if (mapped.IsFailure)
            return mapped;

        // Validate: projections need a non-indexed, non-collection, single scalar value.
        if (!mapped.Value.Selectable || mapped.Value.IsCollection || mapped.Value.IsIndexed ||
            mapped.Value.Expression is null)
        {
            return Result<QueryField>.Fail(QueryOutcome.Failure.NotSelectable(name));
        }

        return mapped;
    }

    /// <summary>
    /// Resolves a field for group-by. Rejects collections, indexed fields, and composite fields
    /// because grouping keys must be simple scalar values.
    /// </summary>
    internal static Result<QueryField> ResolveGroupable(string name, QueryMap<T> map)
    {
        // Lookup: find the field first.
        Result<QueryField> mapped = ResolveMapped(name, map);
        if (mapped.IsFailure)
            return mapped;

        // Validate: grouping keys must be simple scalars — no collections, indexes, or composites.
        if (!mapped.Value.Groupable || mapped.Value.IsCollection || mapped.Value.IsIndexed ||
            mapped.Value.Composite.Count > 0 || mapped.Value.Expression is null)
        {
            return Result<QueryField>.Fail(QueryOutcome.Failure.NotGroupable(name));
        }

        return mapped;
    }

    /// <summary>
    /// Resolves a field for aggregation. Validates that the field is selectable and that
    /// the aggregate function supports the field's type (e.g., Sum requires numeric).
    /// </summary>
    internal static Result<QueryField> ResolveAggregatable(AggregateSpec spec, QueryMap<T> map)
    {
        // Lookup: find the field first.
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