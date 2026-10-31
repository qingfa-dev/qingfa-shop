using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Mapping;

namespace Application.BuildingBlocks.Querying.Parsing;

/// <summary>Parses the Gridify-compatible filter grammar into a neutral AST.</summary>
internal static class FilterText
{
    /// <summary>Parses query parameters into a filter AST.</summary>
    public static Result<FilterNode> Parse<T>(string? text, ParserState<T> state)
    {
        // Default: an empty filter matches everything — return an empty AND group
        // so downstream translation treats it as "no filter applied."
        if (string.IsNullOrWhiteSpace(text))
            return Result<FilterNode>.Success(new FilterGroup(FilterLogic.And, []));

        // Guard: reject oversized input before tokenization to avoid resource exhaustion.
        if (text.Length > state.Options.MaxFilterLength)
        {
            return Result<FilterNode>.Fail(
                QueryOutcome.Failure.TooLong(state.Options.MaxFilterLength));
        }

        var cursor = new TextCursor(text);
        var (newState, nodeResult) = ParseOr(cursor, state);

        if (nodeResult.IsFailure)
            return nodeResult;

        cursor.SkipWhitespace();

        // Validate: after a successful parse, any remaining text means the grammar
        // did not consume the full input — likely a stray character or unclosed parenthesis.
        if (!cursor.AtEnd)
            return Result<FilterNode>.Fail(QueryOutcome.Failure.Syntax("unexpected trailing text"));

        return nodeResult;
    }

    /// <summary>
    /// Parses the lowest-precedence level: OR-separated AND groups.
    /// Grammar: or → and ('|' and)*
    /// </summary>
    private static (ParserState<T> State, Result<FilterNode> Result) ParseOr<T>(
        TextCursor cursor,
        ParserState<T> state)
    {
        // Parse: recursive descent — OR has the lowest precedence, so it delegates to AND.
        var (newState, firstResult) = ParseAnd(cursor, state);
        if (firstResult.IsFailure)
            return (newState, Result<FilterNode>.Fail(firstResult.Errors));

        var children = new List<FilterNode> { firstResult.Value };

        while (true)
        {
            cursor.SkipWhitespace();
            if (cursor.Peek() != '|')
                break;

            cursor.Read();
            var (nextState, nextResult) = ParseAnd(cursor, newState);
            newState = nextState;

            if (nextResult.IsFailure)
                return (newState, Result<FilterNode>.Fail(nextResult.Errors));

            children.Add(nextResult.Value);
        }

        // Flatten: a single child is returned directly instead of wrapping it in
        // a group node, keeping the AST minimal for simple expressions.
        var node = children.Count switch
        {
            0 => null,
            1 => children[0],
            _ => new FilterGroup(FilterLogic.Or, children),
        };

        return (newState, Result<FilterNode>.Success(node!));
    }

    /// <summary>
    /// Parses the middle-precedence level: AND-separated primary expressions.
    /// Grammar: and → primary (',' primary)*
    /// </summary>
    private static (ParserState<T> State, Result<FilterNode> Result) ParseAnd<T>(
        TextCursor cursor,
        ParserState<T> state)
    {
        // Parse: comma separates AND conditions — the same convention as Gridify.
        var (newState, firstResult) = ParsePrimary(cursor, state);
        if (firstResult.IsFailure)
            return (newState, Result<FilterNode>.Fail(firstResult.Errors));

        var children = new List<FilterNode> { firstResult.Value };

        while (true)
        {
            cursor.SkipWhitespace();
            if (cursor.Peek() != ',')
                break;

            cursor.Read();
            var (nextState, nextResult) = ParsePrimary(cursor, newState);
            newState = nextState;

            if (nextResult.IsFailure)
                return (newState, Result<FilterNode>.Fail(nextResult.Errors));

            children.Add(nextResult.Value);
        }

        // Flatten: a single child is returned directly, avoiding a redundant wrapper group.
        var node = children.Count switch
        {
            0 => null,
            1 => children[0],
            _ => new FilterGroup(FilterLogic.And, children),
        };

        return (newState, Result<FilterNode>.Success(node!));
    }

    /// <summary>
    /// Parses the highest-precedence level: a parenthesized group or a single condition.
    /// Grammar: primary → '(' or ')' | condition
    /// </summary>
    private static (ParserState<T> State, Result<FilterNode> Result) ParsePrimary<T>(
        TextCursor cursor,
        ParserState<T> state)
    {
        // Dispatch: a '(' starts a nested group; anything else is a leaf condition.
        cursor.SkipWhitespace();
        if (cursor.Peek() != '(')
            return ParseCondition(cursor, state);

        cursor.Read();
        // Enforce: increment depth before recursing so nested parentheses cannot exceed the limit.
        var (depthState, depthResult) = state.EnterDepth();
        if (depthResult.IsFailure)
            return (depthState, Result<FilterNode>.Fail(depthResult.Errors));

        // Guard: depth tracking must be unwound even if parsing throws, so the
        // condition counter and depth stay consistent for the next parse call.
        try
        {
            var (orState, orResult) = ParseOr(cursor, depthState);
            if (orResult.IsFailure)
                return (orState, Result<FilterNode>.Fail(orResult.Errors));

            cursor.SkipWhitespace();

            // Validate: a closing parenthesis is required to complete the group.
            if (cursor.Peek() != ')')
            {
                return (orState, Result<FilterNode>.Fail(QueryOutcome.Failure.Syntax("missing ')'")));
            }

            cursor.Read();
            // Restore: decrement depth after a successful group parse.
            var finalState = orState.ExitDepth();
            return (finalState, orResult);
        }
        catch
        {
            // Restore: unwind depth on error so the parser state remains consistent.
            var finalState = depthState.ExitDepth();
            throw;
        }
    }

    private static (ParserState<T> State, Result<FilterNode> Result) ParseCondition<T>(
        TextCursor cursor,
        ParserState<T> state)
    {
        // Parse: read the field name first — it's the anchor for all subsequent validation.
        string token = cursor.ReadField();
        if (token.Length == 0)
        {
            return (state, Result<FilterNode>.Fail(QueryOutcome.Failure.Syntax("field is missing")));
        }

        // Split: extract optional index from "Field[0]" syntax before resolving the name.
        (string fieldName, string? indexText) = SplitIndex(token);
        var mapped = state.Field(fieldName);
        if (mapped.IsFailure)
        {
            return (state, Result<FilterNode>.Fail(mapped.Errors));
        }

        QueryField field = mapped.Value;
        // Validate: the map controls which fields are addressable; reject early.
        if (!field.Filterable)
        {
            return (state, Result<FilterNode>.Fail(QueryOutcome.Failure.NotFilterable(fieldName)));
        }

        // Guard: the condition budget is spent before the value is converted, so a
        // rejected condition still counts against the limit.
        var (condState, condResult) = state.AddCondition();
        if (condResult.IsFailure)
        {
            return (condState, Result<FilterNode>.Fail(condResult.Errors));
        }

        string operatorToken = cursor.ReadOperator();
        if (operatorToken.Length == 0)
        {
            return (condState, Result<FilterNode>.Fail(QueryOutcome.Failure.Syntax("operator is missing")));
        }

        if (!TryMapOperator(operatorToken, out FilterOperator @operator, out string? customOperator))
        {
            return (condState, Result<FilterNode>.Fail(QueryOutcome.Failure.Syntax($"unknown operator '{operatorToken}'")));
        }

        bool caseInsensitive;
        string rawValue = cursor.ReadValue(out caseInsensitive);
        Type valueType = field.ComparisonType;

        // Validate: the operator must be compatible with the field's type before conversion.
        if (!ValidateOperator(fieldName, valueType, @operator, caseInsensitive))
        {
            return (condState, Result<FilterNode>.Fail(QueryOutcome.Failure.TypeMismatch(fieldName, operatorToken)));
        }

        object? indexValue = null;
        if (indexText is not null)
        {
            // Validate: an index is only meaningful on a field the map declared as
            // indexed; anything else is not filterable.
            if (!field.IsIndexed || field.IndexType is null)
            {
                return (condState, Result<FilterNode>.Fail(QueryOutcome.Failure.NotFilterable(fieldName)));
            }

            // Convert: the index text must match the field's declared index type.
            var index = ValueConverter.Convert(
                indexText, field.IndexType, converter: null, fieldName);
            if (index.IsFailure)
            {
                return (condState, Result<FilterNode>.Fail(index.Errors));
            }

            indexValue = index.Value.Value;
        }

        // Guard: null search is a special case — some fields explicitly disallow it.
        if (string.Equals(rawValue, QueryConstant.Tokens.Keyword.Null, StringComparison.OrdinalIgnoreCase) &&
            !field.AllowNullSearch)
        {
            return (condState, Result<FilterNode>.Fail(QueryOutcome.Failure.ValueInvalid(fieldName, rawValue)));
        }

        // Convert: transform the raw text value to the field's expected type for comparison.
        var converted = ValueConverter.Convert(
            rawValue, valueType, field.Converter, fieldName);
        if (converted.IsFailure)
        {
            return (condState, Result<FilterNode>.Fail(converted.Errors));
        }

        var node = new FilterCondition(
            fieldName, @operator, rawValue, converted.Value.Value, valueType,
            caseInsensitive, indexValue, customOperator);

        return (condState, Result<FilterNode>.Success(node));
    }

    private static bool ValidateOperator(
        string field,
        Type valueType,
        FilterOperator @operator,
        bool caseInsensitive)
    {
        // Validate: case-insensitive flag only applies to string comparisons.
        if (caseInsensitive && valueType != typeof(string))
            return false;

        return @operator switch
        {
            // Validate: string-specific operators require a string field.
            FilterOperator.Contains or FilterOperator.NotContains or
            FilterOperator.StartsWith or FilterOperator.NotStartsWith or
            FilterOperator.EndsWith or FilterOperator.NotEndsWith => valueType == typeof(string),
            // Validate: ordering operators require IComparable — unwrap Nullable<T> first.
            FilterOperator.LessThan or FilterOperator.GreaterThan or
            FilterOperator.GreaterThanOrEqual or FilterOperator.LessThanOrEqual =>
                typeof(IComparable).IsAssignableFrom(Nullable.GetUnderlyingType(valueType) ?? valueType),
            // Equal, NotEqual, and Custom work on any type.
            _ => true,
        };
    }

    private static (string Field, string? Index) SplitIndex(string token)
    {
        // Parse: extract the index from "Field[0]" syntax. If brackets are malformed,
        // treat the whole token as a field name — the map lookup will fail if it's invalid.
        int open = token.IndexOf('[');
        if (open < 0)
            return (token, null);

        // Validate: a closing bracket is required; otherwise treat as a plain field name.
        return token.EndsWith(']')
            ? (token[..open], token[(open + 1)..^1])
            : (token, null);
    }

    private static bool TryMapOperator(
        string token,
        out FilterOperator @operator,
        out string? customOperator)
    {
        customOperator = null;

        // Custom: a '#' prefix names an operator the adapter must know.
        if (token.StartsWith(QueryConstant.Tokens.Operator.Custom, StringComparison.Ordinal))
        {
            @operator = FilterOperator.Custom;
            customOperator = token[1..];
            // Validate: the custom operator name must not be empty.
            return customOperator.Length > 0;
        }

        // Normalize: map text tokens to enum values using the constant table.
        @operator = token switch
        {
            QueryConstant.Tokens.Operator.Equal => FilterOperator.Equal,
            QueryConstant.Tokens.Operator.NotEqual => FilterOperator.NotEqual,
            QueryConstant.Tokens.Operator.LessThan => FilterOperator.LessThan,
            QueryConstant.Tokens.Operator.GreaterThan => FilterOperator.GreaterThan,
            QueryConstant.Tokens.Operator.GreaterThanOrEqual => FilterOperator.GreaterThanOrEqual,
            QueryConstant.Tokens.Operator.LessThanOrEqual => FilterOperator.LessThanOrEqual,
            QueryConstant.Tokens.Operator.Contains => FilterOperator.Contains,
            QueryConstant.Tokens.Operator.NotContains => FilterOperator.NotContains,
            QueryConstant.Tokens.Operator.StartsWith => FilterOperator.StartsWith,
            QueryConstant.Tokens.Operator.NotStartsWith => FilterOperator.NotStartsWith,
            QueryConstant.Tokens.Operator.EndsWith => FilterOperator.EndsWith,
            QueryConstant.Tokens.Operator.NotEndsWith => FilterOperator.NotEndsWith,
            // Fallback: unknown tokens become Custom so the switch doesn't throw.
            _ => FilterOperator.Custom,
        };

        // Validate: the switch falls back to Custom for an unknown token, so the
        // recognised set is checked explicitly before the operator is accepted.
        return QueryConstant.Tokens.OneCharacterOperators.Contains(token) ||
            QueryConstant.Tokens.TwoCharacterOperators.Contains(token);
    }
}