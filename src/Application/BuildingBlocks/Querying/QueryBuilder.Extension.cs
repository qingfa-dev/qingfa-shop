using System.Linq.Expressions;

using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Querying.Projection;
using Application.BuildingBlocks.Querying.Searching;
using Application.BuildingBlocks.Querying.Sorting;

namespace Application.BuildingBlocks.Querying;

/// <summary>Chains <see cref="QueryBuilder{T}"/> operations over a <see cref="Result{TValue}"/>.</summary>
/// <remarks>
/// Every member forwards to the builder's <c>TryXxx</c> counterpart through
/// <see cref="ResultExtension.Bind{TValue, TOut}"/>, so a failed chain stays
/// failed and later calls are not evaluated.
/// </remarks>
public static class QueryBuilderExtensions
{
    // ---- Filters -------------------------------------------------------

    /// <summary>Adds a filter parsed from text, combined with AND.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="filter">Filter text; empty text is a no-op.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public static Result<QueryBuilder<T>> Where<T>(
        this Result<QueryBuilder<T>> chain,
        string filter)
        where T : class =>
        chain.Bind(builder => builder.TryWhere(filter));

    /// <summary>Adds a typed filter node, combined with AND.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="filter">The filter tree to add.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> Where<T>(
        this Result<QueryBuilder<T>> chain,
        FilterNode filter)
        where T : class =>
        chain.Bind(builder => builder.TryWhere(filter));

    /// <summary>Adds a predicate translated through the map, combined with AND.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="predicate">The predicate to translate.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> Where<T>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, bool>> predicate)
        where T : class =>
        chain.Bind(builder => builder.TryWhere(predicate));

    /// <summary>Adds a filter parsed from text, combined with OR.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="filter">Filter text; empty text is a no-op.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public static Result<QueryBuilder<T>> OrWhere<T>(
        this Result<QueryBuilder<T>> chain,
        string filter)
        where T : class =>
        chain.Bind(builder => builder.TryOrWhere(filter));

    /// <summary>Adds a typed filter node, combined with OR.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="filter">The filter tree to add.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> OrWhere<T>(
        this Result<QueryBuilder<T>> chain,
        FilterNode filter)
        where T : class =>
        chain.Bind(builder => builder.TryOrWhere(filter));

    /// <summary>Adds a predicate translated through the map, combined with OR.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="predicate">The predicate to translate.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> OrWhere<T>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, bool>> predicate)
        where T : class =>
        chain.Bind(builder => builder.TryOrWhere(predicate));

    // ---- Monadic hooks -------------------------------------------------

    /// <summary>Chains a state transformation.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="binder">The transformation applied to the current query.</param>
    /// <returns>A successful chain carrying the new builder, or the transformation failure.</returns>
    public static Result<QueryBuilder<T>> BindQuery<T>(
        this Result<QueryBuilder<T>> chain,
        Func<Query<T>, Result<Query<T>>> binder)
        where T : class =>
        chain.Bind(builder => builder.TryBind(binder));

    /// <summary>Requires the current query to satisfy a predicate.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="predicate">The invariant the query must satisfy.</param>
    /// <param name="error">The error reported when the invariant does not hold.</param>
    /// <returns>A successful chain carrying the builder, or the invariant failure.</returns>
    public static Result<QueryBuilder<T>> EnsureQuery<T>(
        this Result<QueryBuilder<T>> chain,
        Func<Query<T>, bool> predicate,
        Error error)
        where T : class =>
        chain.Bind(builder => builder.TryEnsure(predicate, error));

    // ---- Ordering ------------------------------------------------------

    /// <summary>Replaces the ordering with one parsed from text.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="orderBy">Ordering text such as <c>name,-score</c>.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public static Result<QueryBuilder<T>> OrderBy<T>(
        this Result<QueryBuilder<T>> chain,
        string orderBy)
        where T : class =>
        chain.Bind(builder => builder.TryOrderBy(orderBy));

    /// <summary>Replaces the ordering with a member accessed through the map.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="keySelector">The member to order by, ascending.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> OrderBy<T, TKey>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TKey>> keySelector)
        where T : class =>
        chain.Bind(builder => builder.TryOrderBy(keySelector));

    /// <summary>Replaces the ordering with a typed sort field.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="field">The sort field to use.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> OrderBy<T>(
        this Result<QueryBuilder<T>> chain,
        SortField field)
        where T : class =>
        chain.Bind(builder => builder.TryOrderBy(field));

    /// <summary>Replaces the ordering with a member accessed through the map, descending.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="keySelector">The member to order by, descending.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> OrderByDescending<T, TKey>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TKey>> keySelector)
        where T : class =>
        chain.Bind(builder => builder.TryOrderByDescending(keySelector));

    /// <summary>Appends an ordering parsed from text.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="orderBy">Ordering text such as <c>name,-score</c>.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public static Result<QueryBuilder<T>> ThenBy<T>(
        this Result<QueryBuilder<T>> chain,
        string orderBy)
        where T : class =>
        chain.Bind(builder => builder.TryThenBy(orderBy));

    /// <summary>Appends a member accessed through the map to the ordering.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="keySelector">The member to order by, ascending.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> ThenBy<T, TKey>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TKey>> keySelector)
        where T : class =>
        chain.Bind(builder => builder.TryThenBy(keySelector));

    /// <summary>Appends a typed sort field to the ordering.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="field">The sort field to append.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> ThenBy<T>(
        this Result<QueryBuilder<T>> chain,
        SortField field)
        where T : class =>
        chain.Bind(builder => builder.TryThenBy(field));

    /// <summary>Appends a descending member accessed through the map to the ordering.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="keySelector">The member to order by, descending.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> ThenByDescending<T, TKey>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TKey>> keySelector)
        where T : class =>
        chain.Bind(builder => builder.TryThenByDescending(keySelector));

    // ---- Paging --------------------------------------------------------

    /// <summary>Sets the page request.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size, between 1 and the configured maximum.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    public static Result<QueryBuilder<T>> Page<T>(
        this Result<QueryBuilder<T>> chain,
        int page,
        int pageSize)
        where T : class =>
        chain.Bind(builder => builder.TryPage(page, pageSize));

    /// <summary>Sets the page request from a typed request.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="request">The page request.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> Page<T>(
        this Result<QueryBuilder<T>> chain,
        PageRequest request)
        where T : class =>
        chain.Bind(builder => builder.TryPage(request));

    // ---- Search --------------------------------------------------------

    /// <summary>Sets or clears a text search.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="search">The text to search for; empty or whitespace clears the search.</param>
    /// <param name="mode">How the text is matched; <see cref="SearchMode.Contains"/> by default.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    public static Result<QueryBuilder<T>> Search<T>(
        this Result<QueryBuilder<T>> chain,
        string search,
        SearchMode mode = SearchMode.Contains)
        where T : class =>
        chain.Bind(builder => builder.TrySearch(search, mode));

    /// <summary>Sets a search request.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="request">The search request to apply.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> Search<T>(
        this Result<QueryBuilder<T>> chain,
        SearchRequest request)
        where T : class =>
        chain.Bind(builder => builder.TrySearch(request));

    // ---- Execution options ---------------------------------------------

    /// <summary>Sets whether duplicate rows are removed.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="value">True to apply DISTINCT; defaults to true.</param>
    /// <returns>A successful chain carrying the new builder.</returns>
    public static Result<QueryBuilder<T>> Distinct<T>(
        this Result<QueryBuilder<T>> chain,
        bool value = true)
        where T : class =>
        chain.Bind(builder => builder.TryDistinct(value));

    /// <summary>Sets whether entities are tracked.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="value">True to enable <c>AsNoTracking</c>; defaults to true.</param>
    /// <returns>A successful chain carrying the new builder.</returns>
    public static Result<QueryBuilder<T>> AsNoTracking<T>(
        this Result<QueryBuilder<T>> chain,
        bool value = true)
        where T : class =>
        chain.Bind(builder => builder.TryAsNoTracking(value));

    /// <summary>Sets the hard row cap.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="maxRows">The maximum rows to return; must be positive.</param>
    /// <returns>A successful chain carrying the new builder, or a typed failure when the cap is not positive.</returns>
    public static Result<QueryBuilder<T>> MaxRows<T>(
        this Result<QueryBuilder<T>> chain,
        int maxRows)
        where T : class =>
        chain.Bind(builder => builder.TryMaxRows(maxRows));

    /// <summary>Adds a navigation path to eager loading.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="path">The navigation path, such as <c>Children</c>.</param>
    /// <returns>A successful chain carrying the new builder, or a typed failure when the path is blank.</returns>
    public static Result<QueryBuilder<T>> Include<T>(
        this Result<QueryBuilder<T>> chain,
        string path)
        where T : class =>
        chain.Bind(builder => builder.TryInclude(path));

    /// <summary>Replaces the execution options.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="options">The complete options to apply.</param>
    /// <returns>A successful chain carrying the new builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> WithOptions<T>(
        this Result<QueryBuilder<T>> chain,
        QueryExecutionOptions options)
        where T : class =>
        chain.Bind(builder => builder.TryWithOptions(options));

    // ---- Select --------------------------------------------------------

    /// <summary>Replaces the projection with one parsed from text.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="select">Projection text such as <c>name,score</c>.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public static Result<QueryBuilder<T>> Select<T>(
        this Result<QueryBuilder<T>> chain,
        string select)
        where T : class =>
        chain.Bind(builder => builder.TrySelect(select));

    /// <summary>Replaces the projection with members accessed through the map.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="selectors">The members to project, in column order.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException">A selector is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> Select<T>(
        this Result<QueryBuilder<T>> chain,
        params Expression<Func<T, object?>>[] selectors)
        where T : class =>
        chain.Bind(builder => builder.TrySelect(selectors));

    /// <summary>Replaces the projection with typed select fields.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="fields">The projection columns to use.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> Select<T>(
        this Result<QueryBuilder<T>> chain,
        IReadOnlyList<SelectField> fields)
        where T : class =>
        chain.Bind(builder => builder.TrySelect(fields));

    /// <summary>Appends projection text to the current projection.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="select">Projection text such as <c>name,score</c>.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public static Result<QueryBuilder<T>> AddSelect<T>(
        this Result<QueryBuilder<T>> chain,
        string select)
        where T : class =>
        chain.Bind(builder => builder.TryAddSelect(select));

    /// <summary>Appends members accessed through the map to the current projection.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="selectors">The members to append, in column order.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException">A selector is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> AddSelect<T>(
        this Result<QueryBuilder<T>> chain,
        params Expression<Func<T, object?>>[] selectors)
        where T : class =>
        chain.Bind(builder => builder.TryAddSelect(selectors));

    /// <summary>Appends typed select fields to the current projection.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="fields">The projection columns to append.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> AddSelect<T>(
        this Result<QueryBuilder<T>> chain,
        IReadOnlyList<SelectField> fields)
        where T : class =>
        chain.Bind(builder => builder.TryAddSelect(fields));

    // ---- Projection ----------------------------------------------------

    /// <summary>Stores an entity-to-response projection for a later projected build.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TResponse">The projected response type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="projection">The projection expression.</param>
    /// <returns>A successful chain carrying the new builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="projection"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> Project<T, TResponse>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TResponse>> projection)
        where T : class =>
        chain.Bind(builder => builder.TryProject(projection));

    // ---- Grouping ------------------------------------------------------

    /// <summary>Replaces the grouping keys with those parsed from text.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="groupBy">Grouping text such as <c>active,name</c>.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public static Result<QueryBuilder<T>> GroupBy<T>(
        this Result<QueryBuilder<T>> chain,
        string groupBy)
        where T : class =>
        chain.Bind(builder => builder.TryGroupBy(groupBy));

    /// <summary>Replaces the grouping keys with members accessed through the map.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="selector">The member or anonymous tuple of members to group by.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> GroupBy<T>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, object?>> selector)
        where T : class =>
        chain.Bind(builder => builder.TryGroupBy(selector));

    /// <summary>Appends grouping keys parsed from text.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="groupBy">Grouping text such as <c>active,name</c>.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public static Result<QueryBuilder<T>> AddGroupBy<T>(
        this Result<QueryBuilder<T>> chain,
        string groupBy)
        where T : class =>
        chain.Bind(builder => builder.TryAddGroupBy(groupBy));

    // ---- Aggregation ---------------------------------------------------

    /// <summary>Appends an aggregate computed over a member accessed through the map.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="function">The aggregate function to apply.</param>
    /// <param name="field">The member to aggregate.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> Aggregate<T, TField>(
        this Result<QueryBuilder<T>> chain,
        AggregateFunction function,
        Expression<Func<T, TField>> field,
        string? alias = null)
        where T : class =>
        chain.Bind(builder => builder.TryAggregate(function, field, alias));

    /// <summary>Appends aggregate expressions parsed from text.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="aggregate">Aggregate text such as <c>count(score),sum(score)</c>.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public static Result<QueryBuilder<T>> Aggregate<T>(
        this Result<QueryBuilder<T>> chain,
        string aggregate)
        where T : class =>
        chain.Bind(builder => builder.TryAggregate(aggregate));

    /// <summary>Appends a count aggregate over a member.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="field">The member to count.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public static Result<QueryBuilder<T>> Count<T, TField>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TField>> field,
        string? alias = null)
        where T : class =>
        chain.Bind(builder => builder.TryCount(field, alias));

    /// <summary>Appends a sum aggregate over a member.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="field">The member to sum.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public static Result<QueryBuilder<T>> Sum<T, TField>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TField>> field,
        string? alias = null)
        where T : class =>
        chain.Bind(builder => builder.TrySum(field, alias));

    /// <summary>Appends an average aggregate over a member.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="field">The member to average.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public static Result<QueryBuilder<T>> Average<T, TField>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TField>> field,
        string? alias = null)
        where T : class =>
        chain.Bind(builder => builder.TryAverage(field, alias));

    /// <summary>Appends a minimum aggregate over a member.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="field">The member to minimize.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public static Result<QueryBuilder<T>> Min<T, TField>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TField>> field,
        string? alias = null)
        where T : class =>
        chain.Bind(builder => builder.TryMin(field, alias));

    /// <summary>Appends a maximum aggregate over a member.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="field">The member to maximize.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public static Result<QueryBuilder<T>> Max<T, TField>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TField>> field,
        string? alias = null)
        where T : class =>
        chain.Bind(builder => builder.TryMax(field, alias));

    /// <summary>Appends a distinct-count aggregate over a member.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="field">The member whose distinct values are counted.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public static Result<QueryBuilder<T>> DistinctCount<T, TField>(
        this Result<QueryBuilder<T>> chain,
        Expression<Func<T, TField>> field,
        string? alias = null)
        where T : class =>
        chain.Bind(builder => builder.TryDistinctCount(field, alias));

    // ---- Build ---------------------------------------------------------

    /// <summary>Builds the composed query.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <returns>The query, or the first failure accumulated by the chain.</returns>
    public static Result<Query<T>> Build<T>(this Result<QueryBuilder<T>> chain)
        where T : class =>
        chain.Bind(builder => builder.Build());

    /// <summary>Builds the composed query, requiring an entity shape.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="whenTabular">The error reported when the query is tabular; a default applies when omitted.</param>
    /// <returns>The entity query, or a failure when the state failed or the shape is not Entity.</returns>
    public static Result<Query<T>> BuildEntity<T>(
        this Result<QueryBuilder<T>> chain,
        Error? whenTabular = null)
        where T : class =>
        chain.Bind(builder => builder.BuildEntity(whenTabular));

    /// <summary>Builds the composed query as a typed projection.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <typeparam name="TResponse">The projected response type.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <returns>The projected query, or a failure when the shape or projection does not match.</returns>
    public static Result<ProjectedQuery<T, TResponse>> BuildProjected<T, TResponse>(
        this Result<QueryBuilder<T>> chain)
        where T : class =>
        chain.Bind(builder => builder.BuildProjected<TResponse>());

    /// <summary>Builds the composed query as a tabular result.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="chain">The monadic builder chain.</param>
    /// <param name="whenEntity">The error reported when the query is an entity query; a default applies when omitted.</param>
    /// <returns>The tabular query, or a failure when the shape or combination is invalid.</returns>
    public static Result<Query<T, TabularResult>> BuildTabular<T>(
        this Result<QueryBuilder<T>> chain,
        Error? whenEntity = null)
        where T : class =>
        chain.Bind(builder => builder.BuildTabular(whenEntity));
}