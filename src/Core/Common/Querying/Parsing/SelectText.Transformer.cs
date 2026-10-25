using Core.Common.Querying.Mapping;
using Core.Common.Querying.Projection;

namespace Core.Common.Querying.Parsing;

/// <summary>Parses the projection list into neutral select fields.</summary>
internal static class SelectText
{
    public static Result<IReadOnlyList<SelectField>> Parse<T>(string? text, ParserState<T> state)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<IReadOnlyList<SelectField>>.Success([]);

        string[] parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > state.Options.MaxSelectFields)
        {
            return Result<IReadOnlyList<SelectField>>.Fail(
                QueryOutcome.Failure.Syntax(
                    $"select cannot exceed {state.Options.MaxSelectFields} columns"));
        }

        var errors = new List<Error>();
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<SelectField>();
        foreach (string part in parts)
        {
            // Normalize: an explicit `as` renames the column; without one the
            // column keeps the field name.
            string[] pieces = part.Split(
                QueryConstant.Tokens.Separator.Alias, 2, StringSplitOptions.TrimEntries);
            string fieldName = pieces[0];
            string alias = pieces.Length == 2 ? pieces[1] : fieldName;

            Result<QueryField> mapped = state.Field(fieldName);
            if (mapped.IsFailure)
            {
                errors.AddRange(mapped.Errors);
                continue;
            }

            QueryField field = mapped.Value;
            if (!field.Selectable)
            {
                errors.Add(QueryOutcome.Failure.NotSelectable(fieldName));
                continue;
            }

            // Validate: two columns may not share a name, or the result would carry
            // an ambiguous column.
            if (!aliases.Add(alias))
            {
                errors.Add(QueryOutcome.Failure.Syntax($"duplicate alias '{alias}'"));
                continue;
            }

            result.Add(new SelectField(fieldName, alias));
        }

        return errors.Count > 0
            ? Result<IReadOnlyList<SelectField>>.Fail(errors)
            : Result<IReadOnlyList<SelectField>>.Success(result);
    }
}