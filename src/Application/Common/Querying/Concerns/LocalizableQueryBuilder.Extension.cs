using Application.Common.Querying.Filtering;

using Core.Common.Concerns.Content.Localizable;

namespace Application.Common.Querying.Concerns;

/// <summary>Localized-value membership chains for a query builder.</summary>
public static class LocalizableQueryBuilderExtension
{
    /// <summary>Filters rows carrying the localized value.</summary>
    public static QueryBuilder<T> WithLocalizedValue<T>(
        this QueryBuilder<T> builder,
        LocalizedValue value)
        where T : class, ILocalizable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.LocalizedValues,
            FilterOperator.Contains,
            value);

    /// <summary>Filters rows carrying any supplied localized value.</summary>
    public static QueryBuilder<T> WithAnyLocalizedValue<T>(
        this QueryBuilder<T> builder,
        params LocalizedValue[] values)
        where T : class, ILocalizable =>
        ConcernQueryBuilder.WhereAny(builder, ConcernQueryField.LocalizedValues, values);

    /// <summary>Filters rows carrying every supplied localized value.</summary>
    public static QueryBuilder<T> WithAllLocalizedValues<T>(
        this QueryBuilder<T> builder,
        params LocalizedValue[] values)
        where T : class, ILocalizable =>
        ConcernQueryBuilder.WhereAll(builder, ConcernQueryField.LocalizedValues, values);
}
