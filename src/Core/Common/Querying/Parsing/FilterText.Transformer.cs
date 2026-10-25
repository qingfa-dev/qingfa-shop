using Core.Common.Querying.Filtering;
using Core.Common.Querying.Mapping;

namespace Core.Common.Querying.Parsing;

/// <summary>Parses the Gridify-compatible filter grammar into a neutral AST.</summary>
internal static class FilterText
{
    public static Result<FilterNode> Parse<T>(string? text, ParserState<T> state)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<FilterNode>.Success(new FilterGroup(FilterLogic.And, []));

        if (text.Length > state.Options.MaxFilterLength)
        {
            return Result<FilterNode>.Fail(
                QueryOutcome.Failure.TooLong(state.Options.MaxFilterLength));
        }

        var errors = new List<Error>();
        var cursor = new TextCursor(text);
        FilterNode? node = ParseOr(cursor, state, errors);
        cursor.SkipWhitespace();

        if (!cursor.AtEnd)
            errors.Add(QueryOutcome.Failure.Syntax("unexpected trailing text"));

        // All-or-nothing: one error discards the whole filter, so a caller can
        // never act on a partially parsed tree.
        return errors.Count > 0
            ? Result<FilterNode>.Fail(errors)
            : Result<FilterNode>.Success(node!);
    }

    private static FilterNode? ParseOr<T>(
        TextCursor cursor,
        ParserState<T> state,
        List<Error> errors)
    {
        var children = new List<FilterNode>();
        FilterNode? first = ParseAnd(cursor, state, errors);
        if (first is not null)
            children.Add(first);

        while (true)
        {
            cursor.SkipWhitespace();
            if (cursor.Peek() != '|')
                break;

            cursor.Read();
            FilterNode? next = ParseAnd(cursor, state, errors);
            if (next is not null)
                children.Add(next);
        }

        return children.Count switch
        {
            0 => null,
            1 => children[0],
            _ => new FilterGroup(FilterLogic.Or, children),
        };
    }

    private static FilterNode? ParseAnd<T>(
        TextCursor cursor,
        ParserState<T> state,
        List<Error> errors)
    {
        var children = new List<FilterNode>();
        FilterNode? first = ParsePrimary(cursor, state, errors);
        if (first is not null)
            children.Add(first);

        while (true)
        {
            cursor.SkipWhitespace();
            if (cursor.Peek() != ',')
                break;

            cursor.Read();
            FilterNode? next = ParsePrimary(cursor, state, errors);
            if (next is not null)
                children.Add(next);
        }

        return children.Count switch
        {
            0 => null,
            1 => children[0],
            _ => new FilterGroup(FilterLogic.And, children),
        };
    }

    private static FilterNode? ParsePrimary<T>(
        TextCursor cursor,
        ParserState<T> state,
        List<Error> errors)
    {
        cursor.SkipWhitespace();
        if (cursor.Peek() != '(')
            return ParseCondition(cursor, state, errors);

        cursor.Read();
        Result depth = state.EnterDepth();
        if (depth.IsFailure)
        {
            errors.AddRange(depth.Errors);
            return null;
        }

        // Depth: counted once per opening parenthesis, so the limit measures
        // nesting and not the number of conditions.
        try
        {
            FilterNode? node = ParseOr(cursor, state, errors);
            cursor.SkipWhitespace();

            if (cursor.Peek() != ')')
            {
                errors.Add(QueryOutcome.Failure.Syntax("missing ')'"));
                return null;
            }

            cursor.Read();
            return node;
        }
        finally
        {
            state.ExitDepth();
        }
    }

    private static FilterNode? ParseCondition<T>(
        TextCursor cursor,
        ParserState<T> state,
        List<Error> errors)
    {
        string token = cursor.ReadField();
        if (token.Length == 0)
        {
            errors.Add(QueryOutcome.Failure.Syntax("field is missing"));
            return null;
        }

        (string fieldName, string? indexText) = SplitIndex(token);
        Result<QueryField> mapped = state.Field(fieldName);
        if (mapped.IsFailure)
        {
            errors.AddRange(mapped.Errors);
            return null;
        }

        QueryField field = mapped.Value;
        if (!field.Filterable)
        {
            errors.Add(QueryOutcome.Failure.NotFilterable(fieldName));
            return null;
        }

        // Guard: the condition budget is spent before the value is converted, so a
        // rejected condition still counts against the limit.
        Result condition = state.AddCondition();
        if (condition.IsFailure)
        {
            errors.AddRange(condition.Errors);
            return null;
        }

        string operatorToken = cursor.ReadOperator();
        if (operatorToken.Length == 0)
        {
            errors.Add(QueryOutcome.Failure.Syntax("operator is missing"));
            return null;
        }

        if (!TryMapOperator(operatorToken, out FilterOperator @operator, out string? customOperator))
        {
            errors.Add(QueryOutcome.Failure.Syntax($"unknown operator '{operatorToken}'"));
            return null;
        }

        bool caseInsensitive;
        string rawValue = cursor.ReadValue(out caseInsensitive);
        Type valueType = field.ComparisonType;

        if (!ValidateOperator(fieldName, valueType, @operator, caseInsensitive))
        {
            errors.Add(QueryOutcome.Failure.TypeMismatch(fieldName, operatorToken));
            return null;
        }

        object? indexValue = null;
        if (indexText is not null)
        {
            // Validate: an index is only meaningful on a field the map declared as
            // indexed; anything else is not filterable.
            if (!field.IsIndexed || field.IndexType is null)
            {
                errors.Add(QueryOutcome.Failure.NotFilterable(fieldName));
                return null;
            }

            Result<QueryValue> index = ValueConverter.Convert(
                indexText, field.IndexType, converter: null, fieldName);
            if (index.IsFailure)
            {
                errors.AddRange(index.Errors);
                return null;
            }

            indexValue = index.Value.Value;
        }

        if (string.Equals(rawValue, QueryConstant.Tokens.Keyword.Null, StringComparison.OrdinalIgnoreCase) &&
            !field.AllowNullSearch)
        {
            errors.Add(QueryOutcome.Failure.ValueInvalid(fieldName, rawValue));
            return null;
        }

        Result<QueryValue> converted = ValueConverter.Convert(
            rawValue, valueType, field.Converter, fieldName);
        if (converted.IsFailure)
        {
            errors.AddRange(converted.Errors);
            return null;
        }

        return new FilterCondition(
            fieldName, @operator, rawValue, converted.Value.Value, valueType,
            caseInsensitive, indexValue, customOperator);
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