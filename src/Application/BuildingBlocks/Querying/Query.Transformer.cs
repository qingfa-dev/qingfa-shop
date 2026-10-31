using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Grouping;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Querying.Parsing;
using Application.BuildingBlocks.Querying.Projection;
using Application.BuildingBlocks.Querying.Searching;
using Application.BuildingBlocks.Querying.Sorting;

namespace Application.BuildingBlocks.Querying;

/// <summary>Parses query-string text into a neutral query. Never names an ORM.</summary>
public static partial class QueryParser
{
    /// <summary>Parses query parameters into a neutral query.</summary>
    /// <param name="parameters">The raw query parameters.</param>
    /// <param name="map">The field map the query is allowed to address.</param>
    /// <param name="options">The parser limits; the defaults apply when omitted.</param>
    /// <returns>
    /// The parsed query, or a failure carrying every problem found. Each part is
    /// parsed independently, so one request can report several errors at once.
    /// </returns>
    public static Result<Query<T>> Parse<T>(
        QueryParameters parameters,
        QueryMap<T> map,
        QueryParseLimits? options = null)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(map);
        options ??= QueryParseLimits.Default;

        var state = new ParserState<T>(map, options);
        var parts = new List<IResult<Error>>();

        // Absence: a part is only parsed when the request actually asked for it,
        // so an omitted part stays null on the query instead of becoming empty.
        FilterNode? filter = null;
        if (!string.IsNullOrWhiteSpace(parameters.Filter))
        {
            Result<FilterNode> parsed = FilterText.Parse(parameters.Filter, state);
            parts.Add(parsed);
            filter = parsed.IsFailure ? null : parsed.Value;
        }

        Result<IReadOnlyList<SortField>> order = OrderText.Parse(parameters.OrderBy, state);
        parts.Add(order);

        Result<PageRequest> page = PageText.Parse(parameters.Page, parameters.PageSize, state);
        parts.Add(page);

        SearchRequest? search = null;
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            Result<SearchRequest> parsed = SearchText.Parse(parameters, state);
            parts.Add(parsed);
            search = parsed.IsFailure ? null : parsed.Value;
        }

        Result<IReadOnlyList<SelectField>> select = SelectText.Parse(parameters.Select, state);
        parts.Add(select);

        Result<IReadOnlyList<GroupByField>> group = GroupText.Parse(parameters.GroupBy, state);
        parts.Add(group);

        Result<IReadOnlyList<AggregateSpec>> aggregate = AggregateText.Parse(parameters.Aggregate, state);
        parts.Add(aggregate);

        // The shape rule only applies to the parts the request actually asked for.
        // A part that already failed contributes nothing: its own error is reported.
        IReadOnlyList<SelectField>? selected = ValueOf(select, parameters.Select);
        IReadOnlyList<GroupByField>? grouped = ValueOf(group, parameters.GroupBy);
        IReadOnlyList<AggregateSpec>? aggregated = ValueOf(aggregate, parameters.Aggregate);

        Result shape = ShapeValidator.Validate(selected, grouped, aggregated);
        parts.Add(shape);

        var errors = parts
            .Where(part => part.IsFailure)
            .SelectMany(part => part.Errors)
            .ToList();

        if (errors.Count > 0)
            return Result<Query<T>>.Fail(errors);

        return Result<Query<T>>.Success(new Query<T>
        {
            Filter = filter,
            OrderBy = order.Value,
            Page = page.Value,
            Search = search,
            Select = selected,
            GroupBy = grouped,
            Aggregate = aggregated
        });
    }

    /// <summary>Parses query parameters against the map registered for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type the query addresses.</typeparam>
    /// <param name="parameters">The raw query parameters.</param>
    /// <param name="provider">The query map provider composed for the application.</param>
    /// <param name="options">The parser limits; the defaults apply when omitted.</param>
    /// <returns>
    /// The parsed query, or a failure carrying every problem found. A missing map
    /// fails the parse rather than falling back to an unmapped query.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="parameters"/> or <paramref name="provider"/> is <c>null</c>.
    /// </exception>
    public static Result<Query<T>> Parse<T>(
        QueryParameters parameters,
        IQueryMapProvider provider,
        QueryParseLimits? options = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(provider);

        if (!provider.TryGet<T>(out QueryMap<T>? map))
            return Result<Query<T>>.Fail(QueryMapOutcome.Failure.NotRegistered(typeof(T)));

        return Parse(parameters, map!, options);
    }

    /// <summary>Reads a part's value only when it succeeded and the request asked for it.</summary>
    /// <typeparam name="TItem">The item type the part produces.</typeparam>
    /// <param name="part">The parsed part.</param>
    /// <param name="requested">The raw text the request supplied.</param>
    /// <returns>The part's value, or null when the part failed or was not requested.</returns>
    private static IReadOnlyList<TItem>? ValueOf<TItem>(
        Result<IReadOnlyList<TItem>> part,
        string? requested)
    {
        // Guard: a failed parse must not leak a partial value into the shape validator,
        // because its own error is already reported and the query will fail anyway.
        if (part.IsFailure || string.IsNullOrWhiteSpace(requested))
            return null;
        return part.Value;
    }
}