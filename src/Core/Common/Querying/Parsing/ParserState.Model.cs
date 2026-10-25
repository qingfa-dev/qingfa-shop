using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;
using Core.Common.Querying.Mapping;

namespace Core.Common.Querying.Parsing;

/// <summary>Shared parser state: map, options, and the running limits.</summary>
internal sealed class ParserState<T>(QueryMap<T> map, QueryOptions options)
{
    public QueryMap<T> Map { get; } = map;
    public QueryOptions Options { get; } = options;
    public int Conditions { get; private set; }
    public int Depth { get; private set; }

    /// <summary>Resolves a field name against the map.</summary>
    /// <param name="name">The field name to resolve.</param>
    /// <returns>The mapped field, or a failure when the name is not mapped.</returns>
    public Result<QueryField> Field(string name) =>
        Map.TryGet(name, out QueryField? field)
            ? Result<QueryField>.Success(field)
            : Result<QueryField>.Fail(QueryOutcome.Failure.NotMapped(name));

    /// <summary>Enters one nesting level.</summary>
    /// <returns>Success while the depth stays within the limit; otherwise a failure.</returns>
    public Result EnterDepth()
    {
        Depth++;
        if (Depth <= Options.MaxFilterDepth)
            return Result.Success();

        return Result.Fail(QueryOutcome.Failure.DepthExceeded(Options.MaxFilterDepth));
    }

    public void ExitDepth() => Depth--;

    /// <summary>Counts one condition against the budget.</summary>
    /// <returns>Success while the budget holds; otherwise a failure.</returns>
    public Result AddCondition()
    {
        Conditions++;
        if (Conditions <= Options.MaxConditions)
            return Result.Success();

        return Result.Fail(QueryOutcome.Failure.TooManyConditions(Options.MaxConditions));
    }
}