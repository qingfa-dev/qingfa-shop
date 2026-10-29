using Application.Common.Querying.Filtering;

using Core.Common.Concerns.Organization.Categorizable;

namespace Application.Common.Querying.Concerns;

/// <summary>Category-membership chains for a query builder.</summary>
public static class CategorizableQueryBuilderExtension
{
    /// <summary>Filters rows assigned to the category.</summary>
    public static QueryBuilder<T> WithCategory<T, TCategory>(
        this QueryBuilder<T> builder,
        TCategory category)
        where T : class, ICategorizable<TCategory> =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.Categories,
            FilterOperator.Contains,
            category);

    /// <summary>Filters rows assigned to any supplied category.</summary>
    public static QueryBuilder<T> WithAnyCategory<T, TCategory>(
        this QueryBuilder<T> builder,
        params TCategory[] categories)
        where T : class, ICategorizable<TCategory> =>
        ConcernQueryBuilder.WhereAny(builder, ConcernQueryField.Categories, categories);

    /// <summary>Filters rows assigned to every supplied category.</summary>
    public static QueryBuilder<T> WithAllCategories<T, TCategory>(
        this QueryBuilder<T> builder,
        params TCategory[] categories)
        where T : class, ICategorizable<TCategory> =>
        ConcernQueryBuilder.WhereAll(builder, ConcernQueryField.Categories, categories);
}
