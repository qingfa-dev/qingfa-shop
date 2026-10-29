using Application.Common.Querying.Filtering;

using Core.Common.Concerns.Foundation.Referenceable;

namespace Application.Common.Querying.Concerns;

/// <summary>Reference chains for a query builder.</summary>
public static class ReferenceableQueryBuilderExtension
{
    /// <summary>Filters rows by exact reference.</summary>
    public static QueryBuilder<T> WithReference<T>(
        this QueryBuilder<T> builder,
        string reference)
        where T : class, IReferenceable =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.Reference, reference);

    /// <summary>Filters rows whose reference contains the supplied text.</summary>
    public static QueryBuilder<T> ReferenceContains<T>(
        this QueryBuilder<T> builder,
        string text)
        where T : class, IReferenceable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.Reference,
            FilterOperator.Contains,
            text);
}
