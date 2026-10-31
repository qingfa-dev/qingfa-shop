using Application.BuildingBlocks.Querying.Grouping;
using Application.BuildingBlocks.Querying.Mapping;

namespace Application.BuildingBlocks.Querying.Parsing;

/// <summary>Parses the grouping key list into neutral group-by fields.</summary>
internal static class GroupText
{
    /// <summary>Parses a comma-separated group-by expression into group-by fields.</summary>
    /// <param name="text">The raw group-by text (e.g., "Status,Category").</param>
    /// <param name="state">The parser state with the query map and limits.</param>
    /// <returns>A list of group-by fields, or a failure if syntax is invalid.</returns>
    public static Result<IReadOnlyList<GroupByField>> Parse<T>(string? text, ParserState<T> state)
    {
        // Default: no group-by text means no grouping.
        if (string.IsNullOrWhiteSpace(text))
            return Result<IReadOnlyList<GroupByField>>.Success([]);

        // Split: each comma-separated part is one grouping key.
        string[] parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // Guard: limit the number of group keys to prevent excessive GROUP BY clauses.
        if (parts.Length > state.Options.MaxGroupFields)
        {
            return Result<IReadOnlyList<GroupByField>>.Fail(
                QueryOutcome.Failure.Syntax(
                    $"group by cannot exceed {state.Options.MaxGroupFields} fields"));
        }

        var errors = new List<Error>();
        // Track: group keys must be unique — grouping by the same field twice is redundant.
        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<GroupByField>();

        foreach (string part in parts)
        {
            string fieldName = part;

            // Resolve: look up the field in the map.
            Result<QueryField> mapped = state.Field(fieldName);
            if (mapped.IsFailure)
            {
                errors.AddRange(mapped.Errors);
                continue;
            }

            QueryField field = mapped.Value;
            // Validate: the map controls which fields can be grouping keys.
            if (!field.Groupable)
            {
                errors.Add(QueryOutcome.Failure.NotGroupable(fieldName));
                continue;
            }

            // Validate: the same field cannot be grouped more than once.
            if (!fieldNames.Add(fieldName))
            {
                errors.Add(QueryOutcome.Failure.Duplicate(fieldName));
                continue;
            }

            result.Add(new GroupByField(fieldName));
        }

        // Collect: accumulate all errors so the caller sees every problem in one pass.
        return errors.Count > 0
            ? Result<IReadOnlyList<GroupByField>>.Fail(errors)
            : Result<IReadOnlyList<GroupByField>>.Success(result);
    }
}