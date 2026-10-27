using Application.Common.Querying.Mapping;
using Application.Common.Querying.Sorting;

namespace Application.Common.Querying.Parsing;

/// <summary>Parses the Gridify-compatible ordering grammar into neutral sort fields.</summary>
internal static class OrderText
{
    /// <summary>Parses a comma-separated ordering expression into sort fields.</summary>
    /// <param name="text">The raw ordering text (e.g., "Name,-CreatedAt asc").</param>
    /// <param name="state">The parser state with the query map and limits.</param>
    /// <returns>A list of sort fields, or a failure if syntax is invalid.</returns>
    public static Result<IReadOnlyList<SortField>> Parse<T>(string? text, ParserState<T> state)
    {
        // Default: no ordering text means no sort fields.
        if (string.IsNullOrWhiteSpace(text))
            return Result<IReadOnlyList<SortField>>.Success([]);

        // Split: each comma-separated part is one sort specification.
        string[] parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // Guard: limit the number of sort fields to prevent excessive ORDER BY clauses.
        if (parts.Length > state.Options.MaxSortFields)
        {
            return Result<IReadOnlyList<SortField>>.Fail(
                QueryOutcome.Failure.OrderSyntax(
                    $"cannot exceed {state.Options.MaxSortFields} fields"));
        }

        var errors = new List<Error>();
        var result = new List<SortField>();
        foreach (string part in parts)
        {
            string token = part;
            SortDirection direction = SortDirection.Ascending;

            // Parse: an explicit direction suffix ("asc" / "desc") follows a space.
            int space = token.LastIndexOf(' ');
            if (space >= 0)
            {
                string suffix = token[(space + 1)..];
                if (suffix.Equals(QueryConstant.Tokens.Keyword.Ascending, StringComparison.OrdinalIgnoreCase))
                {
                    token = token[..space].Trim();
                }
                else if (suffix.Equals(QueryConstant.Tokens.Keyword.Descending, StringComparison.OrdinalIgnoreCase))
                {
                    token = token[..space].Trim();
                    direction = SortDirection.Descending;
                }
                else
                {
                    // Validate: an unrecognised suffix is an error, not a field name.
                    errors.Add(QueryOutcome.Failure.OrderSyntax($"unknown direction '{suffix}'"));
                    continue;
                }
            }

            // Sort: a leading '-' is the descending alias, and it wins over the
            // explicit suffix so `-name desc` still sorts descending.
            if (token.StartsWith('-'))
            {
                token = token[1..];
                direction = SortDirection.Descending;
            }

            // Sort: the trailing '?' and '!' carry the null placement, and are
            // stripped before the field name is resolved.
            SortNulls nulls = SortNulls.None;
            if (token.EndsWith('?'))
            {
                token = token[..^1];
                nulls = SortNulls.HasValue;
            }
            else if (token.EndsWith('!'))
            {
                token = token[..^1];
                nulls = SortNulls.NotHasValue;
            }

            // Resolve: the cleaned token is the field name to look up in the map.
            Result<QueryField> mapped = state.Field(token);
            if (mapped.IsFailure)
            {
                errors.AddRange(mapped.Errors);
                continue;
            }

            QueryField field = mapped.Value;
            // Validate: the map controls which fields are sortable.
            if (!field.Sortable)
            {
                errors.Add(QueryOutcome.Failure.NotSortable(token));
                continue;
            }

            // Validate: null placement is meaningless on a non-nullable member.
            if (nulls != SortNulls.None && !IsNullable(field.ValueType))
            {
                errors.Add(QueryOutcome.Failure.OrderSyntax($"field '{token}' is not nullable"));
                continue;
            }

            result.Add(new SortField(token, direction, nulls));
        }

        // Collect: accumulate all errors so the caller sees every problem in one pass.
        return errors.Count > 0
            ? Result<IReadOnlyList<SortField>>.Fail(errors)
            : Result<IReadOnlyList<SortField>>.Success(result);
    }

    // Parse: a type is nullable if it's a reference type or a Nullable<T> wrapper.
    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}