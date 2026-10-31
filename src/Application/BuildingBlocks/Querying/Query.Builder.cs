using System.Linq.Expressions;

using Application.BuildingBlocks.Querying.Expressions;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Mapping;

namespace Application.BuildingBlocks.Querying;

/// <summary>Composes a provider-neutral query from text, typed filters, and transformations.</summary>
/// <typeparam name="T">The entity the query addresses.</typeparam>
/// <remarks>
/// A builder is immutable: every call returns a new builder that carries the
/// resulting state. The state is a <see cref="Result{TValue}"/> rather than an
/// exception, so invalid user-supplied query text fails the chain instead of
/// throwing. A builder whose state has failed keeps that failure; later calls
/// are no-ops that preserve the original errors.
/// </remarks>
public sealed partial class QueryBuilder<T>
    where T : class
{
    // Guard: _map is null only when the provider reported no registered map, which always
    // produces a failed state. A successful state therefore always has a usable map.
    private readonly QueryMap<T>? _map;
    private readonly Result<Query<T>> _state;
    private readonly LambdaExpression? _projection;
    private readonly FilterNode? _scope;

    private QueryBuilder(
        QueryMap<T>? map,
        Result<Query<T>> state,
        LambdaExpression? projection,
        FilterNode? scope)
    {
        _map = map;
        _state = state;
        _projection = projection;
        _scope = scope;
    }

    // ---- Factories -----------------------------------------------------

    /// <summary>Starts an empty entity query against a known map.</summary>
    /// <param name="map">The field map the query may address.</param>
    /// <returns>A builder whose state is a query with no filter, ordering, or projection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
    public static QueryBuilder<T> Create(QueryMap<T> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return new QueryBuilder<T>(
            map,
            Result<Query<T>>.Ok(new Query<T>()),
            projection: null,
            scope: null);
    }

    /// <summary>Starts an empty entity query as a monadic chain.</summary>
    /// <param name="map">The field map the query may address.</param>
    /// <returns>A successful chain carrying the new builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
    public static Result<QueryBuilder<T>> CreateResult(QueryMap<T> map) => Create(map).AsResult();

    /// <summary>Starts a builder from parsed query parameters.</summary>
    /// <param name="map">The field map the query may address.</param>
    /// <param name="parameters">The raw query parameters.</param>
    /// <returns>
    /// A builder carrying the parsed query, or a failed builder when any part
    /// of the parameters is invalid.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="map"/> or <paramref name="parameters"/> is <c>null</c>.
    /// </exception>
    public static QueryBuilder<T> FromParameters(
        QueryMap<T> map,
        QueryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(parameters);
        return new QueryBuilder<T>(
            map,
            QueryParser.Parse(parameters, map),
            projection: null,
            scope: null);
    }

    /// <summary>Starts a builder from parsed query parameters using the registered map.</summary>
    /// <param name="provider">The query map provider composed for the application.</param>
    /// <param name="parameters">The raw query parameters.</param>
    /// <returns>
    /// A builder carrying the parsed query; when no map is registered for
    /// <typeparamref name="T"/>, a failed builder whose state is never usable.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="provider"/> or <paramref name="parameters"/> is <c>null</c>.
    /// </exception>
    public static QueryBuilder<T> FromParameters(
        IQueryMapProvider provider,
        QueryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(parameters);

        if (!provider.TryGet<T>(out QueryMap<T>? map))
        {
            return With(
                null,
                Result<Query<T>>.Fail(QueryMapOutcome.Failure.NotRegistered(typeof(T))),
                null,
                null);
        }

        // Guard: TryGet returned true, so the provider supplied a map.
        return new QueryBuilder<T>(
            map!,
            QueryParser.Parse(parameters, map!),
            projection: null,
            scope: null);
    }

    /// <summary>Starts a monadic chain from parsed query parameters.</summary>
    /// <param name="map">The field map the query may address.</param>
    /// <param name="parameters">The raw query parameters.</param>
    /// <returns>A successful chain carrying the builder, or the parse failure.</returns>
    public static Result<QueryBuilder<T>> FromParametersResult(
        QueryMap<T> map,
        QueryParameters parameters) =>
        FromParameters(map, parameters).AsResult();

    /// <summary>Starts a monadic chain from parsed query parameters using the registered map.</summary>
    /// <param name="provider">The query map provider composed for the application.</param>
    /// <param name="parameters">The raw query parameters.</param>
    /// <returns>A successful chain carrying the builder, or the parse/map failure.</returns>
    public static Result<QueryBuilder<T>> FromParametersResult(
        IQueryMapProvider provider,
        QueryParameters parameters) =>
        FromParameters(provider, parameters).AsResult();

    /// <summary>Exposes the builder's current state as a monadic chain.</summary>
    /// <returns>A successful chain carrying this builder, or the state's failure.</returns>
    public Result<QueryBuilder<T>> AsResult() =>
        _state.IsSuccess
            ? Result<QueryBuilder<T>>.Ok(this)
            : Result<QueryBuilder<T>>.Fail(_state.Errors, _state.StatusCode);

    // ---- Filters -------------------------------------------------------

    /// <summary>Adds a filter parsed from text, combined with AND.</summary>
    /// <param name="filter">Filter text such as <c>active=true</c>; empty text is a no-op.</param>
    /// <returns>A new builder whose state carries the combined filter, or a failure.</returns>
    public QueryBuilder<T> Where(string filter) =>
        ApplyCore(_ => ParseFilter(filter));

    /// <summary>Adds a filter parsed from text, combined with OR.</summary>
    /// <param name="filter">Filter text; empty text is a no-op.</param>
    /// <returns>A new builder whose state carries the combined filter, or a failure.</returns>
    public QueryBuilder<T> OrWhere(string filter) =>
        ApplyCore(_ => ParseFilter(filter, FilterLogic.Or));

    /// <summary>Adds a typed filter node, combined with AND.</summary>
    /// <param name="filter">The filter tree to add.</param>
    /// <returns>
    /// A new builder whose state carries the combined filter, or a failure when
    /// a field is unmapped or not filterable.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <c>null</c>.</exception>
    public QueryBuilder<T> Where(FilterNode filter) => ApplyFilter(filter, FilterLogic.And);

    /// <summary>Adds a typed filter node, combined with OR.</summary>
    /// <param name="filter">The filter tree to add.</param>
    /// <returns>
    /// A new builder whose state carries the combined filter, or a failure when
    /// a field is unmapped or not filterable.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <c>null</c>.</exception>
    public QueryBuilder<T> OrWhere(FilterNode filter) => ApplyFilter(filter, FilterLogic.Or);

    /// <summary>Adds a predicate translated through the map, combined with AND.</summary>
    /// <param name="predicate">The predicate to translate.</param>
    /// <returns>A new builder whose state carries the combined filter, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <c>null</c>.</exception>
    public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return ApplyCore(current =>
            AddExpressionFilter(current, predicate, FilterLogic.And));
    }

    /// <summary>Adds a predicate translated through the map, combined with OR.</summary>
    /// <param name="predicate">The predicate to translate.</param>
    /// <returns>A new builder whose state carries the combined filter, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <c>null</c>.</exception>
    public QueryBuilder<T> OrWhere(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return ApplyCore(current =>
            AddExpressionFilter(current, predicate, FilterLogic.Or));
    }

    /// <summary>Adds a filter parsed from text as a monadic chain, combined with AND.</summary>
    /// <param name="filter">Filter text; empty text is a no-op.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public Result<QueryBuilder<T>> TryWhere(string filter) =>
        TryApply(_ => ParseFilter(filter));

    /// <summary>Adds a filter parsed from text as a monadic chain, combined with OR.</summary>
    /// <param name="filter">Filter text; empty text is a no-op.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public Result<QueryBuilder<T>> TryOrWhere(string filter) =>
        TryApply(_ => ParseFilter(filter, FilterLogic.Or));

    /// <summary>Adds a typed filter node as a monadic chain, combined with AND.</summary>
    /// <param name="filter">The filter tree to add.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryWhere(FilterNode filter) => TryFilter(filter, FilterLogic.And);

    /// <summary>Adds a typed filter node as a monadic chain, combined with OR.</summary>
    /// <param name="filter">The filter tree to add.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryOrWhere(FilterNode filter) => TryFilter(filter, FilterLogic.Or);

    /// <summary>Adds a predicate as a monadic chain, combined with AND.</summary>
    /// <param name="predicate">The predicate to translate.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryWhere(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return TryApply(current =>
            AddExpressionFilter(current, predicate, FilterLogic.And));
    }

    /// <summary>Adds a predicate as a monadic chain, combined with OR.</summary>
    /// <param name="predicate">The predicate to translate.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryOrWhere(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return TryApply(current =>
            AddExpressionFilter(current, predicate, FilterLogic.Or));
    }

    // ---- Scope ---------------------------------------------------------

    /// <summary>Adds a structural scope that every build applies around the query filter.</summary>
    /// <param name="scope">The scope filter to AND with the query filter at build time.</param>
    /// <returns>
    /// A new builder whose scope is the existing scope AND the supplied scope, or a failed
    /// builder when the scope names an unmapped or non-filterable field.
    /// </returns>
    /// <remarks>
    /// A scope is deliberately separate from the query filter: a later <c>OrWhere</c> cannot
    /// widen the query past it because every build combines the two with a top-level AND.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="scope"/> is <c>null</c>.</exception>
    public QueryBuilder<T> ApplyScope(FilterNode scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        // Preserve: a failed state keeps its original errors instead of validating a new scope.
        if (_state.IsFailure)
            return this;

        Result validation = ValidateFilter(scope);
        if (validation.IsFailure)
            return With(
                _map,
                Result<Query<T>>.Fail(validation.Errors, validation.StatusCode),
                _projection,
                _scope);

        FilterNode combined = _scope is null
            ? scope
            : new FilterGroup(FilterLogic.And, [_scope, scope]);

        return With(_map, _state, _projection, combined);
    }

    // ---- Monadic hooks -------------------------------------------------

    /// <summary>Chains a state transformation.</summary>
    /// <param name="binder">The transformation applied to the current query.</param>
    /// <returns>A new builder carrying the transformed state, or a failed builder.</returns>
    public QueryBuilder<T> Bind(Func<Query<T>, Result<Query<T>>> binder) => ApplyCore(binder);

    /// <summary>Requires the current query to satisfy a predicate.</summary>
    /// <param name="predicate">The invariant the query must satisfy.</param>
    /// <param name="error">The error reported when the invariant does not hold.</param>
    /// <returns>The builder unchanged when the invariant holds; otherwise a failed builder.</returns>
    public QueryBuilder<T> Ensure(Func<Query<T>, bool> predicate, Error error) =>
        ApplyCore(current => Result<Query<T>>.Ok(current).Ensure(predicate, error));

    /// <summary>Chains a state transformation as a monadic chain.</summary>
    /// <param name="binder">The transformation applied to the current query.</param>
    /// <returns>A successful chain carrying the new builder, or the transformation failure.</returns>
    public Result<QueryBuilder<T>> TryBind(Func<Query<T>, Result<Query<T>>> binder) =>
        TryApply(binder);

    /// <summary>Requires the current query to satisfy a predicate as a monadic chain.</summary>
    /// <param name="predicate">The invariant the query must satisfy.</param>
    /// <param name="error">The error reported when the invariant does not hold.</param>
    /// <returns>A successful chain carrying the builder, or the invariant failure.</returns>
    public Result<QueryBuilder<T>> TryEnsure(Func<Query<T>, bool> predicate, Error error) =>
        TryApply(current => Result<Query<T>>.Ok(current).Ensure(predicate, error));

    // ---- Build ---------------------------------------------------------

    /// <summary>Builds the composed query.</summary>
    /// <returns>The query with its scope applied, or the first failure accumulated by the chain.</returns>
    public Result<Query<T>> Build() =>
        _state.IsSuccess ? ApplyScopeTo(_state.Value) : _state;

    /// <summary>Builds the composed query, requiring an entity shape.</summary>
    /// <param name="whenTabular">The error reported when the query is tabular; a default applies when omitted.</param>
    /// <returns>The entity query, or a failure when the state failed or the shape is not Entity.</returns>
    public Result<Query<T>> BuildEntity(Error? whenTabular = null) =>
        Build().Ensure(
            query => !query.IsTabular,
            whenTabular ?? QueryOutcome.Failure.EntityShapeRequired);

    // ---- Helpers -------------------------------------------------------

    private static QueryBuilder<T> With(
        QueryMap<T>? map,
        Result<Query<T>> state,
        LambdaExpression? projection,
        FilterNode? scope) =>
        new(map, state, projection, scope);

    private Result<Query<T>> ApplyScopeTo(Query<T> query)
    {
        if (_scope is null)
            return Result<Query<T>>.Ok(query);

        FilterNode combined = query.Filter is null
            ? _scope
            : new FilterGroup(FilterLogic.And, [query.Filter, _scope]);

        return Result<Query<T>>.Ok(query with { Filter = combined });
    }

    private QueryBuilder<T> ApplyCore(Func<Query<T>, Result<Query<T>>> transform) =>
        _state.IsSuccess
            ? With(_map, _state.Bind(transform), _projection, _scope)
            : this;

    private Result<QueryBuilder<T>> TryApply(Func<Query<T>, Result<Query<T>>> transform)
    {
        if (_state.IsFailure)
            return Result<QueryBuilder<T>>.Fail(_state.Errors, _state.StatusCode);

        Result<Query<T>> next = _state.Bind(transform);
        return next.IsSuccess
            ? Result<QueryBuilder<T>>.Ok(With(_map, next, _projection, _scope))
            : Result<QueryBuilder<T>>.Fail(next.Errors, next.StatusCode);
    }

    private QueryBuilder<T> ApplyFilter(FilterNode filter, FilterLogic logic)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // Validate: validation runs inside the transform so a failed state is never
        // re-validated and keeps its original errors.
        return ApplyCore(current => AddValidatedFilter(current, filter, logic));
    }

    private Result<QueryBuilder<T>> TryFilter(FilterNode filter, FilterLogic logic)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return TryApply(current => AddValidatedFilter(current, filter, logic));
    }

    private Result<Query<T>> AddValidatedFilter(
        Query<T> current,
        FilterNode filter,
        FilterLogic logic)
    {
        Result validation = ValidateFilter(filter);
        return validation.IsFailure
            ? Result<Query<T>>.Fail(validation.Errors, validation.StatusCode)
            : AddFilter(current, filter, logic);
    }

    private static Result<Query<T>> AddFilter(
        Query<T> current,
        FilterNode filter,
        FilterLogic logic)
    {
        FilterNode combined = current.Filter is null
            ? filter
            : new FilterGroup(logic, [current.Filter, filter]);

        return Result<Query<T>>.Ok(current with { Filter = combined });
    }

    private Result<Query<T>> ParseFilter(string text) => ParseFilter(text, FilterLogic.And);

    private Result<Query<T>> ParseFilter(string text, FilterLogic logic)
    {
        Result<Query<T>> parsed = QueryParser.Parse<T>(
            new QueryParameters { Filter = text },
            _map!);

        if (parsed.IsFailure)
            return Result<Query<T>>.Fail(parsed.Errors, parsed.StatusCode);

        FilterNode? filter = parsed.Value.Filter;
        return filter is null
            ? Result<Query<T>>.Ok(_state.Value)
            : AddFilter(_state.Value, filter, logic);
    }

    private Result ValidateFilter(FilterNode node) => node switch
    {
        FilterCondition condition => ValidateCondition(condition),
        FilterGroup group => ValidateGroup(group),
        _ => Result.Fail(QueryOutcome.Failure.ExpressionUnsupported(
            $"Unsupported filter node '{node.GetType().Name}'.")),
    };

    private Result ValidateCondition(FilterCondition condition)
    {
        if (_map is null || !_map.TryGet(condition.Field, out QueryField? field))
            return Result.Fail(QueryOutcome.Failure.NotMapped(condition.Field));

        return field.Filterable
            ? Result.Success()
            : Result.Fail(QueryOutcome.Failure.NotFilterable(condition.Field));
    }

    private Result ValidateGroup(FilterGroup group)
    {
        foreach (FilterNode child in group.Children)
        {
            Result validation = ValidateFilter(child);
            if (validation.IsFailure)
                return validation;
        }

        return Result.Success();
    }

    private Result<Query<T>> AddExpressionFilter(
        Query<T> current,
        Expression<Func<T, bool>> predicate,
        FilterLogic logic)
    {
        Result<FilterNode> translated = QueryExpressionTranslator<T>
            .TranslatePredicate(predicate, _map!);

        return translated.IsFailure
            ? Result<Query<T>>.Fail(translated.Errors, translated.StatusCode)
            : AddFilter(current, translated.Value, logic);
    }
}