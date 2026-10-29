using System.Linq.Expressions;

using Application.Common.Querying.Expressions;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Searching;
using Application.Common.Querying.Sorting;

namespace Application.Common.Querying;

/// <summary>Composes ordering, paging, search, and execution options.</summary>
/// <remarks>The type parameter is documented on the core partial declaration.</remarks>
public sealed partial class QueryBuilder<T>
    where T : class
{
    // ---- Ordering ------------------------------------------------------

    /// <summary>Replaces the ordering with one parsed from text.</summary>
    /// <param name="orderBy">Ordering text such as <c>name,-score</c>; empty text clears the ordering.</param>
    /// <returns>A new builder whose state carries the parsed ordering, or a failure.</returns>
    public QueryBuilder<T> OrderBy(string orderBy) =>
        ApplyCore(current => ReplaceOrdering(current, ParseOrder(current, orderBy)));

    /// <summary>Replaces the ordering with a member accessed through the map.</summary>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="keySelector">The member to order by, ascending.</param>
    /// <returns>A new builder whose state carries the ordering, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public QueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return ApplyCore(current =>
            ReplaceOrdering(current, TranslateOrder(keySelector, SortDirection.Ascending)));
    }

    /// <summary>Replaces the ordering with a typed sort field.</summary>
    /// <param name="field">The sort field to use.</param>
    /// <returns>A new builder whose state carries the ordering, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
    public QueryBuilder<T> OrderBy(SortField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return ApplyCore(current => ReplaceOrdering(current, field));
    }

    /// <summary>Replaces the ordering with a member accessed through the map, descending.</summary>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="keySelector">The member to order by, descending.</param>
    /// <returns>A new builder whose state carries the ordering, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public QueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return ApplyCore(current =>
            ReplaceOrdering(current, TranslateOrder(keySelector, SortDirection.Descending)));
    }

    /// <summary>Appends an ordering parsed from text.</summary>
    /// <param name="orderBy">Ordering text such as <c>name,-score</c>; empty text appends nothing.</param>
    /// <returns>A new builder whose state carries the appended ordering, or a failure.</returns>
    public QueryBuilder<T> ThenBy(string orderBy) =>
        ApplyCore(current => AppendOrdering(current, ParseOrder(current, orderBy)));

    /// <summary>Appends a member accessed through the map to the ordering.</summary>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="keySelector">The member to order by, ascending.</param>
    /// <returns>A new builder whose state carries the appended ordering, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public QueryBuilder<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return ApplyCore(current =>
            AppendOrdering(current, TranslateOrder(keySelector, SortDirection.Ascending)));
    }

    /// <summary>Appends a typed sort field to the ordering.</summary>
    /// <param name="field">The sort field to append.</param>
    /// <returns>A new builder whose state carries the appended ordering, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
    public QueryBuilder<T> ThenBy(SortField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return ApplyCore(current => AppendOrdering(current, field));
    }

    /// <summary>Appends a descending member accessed through the map to the ordering.</summary>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="keySelector">The member to order by, descending.</param>
    /// <returns>A new builder whose state carries the appended ordering, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public QueryBuilder<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return ApplyCore(current =>
            AppendOrdering(current, TranslateOrder(keySelector, SortDirection.Descending)));
    }

    /// <summary>Replaces the ordering as a monadic chain.</summary>
    /// <param name="orderBy">Ordering text such as <c>name,-score</c>.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public Result<QueryBuilder<T>> TryOrderBy(string orderBy) =>
        TryApply(current => ReplaceOrdering(current, ParseOrder(current, orderBy)));

    /// <summary>Replaces the ordering with a member as a monadic chain.</summary>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="keySelector">The member to order by, ascending.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryOrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return TryApply(current =>
            ReplaceOrdering(current, TranslateOrder(keySelector, SortDirection.Ascending)));
    }

    /// <summary>Replaces the ordering with a typed sort field as a monadic chain.</summary>
    /// <param name="field">The sort field to use.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryOrderBy(SortField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return TryApply(current => ReplaceOrdering(current, field));
    }

    /// <summary>Replaces the ordering with a descending member as a monadic chain.</summary>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="keySelector">The member to order by, descending.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryOrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return TryApply(current =>
            ReplaceOrdering(current, TranslateOrder(keySelector, SortDirection.Descending)));
    }

    /// <summary>Appends an ordering parsed from text as a monadic chain.</summary>
    /// <param name="orderBy">Ordering text such as <c>name,-score</c>.</param>
    /// <returns>A successful chain carrying the new builder, or the parse failure.</returns>
    public Result<QueryBuilder<T>> TryThenBy(string orderBy) =>
        TryApply(current => AppendOrdering(current, ParseOrder(current, orderBy)));

    /// <summary>Appends a member to the ordering as a monadic chain.</summary>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="keySelector">The member to order by, ascending.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return TryApply(current =>
            AppendOrdering(current, TranslateOrder(keySelector, SortDirection.Ascending)));
    }

    /// <summary>Appends a typed sort field as a monadic chain.</summary>
    /// <param name="field">The sort field to append.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryThenBy(SortField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return TryApply(current => AppendOrdering(current, field));
    }

    /// <summary>Appends a descending member to the ordering as a monadic chain.</summary>
    /// <typeparam name="TKey">The member type.</typeparam>
    /// <param name="keySelector">The member to order by, descending.</param>
    /// <returns>A successful chain carrying the new builder, or the translation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return TryApply(current =>
            AppendOrdering(current, TranslateOrder(keySelector, SortDirection.Descending)));
    }

    // ---- Paging --------------------------------------------------------

    /// <summary>Sets the page request.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size, between 1 and the configured maximum.</param>
    /// <returns>A new builder whose state carries the page, or a failure.</returns>
    public QueryBuilder<T> Page(int page, int pageSize) =>
        ApplyCore(current => ApplyPage(current, page, pageSize));

    /// <summary>Sets the page request from a typed request.</summary>
    /// <param name="request">The page request.</param>
    /// <returns>A new builder whose state carries the page, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public QueryBuilder<T> Page(PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ApplyCore(current => ApplyPage(current, request.Page, request.PageSize));
    }

    /// <summary>Sets the page request as a monadic chain.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size, between 1 and the configured maximum.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    public Result<QueryBuilder<T>> TryPage(int page, int pageSize) =>
        TryApply(current => ApplyPage(current, page, pageSize));

    /// <summary>Sets the page request from a typed request as a monadic chain.</summary>
    /// <param name="request">The page request.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryPage(PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return TryApply(current => ApplyPage(current, request.Page, request.PageSize));
    }

    // ---- Search --------------------------------------------------------

    /// <summary>Sets or clears a text search.</summary>
    /// <param name="search">The text to search for; empty or whitespace clears the search.</param>
    /// <param name="mode">How the text is matched; <see cref="SearchMode.Contains"/> by default.</param>
    /// <returns>A new builder whose state carries the search, or a failure.</returns>
    public QueryBuilder<T> Search(string search, SearchMode mode = SearchMode.Contains) =>
        ApplyCore(current => ApplySearch(current, search, mode));

    /// <summary>Sets a search request.</summary>
    /// <param name="request">The search request to apply.</param>
    /// <returns>A new builder whose state carries the search, or a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public QueryBuilder<T> Search(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ApplyCore(current => ApplySearch(current, request));
    }

    /// <summary>Sets or clears a text search as a monadic chain.</summary>
    /// <param name="search">The text to search for; empty or whitespace clears the search.</param>
    /// <param name="mode">How the text is matched; <see cref="SearchMode.Contains"/> by default.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    public Result<QueryBuilder<T>> TrySearch(string search, SearchMode mode = SearchMode.Contains) =>
        TryApply(current => ApplySearch(current, search, mode));

    /// <summary>Sets a search request as a monadic chain.</summary>
    /// <param name="request">The search request to apply.</param>
    /// <returns>A successful chain carrying the new builder, or the validation failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TrySearch(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return TryApply(current => ApplySearch(current, request));
    }

    // ---- Execution options ---------------------------------------------

    /// <summary>Sets whether duplicate rows are removed.</summary>
    /// <param name="value">True to apply DISTINCT; defaults to true.</param>
    /// <returns>A new builder whose state carries the updated options.</returns>
    public QueryBuilder<T> Distinct(bool value = true) =>
        ApplyCore(current => UpdateOptions(current, current.Options with { Distinct = value }));

    /// <summary>Sets whether entities are tracked.</summary>
    /// <param name="value">True to enable <c>AsNoTracking</c>; defaults to true.</param>
    /// <returns>A new builder whose state carries the updated options.</returns>
    public QueryBuilder<T> AsNoTracking(bool value = true) =>
        ApplyCore(current => UpdateOptions(current, current.Options with { AsNoTracking = value }));

    /// <summary>Sets the hard row cap.</summary>
    /// <param name="maxRows">The maximum rows to return; must be positive.</param>
    /// <returns>A new builder whose state carries the updated options.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRows"/> is zero or negative.</exception>
    public QueryBuilder<T> MaxRows(int maxRows)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRows);
        return ApplyCore(current => UpdateOptions(current, current.Options with { MaxRows = maxRows }));
    }

    /// <summary>Adds a navigation path to eager loading.</summary>
    /// <param name="path">The navigation path, such as <c>Children</c> or <c>Customer.Address</c>.</param>
    /// <returns>A new builder whose state carries the updated options.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty, or whitespace.</exception>
    public QueryBuilder<T> Include(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return ApplyCore(current =>
            current.Options.Includes.Contains(path, StringComparer.Ordinal)
                ? Result<Query<T>>.Ok(current)
                : UpdateOptions(current, current.Options with
                {
                    Includes = [.. current.Options.Includes, path],
                }));
    }

    /// <summary>Replaces the execution options.</summary>
    /// <param name="options">The complete options to apply.</param>
    /// <returns>A new builder whose state carries the options.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    public QueryBuilder<T> WithOptions(QueryExecutionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ApplyCore(current => UpdateOptions(current, options));
    }

    /// <summary>Sets whether duplicate rows are removed, as a monadic chain.</summary>
    /// <param name="value">True to apply DISTINCT; defaults to true.</param>
    /// <returns>A successful chain carrying the new builder.</returns>
    public Result<QueryBuilder<T>> TryDistinct(bool value = true) =>
        TryApply(current => UpdateOptions(current, current.Options with { Distinct = value }));

    /// <summary>Sets whether entities are tracked, as a monadic chain.</summary>
    /// <param name="value">True to enable <c>AsNoTracking</c>; defaults to true.</param>
    /// <returns>A successful chain carrying the new builder.</returns>
    public Result<QueryBuilder<T>> TryAsNoTracking(bool value = true) =>
        TryApply(current => UpdateOptions(current, current.Options with { AsNoTracking = value }));

    /// <summary>Sets the hard row cap as a monadic chain.</summary>
    /// <param name="maxRows">The maximum rows to return; must be positive.</param>
    /// <returns>A successful chain carrying the new builder, or a typed failure when the cap is not positive.</returns>
    public Result<QueryBuilder<T>> TryMaxRows(int maxRows)
    {
        // Preserve: a failed chain keeps its original errors instead of validating new input.
        if (_state.IsFailure)
            return Result<QueryBuilder<T>>.Fail(_state.Errors, _state.StatusCode);

        return maxRows > 0
            ? TryApply(current => UpdateOptions(
                current,
                current.Options with { MaxRows = maxRows }))
            : Result<QueryBuilder<T>>.Fail(QueryOutcome.Failure.MaxRowsInvalid);
    }

    /// <summary>Adds a navigation path to eager loading, as a monadic chain.</summary>
    /// <param name="path">The navigation path, such as <c>Children</c> or <c>Customer.Address</c>.</param>
    /// <returns>A successful chain carrying the new builder, or a typed failure when the path is blank.</returns>
    public Result<QueryBuilder<T>> TryInclude(string path)
    {
        // Preserve: a failed chain keeps its original errors instead of validating new input.
        if (_state.IsFailure)
            return Result<QueryBuilder<T>>.Fail(_state.Errors, _state.StatusCode);

        if (string.IsNullOrWhiteSpace(path))
            return Result<QueryBuilder<T>>.Fail(QueryOutcome.Failure.IncludeInvalid);

        return TryApply(current =>
            current.Options.Includes.Contains(path, StringComparer.Ordinal)
                ? Result<Query<T>>.Ok(current)
                : UpdateOptions(current, current.Options with
                {
                    Includes = [.. current.Options.Includes, path],
                }));
    }

    /// <summary>Replaces the execution options as a monadic chain.</summary>
    /// <param name="options">The complete options to apply.</param>
    /// <returns>A successful chain carrying the new builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    public Result<QueryBuilder<T>> TryWithOptions(QueryExecutionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return TryApply(current => UpdateOptions(current, options));
    }

    // ---- Helpers -------------------------------------------------------

    private static Result<Query<T>> UpdateOptions(Query<T> current, QueryExecutionOptions options) =>
        Result<Query<T>>.Ok(current with { Options = options });

    private static Result<Query<T>> ApplyPage(Query<T> current, int page, int pageSize)
    {
        // Validate: paging is strict; an out-of-range value is an error instead of a silent clamp.
        if (page < 1)
            return Result<Query<T>>.Fail(QueryOutcome.Failure.PageInvalid);

        if (pageSize < 1 || pageSize > QueryConstant.Limits.Page.MaxSize)
            return Result<Query<T>>.Fail(
                QueryOutcome.Failure.PageSizeInvalid(QueryConstant.Limits.Page.MaxSize));

        return Result<Query<T>>.Ok(current with { Page = new PageRequest(page, pageSize) });
    }

    private Result<Query<T>> ApplySearch(Query<T> current, string? search, SearchMode mode)
    {
        // Validate: empty input clears the search instead of constructing an invalid request.
        if (string.IsNullOrWhiteSpace(search))
            return Result<Query<T>>.Ok(current with { Search = null });

        string text = search.Trim();

        // Guard: the request constructor throws for oversized text, so the limit is
        // checked here to keep user input on the typed-failure path.
        if (text.Length > QueryConstant.Limits.Search.MaxLength)
            return Result<Query<T>>.Fail(
                QueryOutcome.Failure.SearchTooLong(QueryConstant.Limits.Search.MaxLength));

        string[] terms = text.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (terms.Length > QueryConstant.Limits.Search.MaxTerms)
            return Result<Query<T>>.Fail(
                QueryOutcome.Failure.TooManySearchTerms(QueryConstant.Limits.Search.MaxTerms));

        // Validate: a map without a searchable field cannot serve the search.
        if (_map is null || !_map.Fields.Any(field => field.Searchable))
            return Result<Query<T>>.Fail(QueryOutcome.Failure.SearchNotConfigured);

        SearchRequest request = mode == SearchMode.FullText
            ? new FullTextSearchRequest(text)
            : new BasicSearchRequest(text, mode);

        return Result<Query<T>>.Ok(current with { Search = request });
    }

    private Result<Query<T>> ApplySearch(Query<T> current, SearchRequest request)
    {
        Result validation = ValidateSearch(request.Fields);
        return validation.IsFailure
            ? Result<Query<T>>.Fail(validation.Errors, validation.StatusCode)
            : Result<Query<T>>.Ok(current with { Search = request });
    }

    private Result ValidateSearch(IReadOnlyList<string>? fields)
    {
        // Validate: a map without a searchable field cannot serve the search.
        if (_map is null || !_map.Fields.Any(field => field.Searchable))
            return Result.Fail(QueryOutcome.Failure.SearchNotConfigured);

        if (fields is null)
            return Result.Success();

        if (fields.Count == 0)
            return Result.Fail(QueryOutcome.Failure.SearchNotConfigured);

        if (fields.Count > QueryConstant.Limits.Search.MaxFields)
            return Result.Fail(
                QueryOutcome.Failure.TooManySearchFields(QueryConstant.Limits.Search.MaxFields));

        // Validate: every named field must exist and be searchable, so a typo fails
        // instead of silently matching nothing.
        foreach (string name in fields)
        {
            if (!_map.TryGet(name, out QueryField? field))
                return Result.Fail(QueryOutcome.Failure.NotMapped(name));

            if (!field.Searchable)
                return Result.Fail(QueryOutcome.Failure.SearchNotConfigured);
        }

        return Result.Success();
    }

    private Result<IReadOnlyList<SortField>> ParseOrder(Query<T> current, string? text)
    {
        Result<Query<T>> parsed = QueryParser.Parse<T>(
            new QueryParameters { OrderBy = text },
            _map!,
            current.Limits);

        return parsed.IsFailure
            ? Result<IReadOnlyList<SortField>>.Fail(parsed.Errors, parsed.StatusCode)
            : Result<IReadOnlyList<SortField>>.Ok(parsed.Value.OrderBy);
    }

    private static Result<Query<T>> ReplaceOrdering(
        Query<T> current,
        Result<IReadOnlyList<SortField>> parsed) =>
        parsed.IsFailure
            ? Result<Query<T>>.Fail(parsed.Errors, parsed.StatusCode)
            : Result<Query<T>>.Ok(current with { OrderBy = parsed.Value });

    private Result<Query<T>> ReplaceOrdering(Query<T> current, SortField field)
    {
        Result validation = ValidateSortField(field);
        return validation.IsFailure
            ? Result<Query<T>>.Fail(validation.Errors, validation.StatusCode)
            : Result<Query<T>>.Ok(current with { OrderBy = [field] });
    }

    private Result<Query<T>> ReplaceOrdering(Query<T> current, Result<SortField> translated) =>
        translated.IsFailure
            ? Result<Query<T>>.Fail(translated.Errors, translated.StatusCode)
            : ReplaceOrdering(current, translated.Value);

    private static Result<Query<T>> AppendOrdering(
        Query<T> current,
        Result<IReadOnlyList<SortField>> parsed)
    {
        if (parsed.IsFailure)
            return Result<Query<T>>.Fail(parsed.Errors, parsed.StatusCode);

        Result capacity = EnsureOrderCapacity(current, parsed.Value.Count);
        return capacity.IsFailure
            ? Result<Query<T>>.Fail(capacity.Errors, capacity.StatusCode)
            : Result<Query<T>>.Ok(current with
            {
                OrderBy = [.. current.OrderBy, .. parsed.Value],
            });
    }

    private Result<Query<T>> AppendOrdering(Query<T> current, SortField field)
    {
        Result validation = ValidateSortField(field);
        if (validation.IsFailure)
            return Result<Query<T>>.Fail(validation.Errors, validation.StatusCode);

        Result capacity = EnsureOrderCapacity(current, 1);
        return capacity.IsFailure
            ? Result<Query<T>>.Fail(capacity.Errors, capacity.StatusCode)
            : Result<Query<T>>.Ok(current with { OrderBy = [.. current.OrderBy, field] });
    }

    private Result<Query<T>> AppendOrdering(Query<T> current, Result<SortField> translated) =>
        translated.IsFailure
            ? Result<Query<T>>.Fail(translated.Errors, translated.StatusCode)
            : AppendOrdering(current, translated.Value);

    private static Result EnsureOrderCapacity(Query<T> current, int additional)
    {
        int total = current.OrderBy.Count + additional;
        return total <= current.Limits.MaxSortFields
            ? Result.Success()
            : Result.Fail(QueryOutcome.Failure.OrderSyntax(
                $"cannot exceed {current.Limits.MaxSortFields} fields"));
    }

    private Result ValidateSortField(SortField field)
    {
        if (_map is null || !_map.TryGet(field.Field, out QueryField? mapped))
            return Result.Fail(QueryOutcome.Failure.NotMapped(field.Field));

        return mapped.Sortable
            ? Result.Success()
            : Result.Fail(QueryOutcome.Failure.NotSortable(field.Field));
    }

    private Result<SortField> TranslateOrder<TKey>(
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction)
    {
        Result<QueryField> resolved = QueryExpressionTranslator<T>.ResolveMember(
            keySelector.Body,
            keySelector.Parameters[0],
            _map!);

        if (resolved.IsFailure)
            return Result<SortField>.Fail(resolved.Errors, resolved.StatusCode);

        QueryField field = resolved.Value;
        return field.Sortable
            ? Result<SortField>.Ok(new SortField(field.Name, direction, SortNulls.None))
            : Result<SortField>.Fail(QueryOutcome.Failure.NotSortable(field.Name));
    }
}