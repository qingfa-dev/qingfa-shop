using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Projection;

namespace Application.BuildingBlocks.Querying.Parsing;

/// <summary>Parses the projection list into neutral select fields.</summary>
internal static class SelectText
{
    /// <summary>Parses a comma-separated projection expression into select fields.</summary>
    /// <param name="text">The raw select text (e.g., "Name,CreatedAt as Date").</param>
    /// <param name="state">The parser state with the query map and limits.</param>
    /// <returns>A list of select fields, or a failure if syntax is invalid.</returns>
    public static Result<IReadOnlyList<SelectField>> Parse<T>(string? text, ParserState<T> state)
    {
        // Default: no select text means no projection.
        if (string.IsNullOrWhiteSpace(text))
            return Result<IReadOnlyList<SelectField>>.Success([]);

        // Split: each comma-separated part is one column specification.
        string[] parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // Guard: limit the number of columns to prevent excessive SELECT clauses.
        if (parts.Length > state.Options.MaxSelectFields)
        {
            return Result<IReadOnlyList<SelectField>>.Fail(
                QueryOutcome.Failure.Syntax(
                    $"select cannot exceed {state.Options.MaxSelectFields} columns"));
        }

        var errors = new List<Error>();
        // Track: aliases must be unique so the result set has no duplicate column names.
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Track: fields must be unique so the same column is not projected twice.
        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<SelectField>();

        foreach (string part in parts)
        {
            // Normalize: an explicit `as` renames the column; without one the
            // column keeps the field name.
            string[] pieces = part.Split(
                QueryConstant.Tokens.Separator.Alias, 2, StringSplitOptions.TrimEntries);
            string fieldName = pieces[0];
            string alias = pieces.Length == 2 ? pieces[1] : fieldName;

            // Resolve: look up the field in the map before checking capabilities.
            Result<QueryField> mapped = state.Field(fieldName);
            if (mapped.IsFailure)
            {
                errors.AddRange(mapped.Errors);
                continue;
            }

            QueryField field = mapped.Value;
            // Validate: the map controls which fields are projectable.
            if (!field.Selectable)
            {
                errors.Add(QueryOutcome.Failure.NotSelectable(fieldName));
                continue;
            }

            // Validate: the same field cannot be selected more than once.
            if (!fieldNames.Add(fieldName))
            {
                errors.Add(QueryOutcome.Failure.Duplicate(fieldName));
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

        // Collect: accumulate all errors so the caller sees every problem in one pass.
        return errors.Count > 0
            ? Result<IReadOnlyList<SelectField>>.Fail(errors)
            : Result<IReadOnlyList<SelectField>>.Success(result);
    }
}