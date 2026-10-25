using Application.Common.Querying.Grouping;
using Application.Common.Querying.Mapping;

namespace Application.Common.Querying.Parsing;

/// <summary>Parses the grouping key list into neutral group-by fields.</summary>
internal static class GroupText
{
    public static Result<IReadOnlyList<GroupByField>> Parse<T>(string? text, ParserState<T> state)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<IReadOnlyList<GroupByField>>.Success([]);

        string[] parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > state.Options.MaxGroupFields)
        {
            return Result<IReadOnlyList<GroupByField>>.Fail(
                QueryOutcome.Failure.Syntax(
                    $"group by cannot exceed {state.Options.MaxGroupFields} fields"));
        }

        var errors = new List<Error>();
        var result = new List<GroupByField>();
        foreach (string part in parts)
        {
            string fieldName = part;

            Result<QueryField> mapped = state.Field(fieldName);
            if (mapped.IsFailure)
            {
                errors.AddRange(mapped.Errors);
                continue;
            }

            QueryField field = mapped.Value;
            if (!field.Groupable)
            {
                errors.Add(QueryOutcome.Failure.NotGroupable(fieldName));
                continue;
            }

            result.Add(new GroupByField(fieldName));
        }

        return errors.Count > 0
            ? Result<IReadOnlyList<GroupByField>>.Fail(errors)
            : Result<IReadOnlyList<GroupByField>>.Success(result);
    }
}