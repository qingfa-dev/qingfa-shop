using System.Linq.Expressions;

using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Expressions;
using Application.BuildingBlocks.Querying.Grouping;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Projection;

namespace Application.BuildingBlocks.Querying;

/// <summary>Composes projections, grouping, aggregation, and typed shape builds.</summary>
/// <remarks>The type parameter is documented on the core partial declaration.</remarks>
public sealed partial class QueryBuilder<T>
    where T : class
{
    // ---- Select --------------------------------------------------------

    /// <summary>Replaces the projection with one parsed from text.</summary>
    /// <param name="select">Projection text such as <c>name,score</c>; empty text clears the projection.</param>
    /// <returns>A new builder whose state carries the parsed columns, or a failure.</returns>
    public QueryBuilder<T> Select(string select) =>
        ApplyCore(current => ReplaceSelect(current, select));

    /// <summary>Replaces the projection with members accessed through the map.</summary>
    /// <param name="selectors">The members to project, in column order.</param>
    /// <returns>A new builder whose state carries the translated columns, or a failure.</returns>
    /// <exception cref="ArgumentNullException">A selector is <c>null</c>.</exception>
    public QueryBuilder<T> Select(params Expression<Func<T, object?>>[] selectors)
    {
        ArgumentNullException.ThrowIfNull(selectors);
        return ApplyCore(current =>
            ReplaceSelect(
                current,
                ValidateSelect(
                    TranslateSelect(selectors),
                    current.Limits.MaxSelectFields)));
    }

    /// <summary>Replaces the projection with typed select fields.</summary>
    /// <param name="fields">The projection columns to use.</param>
    /// <returns>A new builder whose state carries the columns, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <c>null</c>.</exception>
    public QueryBuilder<T> Select(IReadOnlyList<SelectField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return ApplyCore(current =>
            ReplaceSelect(current, ValidateSelect(fields, current.Limits.MaxSelectFields)));
    }

    /// <summary>Appends projection text to the current projection.</summary>
    /// <param name="select">Projection text such as <c>name,score</c>; empty text appends nothing.</param>
    /// <returns>A new builder whose state carries the appended columns, or a failure.</returns>
    public QueryBuilder<T> AddSelect(string select) =>
        ApplyCore(current => AppendSelect(current, select));

    /// <summary>Appends members accessed through the map to the current projection.</summary>
    /// <param name="selectors">The members to append, in column order.</param>
    /// <returns>A new builder whose state carries the appended columns, or a failure.</returns>
    /// <exception cref="ArgumentNullException">A selector is <c>null</c>.</exception>
    public QueryBuilder<T> AddSelect(params Expression<Func<T, object?>>[] selectors)
    {
        ArgumentNullException.ThrowIfNull(selectors);
        return ApplyCore(current =>
            AppendSelect(current, ValidateSelect(TranslateSelect(selectors), current.Limits.MaxSelectFields)));
    }

    /// <summary>Appends typed select fields to the current projection.</summary>
    /// <param name="fields">The projection columns to append.</param>
    /// <returns>A new builder whose state carries the appended columns, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <c>null</c>.</exception>
    public QueryBuilder<T> AddSelect(IReadOnlyList<SelectField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return ApplyCore(current => AppendSelect(current, ValidateSelect(fields, current.Limits.MaxSelectFields)));
    }

    // ---- Projection ----------------------------------------------------

    /// <summary>Stores an entity-to-response projection for a later projected build.</summary>
    /// <typeparam name="TResponse">The projected response type.</typeparam>
    /// <param name="projection">The projection expression.</param>
    /// <returns>A new builder that carries the projection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="projection"/> is <c>null</c>.</exception>
    public QueryBuilder<T> Project<TResponse>(Expression<Func<T, TResponse>> projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return With(_map, _state, projection, _scope);
    }

    // ---- Grouping ------------------------------------------------------

    /// <summary>Replaces the grouping keys with those parsed from text.</summary>
    /// <param name="groupBy">Grouping text such as <c>active,name</c>; empty text clears the grouping.</param>
    /// <returns>A new builder whose state carries the parsed keys, or a failure.</returns>
    public QueryBuilder<T> GroupBy(string groupBy) =>
        ApplyCore(current => ReplaceGroup(current, groupBy));

    /// <summary>Replaces the grouping keys with members accessed through the map.</summary>
    /// <param name="selector">The member or anonymous tuple of members to group by.</param>
    /// <returns>A new builder whose state carries the translated keys, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <c>null</c>.</exception>
    public QueryBuilder<T> GroupBy(Expression<Func<T, object?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return ApplyCore(current => ReplaceGroup(current, TranslateGroup(selector)));
    }

    /// <summary>Appends grouping keys parsed from text.</summary>
    /// <param name="groupBy">Grouping text such as <c>active,name</c>; empty text appends nothing.</param>
    /// <returns>A new builder whose state carries the appended keys, or a failure.</returns>
    public QueryBuilder<T> AddGroupBy(string groupBy) =>
        ApplyCore(current => AppendGroup(current, groupBy));

    // ---- Aggregation ---------------------------------------------------

    /// <summary>Appends an aggregate computed over a member accessed through the map.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="function">The aggregate function to apply.</param>
    /// <param name="field">The member to aggregate.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A new builder whose state carries the appended aggregate, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
    public QueryBuilder<T> Aggregate<TField>(
        AggregateFunction function,
        Expression<Func<T, TField>> field,
        string? alias = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        return ApplyCore(current => AppendAggregate(current, function, field, alias));
    }

    /// <summary>Appends aggregate expressions parsed from text.</summary>
    /// <param name="aggregate">Aggregate text such as <c>count(score),sum(score)</c>.</param>
    /// <returns>A new builder whose state carries the appended aggregates, or a failure.</returns>
    public QueryBuilder<T> Aggregate(string aggregate) =>
        ApplyCore(current => AppendAggregate(current, aggregate));

    /// <summary>Appends a count aggregate over a member.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member to count.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A new builder whose state carries the appended aggregate, or a failure.</returns>
    public QueryBuilder<T> Count<TField>(Expression<Func<T, TField>> field, string? alias = null) =>
        Aggregate(AggregateFunction.Count, field, alias);

    /// <summary>Appends a sum aggregate over a member.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member to sum.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A new builder whose state carries the appended aggregate, or a failure.</returns>
    public QueryBuilder<T> Sum<TField>(Expression<Func<T, TField>> field, string? alias = null) =>
        Aggregate(AggregateFunction.Sum, field, alias);

    /// <summary>Appends an average aggregate over a member.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member to average.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A new builder whose state carries the appended aggregate, or a failure.</returns>
    public QueryBuilder<T> Average<TField>(Expression<Func<T, TField>> field, string? alias = null) =>
        Aggregate(AggregateFunction.Average, field, alias);

    /// <summary>Appends a minimum aggregate over a member.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member to minimize.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A new builder whose state carries the appended aggregate, or a failure.</returns>
    public QueryBuilder<T> Min<TField>(Expression<Func<T, TField>> field, string? alias = null) =>
        Aggregate(AggregateFunction.Minimum, field, alias);

    /// <summary>Appends a maximum aggregate over a member.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member to maximize.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A new builder whose state carries the appended aggregate, or a failure.</returns>
    public QueryBuilder<T> Max<TField>(Expression<Func<T, TField>> field, string? alias = null) =>
        Aggregate(AggregateFunction.Maximum, field, alias);

    /// <summary>Appends a distinct-count aggregate over a member.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member whose distinct values are counted.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A new builder whose state carries the appended aggregate, or a failure.</returns>
    public QueryBuilder<T> DistinctCount<TField>(
        Expression<Func<T, TField>> field,
        string? alias = null) =>
        Aggregate(AggregateFunction.DistinctCount, field, alias);

    // ---- Monadic projection --------------------------------------------

    /// <summary>Replaces the projection with one parsed from text, as a monadic chain.</summary>
    /// <param name="select">Projection text such as <c>name,score</c>; empty text clears the projection.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public Result<QueryBuilder<T>> TrySelect(string select) =>
        TryApply(current => ReplaceSelect(current, select));

    /// <summary>Replaces the projection with members accessed through the map, as a monadic chain.</summary>
    /// <param name="selectors">The members to project, in column order.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException">A selector is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TrySelect(params Expression<Func<T, object?>>[] selectors)
    {
        ArgumentNullException.ThrowIfNull(selectors);
        return TryApply(current =>
            ReplaceSelect(current, ValidateSelect(TranslateSelect(selectors), current.Limits.MaxSelectFields)));
    }

    /// <summary>Replaces the projection with typed select fields, as a monadic chain.</summary>
    /// <param name="fields">The projection columns to use.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TrySelect(IReadOnlyList<SelectField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return TryApply(current => ReplaceSelect(current, ValidateSelect(fields, current.Limits.MaxSelectFields)));
    }

    /// <summary>Appends projection text to the current projection, as a monadic chain.</summary>
    /// <param name="select">Projection text such as <c>name,score</c>; empty text appends nothing.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public Result<QueryBuilder<T>> TryAddSelect(string select) =>
        TryApply(current => AppendSelect(current, select));

    /// <summary>Appends members accessed through the map to the projection, as a monadic chain.</summary>
    /// <param name="selectors">The members to append, in column order.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException">A selector is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryAddSelect(params Expression<Func<T, object?>>[] selectors)
    {
        ArgumentNullException.ThrowIfNull(selectors);
        return TryApply(current =>
            AppendSelect(current, ValidateSelect(TranslateSelect(selectors), current.Limits.MaxSelectFields)));
    }

    /// <summary>Appends typed select fields to the projection, as a monadic chain.</summary>
    /// <param name="fields">The projection columns to append.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryAddSelect(IReadOnlyList<SelectField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return TryApply(current => AppendSelect(current, ValidateSelect(fields, current.Limits.MaxSelectFields)));
    }

    /// <summary>Stores an entity-to-response projection, as a monadic chain.</summary>
    /// <typeparam name="TResponse">The projected response type.</typeparam>
    /// <param name="projection">The projection expression.</param>
    /// <returns>A successful chain carrying the new builder, or the state's failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="projection"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryProject<TResponse>(
        Expression<Func<T, TResponse>> projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return AsResult().Map(builder => builder.Project(projection));
    }

    /// <summary>Replaces the grouping keys with those parsed from text, as a monadic chain.</summary>
    /// <param name="groupBy">Grouping text such as <c>active,name</c>; empty text clears the grouping.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public Result<QueryBuilder<T>> TryGroupBy(string groupBy) =>
        TryApply(current => ReplaceGroup(current, groupBy));

    /// <summary>Replaces the grouping keys with members, as a monadic chain.</summary>
    /// <param name="selector">The member or anonymous tuple of members to group by.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryGroupBy(Expression<Func<T, object?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return TryApply(current => ReplaceGroup(current, TranslateGroup(selector)));
    }

    /// <summary>Appends grouping keys parsed from text, as a monadic chain.</summary>
    /// <param name="groupBy">Grouping text such as <c>active,name</c>; empty text appends nothing.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public Result<QueryBuilder<T>> TryAddGroupBy(string groupBy) =>
        TryApply(current => AppendGroup(current, groupBy));

    /// <summary>Appends an aggregate over a member, as a monadic chain.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="function">The aggregate function to apply.</param>
    /// <param name="field">The member to aggregate.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryAggregate<TField>(
        AggregateFunction function,
        Expression<Func<T, TField>> field,
        string? alias = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        return TryApply(current => AppendAggregate(current, function, field, alias));
    }

    /// <summary>Appends aggregate expressions parsed from text, as a monadic chain.</summary>
    /// <param name="aggregate">Aggregate text such as <c>count(score),sum(score)</c>.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public Result<QueryBuilder<T>> TryAggregate(string aggregate) =>
        TryApply(current => AppendAggregate(current, aggregate));

    /// <summary>Appends a count aggregate over a member, as a monadic chain.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member to count.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public Result<QueryBuilder<T>> TryCount<TField>(
        Expression<Func<T, TField>> field,
        string? alias = null) =>
        TryAggregate(AggregateFunction.Count, field, alias);

    /// <summary>Appends a sum aggregate over a member, as a monadic chain.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member to sum.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public Result<QueryBuilder<T>> TrySum<TField>(
        Expression<Func<T, TField>> field,
        string? alias = null) =>
        TryAggregate(AggregateFunction.Sum, field, alias);

    /// <summary>Appends an average aggregate over a member, as a monadic chain.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member to average.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public Result<QueryBuilder<T>> TryAverage<TField>(
        Expression<Func<T, TField>> field,
        string? alias = null) =>
        TryAggregate(AggregateFunction.Average, field, alias);

    /// <summary>Appends a minimum aggregate over a member, as a monadic chain.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member to minimize.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public Result<QueryBuilder<T>> TryMin<TField>(
        Expression<Func<T, TField>> field,
        string? alias = null) =>
        TryAggregate(AggregateFunction.Minimum, field, alias);

    /// <summary>Appends a maximum aggregate over a member, as a monadic chain.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member to maximize.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public Result<QueryBuilder<T>> TryMax<TField>(
        Expression<Func<T, TField>> field,
        string? alias = null) =>
        TryAggregate(AggregateFunction.Maximum, field, alias);

    /// <summary>Appends a distinct-count aggregate over a member, as a monadic chain.</summary>
    /// <typeparam name="TField">The aggregated member type.</typeparam>
    /// <param name="field">The member whose distinct values are counted.</param>
    /// <param name="alias">The result column alias; a canonical default applies when omitted.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    public Result<QueryBuilder<T>> TryDistinctCount<TField>(
        Expression<Func<T, TField>> field,
        string? alias = null) =>
        TryAggregate(AggregateFunction.DistinctCount, field, alias);

    // ---- Shape builds --------------------------------------------------

    /// <summary>Builds the composed query as a typed projection.</summary>
    /// <typeparam name="TResponse">The projected response type.</typeparam>
    /// <returns>
    /// The projected query, or a failure when no projection was stored, the stored
    /// projection has a different response type, or the state is tabular.
    /// </returns>
    public Result<ProjectedQuery<T, TResponse>> BuildProjected<TResponse>()
    {
        // Preserve: an existing failed state outranks a missing or mismatched projection.
        if (_state.IsFailure)
            return Result<ProjectedQuery<T, TResponse>>.Fail(
                _state.Errors,
                _state.StatusCode);

        // Validate: a projected query must carry a projection expression.
        if (_projection is null)
            return Result<ProjectedQuery<T, TResponse>>.Fail(
                QueryOutcome.Failure.ProjectionRequired);

        // Validate: the stored expression must produce the requested response type.
        if (_projection is not Expression<Func<T, TResponse>> projection)
            return Result<ProjectedQuery<T, TResponse>>.Fail(
                QueryOutcome.Failure.ProjectionMismatch);

        return Build()
            .Ensure(query => !query.IsTabular, QueryOutcome.Failure.EntityShapeRequired)
            .Map(query => new ProjectedQuery<T, TResponse>(query, projection));
    }

    /// <summary>Builds the composed query as a tabular result.</summary>
    /// <param name="whenEntity">The error reported when the query is an entity query; a default applies when omitted.</param>
    /// <returns>
    /// The tabular query, or a failure when a projection is stored, the state failed,
    /// the shape is not tabular, or the select/group/aggregate combination is invalid.
    /// </returns>
    public Result<Query<T, TabularResult>> BuildTabular(Error? whenEntity = null)
    {
        // Validate: a stored entity projection and a tabular shape are mutually exclusive.
        if (_projection is not null)
            return Result<Query<T, TabularResult>>.Fail(
                QueryOutcome.Failure.ShapeConflict(
                    "a stored projection cannot be built as tabular data."));

        return Build()
            .Ensure(
                query => query.IsTabular,
                whenEntity ?? QueryOutcome.Failure.TabularShapeRequired)
            .Bind(query => ValidateTabularShape(query)
                .Map(() => new Query<T, TabularResult>(query)));
    }

    // ---- Helpers -------------------------------------------------------

    private static Result ValidateTabularShape(Query<T> query)
    {
        // Enforce: in SQL, mixing SELECT columns with aggregates requires GROUP BY.
        if (query.Select is not null &&
            query.Aggregate is not null &&
            query.GroupBy is null)
        {
            return Result.Fail(QueryOutcome.Failure.MixedShape);
        }

        // Validate: every selected field must appear in the group-by keys,
        // mirroring the SQL rule that non-aggregated columns must be grouped.
        if (query.GroupBy is not null && query.Select is not null)
        {
            foreach (SelectField selected in query.Select)
            {
                if (!query.GroupBy.Any(key =>
                    string.Equals(key.Field, selected.Field, StringComparison.OrdinalIgnoreCase)))
                {
                    return Result.Fail(QueryOutcome.Failure.FieldNotGrouped(selected.Field));
                }
            }
        }

        return Result.Success();
    }

    private Result<Query<T>> ReplaceSelect(Query<T> current, string select)
    {
        Result<Query<T>> parsed = ParsePart(current, new QueryParameters { Select = select });

        return parsed.IsFailure
            ? Result<Query<T>>.Fail(parsed.Errors, parsed.StatusCode)
            : Result<Query<T>>.Ok(current with { Select = parsed.Value.Select });
    }

    private static Result<Query<T>> ReplaceSelect(
        Query<T> current,
        Result<IReadOnlyList<SelectField>> translated) =>
        translated.IsFailure
            ? Result<Query<T>>.Fail(translated.Errors, translated.StatusCode)
            : Result<Query<T>>.Ok(current with
            {
                // Normalize: an empty projection clears the selection, matching the text path.
                Select = translated.Value.Count == 0 ? null : translated.Value,
            });

    private Result<Query<T>> AppendSelect(Query<T> current, string select)
    {
        Result<Query<T>> parsed = ParsePart(current, new QueryParameters { Select = select });

        return parsed.IsFailure
            ? Result<Query<T>>.Fail(parsed.Errors, parsed.StatusCode)
            : AppendSelect(current, parsed.Value.Select);
    }

    private Result<Query<T>> AppendSelect(
        Query<T> current,
        Result<IReadOnlyList<SelectField>> translated) =>
        translated.IsFailure
            ? Result<Query<T>>.Fail(translated.Errors, translated.StatusCode)
            : AppendSelect(current, translated.Value);

    private Result<Query<T>> AppendSelect(
        Query<T> current,
        IReadOnlyList<SelectField>? fields)
    {
        if (fields is null || fields.Count == 0)
            return Result<Query<T>>.Ok(current);

        IReadOnlyList<SelectField> merged = [.. (current.Select ?? []), .. fields];
        Result<IReadOnlyList<SelectField>> validated =
            ValidateSelect(merged, current.Limits.MaxSelectFields);

        return validated.IsFailure
            ? Result<Query<T>>.Fail(validated.Errors, validated.StatusCode)
            : Result<Query<T>>.Ok(current with { Select = validated.Value });
    }

    private Result<Query<T>> ReplaceGroup(Query<T> current, string groupBy)
    {
        Result<Query<T>> parsed = ParsePart(current, new QueryParameters { GroupBy = groupBy });

        return parsed.IsFailure
            ? Result<Query<T>>.Fail(parsed.Errors, parsed.StatusCode)
            : Result<Query<T>>.Ok(current with { GroupBy = parsed.Value.GroupBy });
    }

    private Result<Query<T>> ReplaceGroup(
        Query<T> current,
        Result<IReadOnlyList<GroupByField>> translated) =>
        translated.IsFailure
            ? Result<Query<T>>.Fail(translated.Errors, translated.StatusCode)
            : Result<Query<T>>.Ok(current with { GroupBy = translated.Value });

    private Result<Query<T>> AppendGroup(Query<T> current, string groupBy)
    {
        Result<Query<T>> parsed = ParsePart(current, new QueryParameters { GroupBy = groupBy });

        return parsed.IsFailure
            ? Result<Query<T>>.Fail(parsed.Errors, parsed.StatusCode)
            : AppendGroup(current, parsed.Value.GroupBy);
    }

    private Result<Query<T>> AppendGroup(
        Query<T> current,
        IReadOnlyList<GroupByField>? keys)
    {
        if (keys is null || keys.Count == 0)
            return Result<Query<T>>.Ok(current);

        IReadOnlyList<GroupByField> merged = [.. (current.GroupBy ?? []), .. keys];
        Result<IReadOnlyList<GroupByField>> validated =
            ValidateGroup(merged, current.Limits.MaxGroupFields);

        return validated.IsFailure
            ? Result<Query<T>>.Fail(validated.Errors, validated.StatusCode)
            : Result<Query<T>>.Ok(current with { GroupBy = validated.Value });
    }

    private Result<Query<T>> AppendAggregate(Query<T> current, string aggregate)
    {
        Result<Query<T>> parsed = ParsePart(
            current,
            new QueryParameters { Aggregate = aggregate });

        return parsed.IsFailure
            ? Result<Query<T>>.Fail(parsed.Errors, parsed.StatusCode)
            : AppendAggregate(current, parsed.Value.Aggregate);
    }

    private Result<Query<T>> AppendAggregate<TField>(
        Query<T> current,
        AggregateFunction function,
        Expression<Func<T, TField>> field,
        string? alias)
    {
        Result<AggregateSpec> translated = QueryExpressionTranslator<T>
            .TranslateAggregate(function, field, alias, _map!);

        return translated.IsFailure
            ? Result<Query<T>>.Fail(translated.Errors, translated.StatusCode)
            : AppendAggregate(current, [translated.Value]);
    }

    private Result<Query<T>> AppendAggregate(
        Query<T> current,
        IReadOnlyList<AggregateSpec>? aggregates)
    {
        if (aggregates is null || aggregates.Count == 0)
            return Result<Query<T>>.Ok(current);

        IReadOnlyList<AggregateSpec> merged = [.. (current.Aggregate ?? []), .. aggregates];
        Result<IReadOnlyList<AggregateSpec>> validated =
            ValidateAggregate(merged, current.Limits.MaxAggregates);

        return validated.IsFailure
            ? Result<Query<T>>.Fail(validated.Errors, validated.StatusCode)
            : Result<Query<T>>.Ok(current with { Aggregate = validated.Value });
    }

    private Result<IReadOnlyList<SelectField>> TranslateSelect(
        IReadOnlyList<Expression<Func<T, object?>>> selectors)
    {
        var fields = new List<SelectField>(selectors.Count);

        foreach (Expression<Func<T, object?>> selector in selectors)
        {
            ArgumentNullException.ThrowIfNull(selector);
            Result<SelectField> translated = QueryExpressionTranslator<T>
                .TranslateSelect(selector, _map!);

            if (translated.IsFailure)
                return Result<IReadOnlyList<SelectField>>.Fail(
                    translated.Errors,
                    translated.StatusCode);

            fields.Add(translated.Value);
        }

        return Result<IReadOnlyList<SelectField>>.Ok(fields);
    }

    private Result<IReadOnlyList<GroupByField>> TranslateGroup(
        Expression<Func<T, object?>> selector) =>
        QueryExpressionTranslator<T>.TranslateGroup(selector, _map!);

    private Result<IReadOnlyList<SelectField>> ValidateSelect(
        IReadOnlyList<SelectField> fields,
        int maxFields)
    {
        // Validate: the merged projection must respect the same column limit as parsed text.
        if (fields.Count > maxFields)
            return Result<IReadOnlyList<SelectField>>.Fail(
                QueryOutcome.Failure.Syntax($"select cannot exceed {maxFields} columns"));

        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (SelectField? field in fields)
        {
            // Validate: typed field lists are assembled by callers, so a null entry
            // is a query failure rather than an exception.
            if (field is null)
                return Result<IReadOnlyList<SelectField>>.Fail(
                    QueryOutcome.Failure.ExpressionUnsupported(
                        "A select field cannot be null."));

            if (string.IsNullOrWhiteSpace(field.Field))
                return Result<IReadOnlyList<SelectField>>.Fail(
                    QueryOutcome.Failure.NotMapped(field.Field));

            // Validate: every named field must exist and be projectable.
            if (_map is null || !_map.TryGet(field.Field, out QueryField? mapped))
                return Result<IReadOnlyList<SelectField>>.Fail(
                    QueryOutcome.Failure.NotMapped(field.Field));

            if (!mapped.Selectable)
                return Result<IReadOnlyList<SelectField>>.Fail(
                    QueryOutcome.Failure.NotSelectable(field.Field));

            // Validate: an alias is required so the result set has a column name.
            if (string.IsNullOrWhiteSpace(field.Alias))
                return Result<IReadOnlyList<SelectField>>.Fail(
                    QueryOutcome.Failure.ExpressionUnsupported(
                        $"A select alias is required for field '{field.Field}'."));

            // Validate: one projection may not repeat a field or an alias,
            // including fields added by an earlier call.
            if (!fieldNames.Add(field.Field))
                return Result<IReadOnlyList<SelectField>>.Fail(
                    QueryOutcome.Failure.Duplicate(field.Field));

            if (!aliases.Add(field.Alias))
                return Result<IReadOnlyList<SelectField>>.Fail(
                    QueryOutcome.Failure.Duplicate(field.Alias));
        }

        return Result<IReadOnlyList<SelectField>>.Ok(fields);
    }

    private Result<IReadOnlyList<SelectField>> ValidateSelect(
        Result<IReadOnlyList<SelectField>> translated,
        int maxFields) =>
        translated.IsFailure
            ? translated
            : ValidateSelect(translated.Value, maxFields);

    private Result<IReadOnlyList<GroupByField>> ValidateGroup(
        IReadOnlyList<GroupByField> keys,
        int maxFields)
    {
        if (keys.Count > maxFields)
            return Result<IReadOnlyList<GroupByField>>.Fail(
                QueryOutcome.Failure.Syntax($"group by cannot exceed {maxFields} fields"));

        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (GroupByField key in keys)
        {
            if (!fieldNames.Add(key.Field))
                return Result<IReadOnlyList<GroupByField>>.Fail(
                    QueryOutcome.Failure.Duplicate(key.Field));
        }

        return Result<IReadOnlyList<GroupByField>>.Ok(keys);
    }

    private static Result<IReadOnlyList<AggregateSpec>> ValidateAggregate(
        IReadOnlyList<AggregateSpec> aggregates,
        int maxAggregates)
    {
        if (aggregates.Count > maxAggregates)
            return Result<IReadOnlyList<AggregateSpec>>.Fail(
                QueryOutcome.Failure.AggregateSyntax(
                    $"cannot exceed {maxAggregates} aggregates"));

        // Validate: result columns must have unique aliases, including aliases
        // introduced by an earlier call.
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AggregateSpec aggregate in aggregates)
        {
            if (!aliases.Add(aggregate.Alias))
                return Result<IReadOnlyList<AggregateSpec>>.Fail(
                    QueryOutcome.Failure.Duplicate(aggregate.Alias));
        }

        return Result<IReadOnlyList<AggregateSpec>>.Ok(aggregates);
    }

    private Result<Query<T>> ParsePart(Query<T> current, QueryParameters parameters) =>
        QueryParser.Parse<T>(parameters, _map!, current.Limits);
}