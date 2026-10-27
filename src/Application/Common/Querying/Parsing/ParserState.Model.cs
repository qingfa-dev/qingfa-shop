
using Application.Common.Querying.Mapping;

namespace Application.Common.Querying.Parsing;
/// <summary>Immutable parser state: map, options, and the running limits.</summary>
/// <remarks>
/// All mutation methods return a new <see cref="ParserState{T}"/> instance.
/// Thread the returned state through recursive parsing calls.
/// </remarks>
/// <param name="Map">The query map that defines which fields are addressable.</param>
/// <param name="Options">Parse-time limits (max depth, max conditions, etc.).</param>
/// <param name="Conditions">Running count of conditions parsed so far.</param>
/// <param name="Depth">Current nesting depth of parentheses in the filter expression.</param>
internal sealed record ParserState<T>(
    QueryMap<T> Map,
    QueryParseLimits Options,
    int Conditions = 0,
    int Depth = 0)
{
    /// <summary>Resolves a field name against the map.</summary>
    /// <param name="name">The field name to resolve.</param>
    /// <returns>The mapped field, or a failure when the name is not mapped.</returns>
    public Result<QueryField> Field(string name) =>
        Map.TryGet(name, out QueryField? field)
            ? Result<QueryField>.Success(field)
            : Result<QueryField>.Fail(QueryOutcome.Failure.NotMapped(name));

    /// <summary>Enters one nesting level, checking the depth limit.</summary>
    /// <returns>A tuple of the new state and a result indicating success or depth limit exceeded.</returns>
    public (ParserState<T> State, Result Result) EnterDepth()
    {
        // Enforce: depth limit prevents stack overflow from deeply nested parentheses.
        var newDepth = Depth + 1;
        if (newDepth <= Options.MaxFilterDepth)
            return (this with { Depth = newDepth }, Result.Success());

        return (this, Result.Fail(QueryOutcome.Failure.DepthExceeded(Options.MaxFilterDepth)));
    }

    /// <summary>Exits one nesting level.</summary>
    /// <returns>A new state with decremented depth.</returns>
    public ParserState<T> ExitDepth() => this with { Depth = Depth - 1 };

    /// <summary>Counts one condition against the budget.</summary>
    /// <returns>A tuple of the new state and a result indicating success or budget exceeded.</returns>
    public (ParserState<T> State, Result Result) AddCondition()
    {
        // Enforce: condition limit prevents resource exhaustion from overly complex filters.
        var newConditions = Conditions + 1;
        if (newConditions <= Options.MaxConditions)
            return (this with { Conditions = newConditions }, Result.Success());

        return (this, Result.Fail(QueryOutcome.Failure.TooManyConditions(Options.MaxConditions)));
    }
}