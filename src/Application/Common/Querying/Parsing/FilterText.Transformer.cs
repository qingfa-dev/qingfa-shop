using Application.Common.Querying.Filtering;
using Application.Common.Querying.Mapping;

namespace Application.Common.Querying.Parsing;

/// <summary>Parses the Gridify-compatible filter grammar into a neutral AST.</summary>
internal static class FilterText
{
    /// <summary>Parses query parameters into a filter AST.</summary>
    public static Result<FilterNode> Parse<T>(string? text, ParserState<T> state)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<FilterNode>.Success(new FilterGroup(FilterLogic.And, []));

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

        if (!cursor.AtEnd)
            return Result<FilterNode>.Fail(QueryOutcome.Failure.Syntax("unexpected trailing text"));

        return nodeResult;
    }

    private static (ParserState<T> State, Result<FilterNode> Result) ParseOr<T>(
        TextCursor cursor,
        ParserState<T> state)
    {
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

        var node = children.Count switch
        {
            0 => null,
            1 => children[0],
            _ => new FilterGroup(FilterLogic.Or, children),
        };

        return (newState, Result<FilterNode>.Success(node!));
    }

    private static (ParserState<T> State, Result<FilterNode> Result) ParseAnd<T>(
        TextCursor cursor,
        ParserState<T> state)
    {
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

        var node = children.Count switch
        {
            0 => null,
            1 => children[0],
            _ => new FilterGroup(FilterLogic.And, children),
        };

        return (newState, Result<FilterNode>.Success(node!));
    }

    private static (ParserState<T> State, Result<FilterNode> Result) ParsePrimary<T>(
        TextCursor cursor,
        ParserState<T> state)
    {
        cursor.SkipWhitespace();
        if (cursor.Peek() != '(')
            return ParseCondition(cursor, state);

        cursor.Read();
        var (depthState, depthResult) = state.EnterDepth();
        if (depthResult.IsFailure)
            return (depthState, Result<FilterNode>.Fail(depthResult.Errors));

        try
        {
            var (orState, orResult) = ParseOr(cursor, depthState);
            if (orResult.IsFailure)
                return (orState, Result<FilterNode>.Fail(orResult.Errors));

            cursor.SkipWhitespace();

            if (cursor.Peek() != ')')
            {
                return (orState, Result<FilterNode>.Fail(QueryOutcome.Failure.Syntax("missing ')'")));
            }

            cursor.Read();
            var finalState = orState.ExitDepth();
            return (finalState, orResult);
        }
        catch
        {
            // Ensure depth is exited even on exception
            var finalState = depthState.ExitDepth();
            throw;
        }
    }

    private static (ParserState<T> State, Result<FilterNode> Result) ParseCondition<T>(
        TextCursor cursor,
        ParserState<T> state)
    {
        string token = cursor.ReadField();
        if (token.Length == 0)
        {
            return (state, Result<FilterNode>.Fail(QueryOutcome.Failure.Syntax("field is missing")));
        }

        (string fieldName, string? indexText) = SplitIndex(token);
        var mapped = state.Field(fieldName);
        if (mapped.IsFailure)
        {
            return (state, Result<FilterNode>.Fail(mapped.Errors));
        }

        QueryField field = mapped.Value;
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

            var index = ValueConverter.Convert(
                indexText, field.IndexType, converter: null, fieldName);
            if (index.IsFailure)
            {
                return (condState, Result<FilterNode>.Fail(index.Errors));
            }

            indexValue = index.Value.Value;
        }

        if (string.Equals(rawValue, QueryConstant.Tokens.Keyword.Null, StringComparison.OrdinalIgnoreCase) &&
            !field.AllowNullSearch)
        {
            return (condState, Result<FilterNode>.Fail(QueryOutcome.Failure.ValueInvalid(fieldName, rawValue)));
        }

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
        if (caseInsensitive && valueType != typeof(string))
            return false;

        return @operator switch
        {
            FilterOperator.Contains or FilterOperator.NotContains or
            FilterOperator.StartsWith or FilterOperator.NotStartsWith or
            FilterOperator.EndsWith or FilterOperator.NotEndsWith => valueType == typeof(string),
            FilterOperator.LessThan or FilterOperator.GreaterThan or
            FilterOperator.GreaterThanOrEqual or FilterOperator.LessThanOrEqual =>
                typeof(IComparable).IsAssignableFrom(Nullable.GetUnderlyingType(valueType) ?? valueType),
            _ => true,
        };
    }

    private static (string Field, string? Index) SplitIndex(string token)
    {
        int open = token.IndexOf('[');
        if (open < 0)
            return (token, null);

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
            return customOperator.Length > 0;
        }

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
            _ => FilterOperator.Custom,
        };

        // Validate: the switch falls back to Custom for an unknown token, so the
        // recognised set is checked explicitly before the operator is accepted.
        return QueryConstant.Tokens.OneCharacterOperators.Contains(token) ||
            QueryConstant.Tokens.TwoCharacterOperators.Contains(token);
    }
}