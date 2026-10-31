namespace Application.BuildingBlocks.Persistence.Capabilities;
/// <summary>
/// Guard utilities for validating query-related values against <see cref="QueryCapabilityConstant"/> limits.
/// </summary>
/// <remarks>
/// Returns <see cref="Result"/> instead of throwing, enabling monadic validation composition.
/// </remarks>
public static class QueryCapabilityGuard
{
    #region Filter

    /// <summary>
    /// Validates a filter text length.
    /// </summary>
    /// <param name="filter">The filter text to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateFilterLength(
        string? filter,
        string parameterName = "filter")
    {
        if (filter is null || filter.Length == 0)
        {
            return Result.Ok();
        }

        if (filter.Length > QueryCapabilityConstant.Filter.MaxLength)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.FilterTooLong(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates the nesting depth of a filter expression.
    /// </summary>
    /// <param name="depth">The nesting depth to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateFilterDepth(
        int depth,
        string parameterName = "filter")
    {
        if (depth < 0)
        {
            return Result.Ok();
        }

        if (depth > QueryCapabilityConstant.Filter.MaxDepth)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.FilterTooDeep(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates the number of conditions in a filter.
    /// </summary>
    /// <param name="conditionCount">The condition count to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateFilterConditionCount(
        int conditionCount,
        string parameterName = "filter")
    {
        if (conditionCount < 0)
        {
            return Result.Ok();
        }

        if (conditionCount > QueryCapabilityConstant.Filter.MaxConditions)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.FilterTooManyConditions(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that an operator is recognized by the grammar.
    /// </summary>
    /// <param name="operator">The operator to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateFilterOperator(
        string? @operator,
        string parameterName = "filter")
    {
        if (@operator is null || @operator.Length == 0)
        {
            return Result.Ok();
        }

        var allOperators = QueryCapabilityConstant.Tokens.TwoCharacterOperators
            .Concat(QueryCapabilityConstant.Tokens.OneCharacterOperators)
            .ToList();

        // Custom operators start with '#'
        if (@operator.StartsWith(QueryCapabilityConstant.Tokens.Operator.Custom))
        {
            return Result.Ok();
        }

        if (!allOperators.Contains(@operator))
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.FilterInvalidOperator(parameterName, @operator));
        }

        return Result.Ok();
    }

    #endregion

    #region Order

    /// <summary>
    /// Validates the number of order-by fields.
    /// </summary>
    /// <param name="fieldCount">The field count to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateOrderFieldCount(
        int fieldCount,
        string parameterName = "orderBy")
    {
        if (fieldCount < 0)
        {
            return Result.Ok();
        }

        if (fieldCount > QueryCapabilityConstant.Order.MaxFields)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.OrderTooManyFields(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates an order direction keyword.
    /// </summary>
    /// <param name="direction">The direction to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateOrderDirection(
        string? direction,
        string parameterName = "orderBy")
    {
        if (direction is null || direction.Length == 0)
        {
            return Result.Ok();
        }

        var lower = direction.ToLowerInvariant();
        if (lower != QueryCapabilityConstant.Tokens.Keyword.Ascending &&
            lower != QueryCapabilityConstant.Tokens.Keyword.Descending)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.OrderInvalidDirection(parameterName, direction));
        }

        return Result.Ok();
    }

    #endregion

    #region Select / Projection

    /// <summary>
    /// Validates the number of projected columns.
    /// </summary>
    /// <param name="columnCount">The column count to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateSelectColumnCount(
        int columnCount,
        string parameterName = "select")
    {
        if (columnCount < 0)
        {
            return Result.Ok();
        }

        if (columnCount > QueryCapabilityConstant.Select.MaxColumns)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.SelectTooManyColumns(parameterName));
        }

        return Result.Ok();
    }

    #endregion

    #region Group

    /// <summary>
    /// Validates the number of group-by keys.
    /// </summary>
    /// <param name="keyCount">The key count to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateGroupKeyCount(
        int keyCount,
        string parameterName = "groupBy")
    {
        if (keyCount < 0)
        {
            return Result.Ok();
        }

        if (keyCount > QueryCapabilityConstant.Group.MaxKeys)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.GroupTooManyKeys(parameterName));
        }

        return Result.Ok();
    }

    #endregion

    #region Aggregate

    /// <summary>
    /// Validates the number of aggregate expressions.
    /// </summary>
    /// <param name="count">The aggregate count to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateAggregateCount(
        int count,
        string parameterName = "aggregate")
    {
        if (count < 0)
        {
            return Result.Ok();
        }

        if (count > QueryCapabilityConstant.Aggregate.MaxCount)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.AggregateTooMany(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates an aggregate function name.
    /// </summary>
    /// <param name="function">The function name to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateAggregateFunction(
        string? function,
        string parameterName = "aggregate")
    {
        if (function is null || function.Length == 0)
        {
            return Result.Ok();
        }

        var validFunctions = new[]
        {
            QueryCapabilityConstant.Functions.Sum,
            QueryCapabilityConstant.Functions.Average,
            QueryCapabilityConstant.Functions.Minimum,
            QueryCapabilityConstant.Functions.Maximum,
            QueryCapabilityConstant.Functions.Count,
            QueryCapabilityConstant.Functions.DistinctCount,
        };

        if (!validFunctions.Contains(function, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.AggregateInvalidFunction(parameterName, function));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates an aggregate expression against the expected pattern.
    /// </summary>
    /// <param name="expression">The expression to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateAggregateExpression(
        string? expression,
        string parameterName = "aggregate")
    {
        if (expression is null || expression.Length == 0)
        {
            return Result.Ok();
        }

        var regex = new System.Text.RegularExpressions.Regex(QueryCapabilityConstant.Functions.AggregatePattern);
        if (!regex.IsMatch(expression))
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.AggregateInvalidExpression(parameterName, expression));
        }

        return Result.Ok();
    }

    #endregion

    #region Search

    /// <summary>
    /// Validates a search text length.
    /// </summary>
    /// <param name="search">The search text to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateSearchLength(
        string? search,
        string parameterName = "search")
    {
        if (search is null || search.Length == 0)
        {
            return Result.Ok();
        }

        if (search.Length > QueryCapabilityConstant.Search.MaxLength)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.SearchTooLong(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates the number of search terms.
    /// </summary>
    /// <param name="termCount">The term count to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateSearchTermCount(
        int termCount,
        string parameterName = "search")
    {
        if (termCount < 0)
        {
            return Result.Ok();
        }

        if (termCount > QueryCapabilityConstant.Search.MaxTerms)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.SearchTooManyTerms(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates the number of search fields.
    /// </summary>
    /// <param name="fieldCount">The field count to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateSearchFieldCount(
        int fieldCount,
        string parameterName = "search")
    {
        if (fieldCount < 0)
        {
            return Result.Ok();
        }

        if (fieldCount > QueryCapabilityConstant.Search.MaxFields)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.SearchTooManyFields(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates a search mode name.
    /// </summary>
    /// <param name="mode">The mode to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateSearchMode(
        string? mode,
        string parameterName = "search")
    {
        if (mode is null || mode.Length == 0)
        {
            return Result.Ok();
        }

        var validModes = new[]
        {
            QueryCapabilityConstant.Tokens.SearchMode.Contains,
            QueryCapabilityConstant.Tokens.SearchMode.StartsWith,
            QueryCapabilityConstant.Tokens.SearchMode.Exact,
            QueryCapabilityConstant.Tokens.SearchMode.FullText,
            QueryCapabilityConstant.Tokens.SearchMode.Fuzzy,
        };

        if (!validModes.Contains(mode, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.SearchInvalidMode(parameterName, mode));
        }

        return Result.Ok();
    }

    #endregion

    #region Map

    /// <summary>
    /// Validates a mapped field name length.
    /// </summary>
    /// <param name="fieldName">The field name to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateMapFieldNameLength(
        string? fieldName,
        string parameterName = "fieldName")
    {
        if (fieldName is null || fieldName.Length == 0)
        {
            return Result.Ok();
        }

        if (fieldName.Length > QueryCapabilityConstant.Map.MaxFieldNameLength)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.MapFieldNameTooLong(parameterName));
        }

        return Result.Ok();
    }

    #endregion

    #region Capability Support

    /// <summary>
    /// Validates that a capability is supported by the provider.
    /// </summary>
    /// <param name="capabilityName">The human-readable name of the capability.</param>
    /// <param name="isSupported">Whether the capability is supported.</param>
    /// <param name="providerName">The optional provider name for error context.</param>
    /// <returns>A successful Result if supported; otherwise a failed Result with validation error.</returns>
    public static Result ValidateCapabilitySupported(
        string capabilityName,
        bool isSupported,
        string? providerName = null)
    {
        if (!isSupported)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.CapabilityNotSupported(capabilityName, providerName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that a search mode is supported by the provider.
    /// </summary>
    /// <param name="supportedModes">The modes supported by the provider.</param>
    /// <param name="requestedMode">The mode requested by the caller.</param>
    /// <param name="providerName">The optional provider name for error context.</param>
    /// <returns>A successful Result if supported; otherwise a failed Result with validation error.</returns>
    public static Result ValidateSearchModeSupported(
        SearchModeSupport supportedModes,
        SearchModeSupport requestedMode,
        string? providerName = null)
    {
        if ((supportedModes & requestedMode) == 0)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.SearchModeNotSupported(requestedMode.ToString(), providerName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that a page size does not exceed the provider's maximum.
    /// </summary>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="maxPageSize">The provider's maximum page size.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if within limits; otherwise a failed Result with validation error.</returns>
    public static Result ValidatePageSizeWithinProviderLimit(
        int pageSize,
        int? maxPageSize,
        string parameterName = "pageSize")
    {
        if (maxPageSize is null)
        {
            return Result.Ok();
        }

        if (pageSize > maxPageSize.Value)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.PageSizeExceedsProviderLimit(pageSize, maxPageSize.Value, parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that an expression depth does not exceed the provider's maximum.
    /// </summary>
    /// <param name="depth">The expression depth.</param>
    /// <param name="maxDepth">The provider's maximum expression depth.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if within limits; otherwise a failed Result with validation error.</returns>
    public static Result ValidateExpressionDepthWithinProviderLimit(
        int depth,
        int? maxDepth,
        string parameterName = "expression")
    {
        if (maxDepth is null)
        {
            return Result.Ok();
        }

        if (depth > maxDepth.Value)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.ExpressionDepthExceedsProviderLimit(depth, maxDepth.Value, parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that the order-by clause count does not exceed the provider's maximum.
    /// </summary>
    /// <param name="count">The number of order-by clauses.</param>
    /// <param name="maxClauses">The provider's maximum order-by clauses.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if within limits; otherwise a failed Result with validation error.</returns>
    public static Result ValidateOrderByClausesWithinProviderLimit(
        int count,
        int? maxClauses,
        string parameterName = "orderBy")
    {
        if (maxClauses is null)
        {
            return Result.Ok();
        }

        if (count > maxClauses.Value)
        {
            return Result.Fail(QueryCapabilityOutcome.Failure.OrderByClausesExceedProviderLimit(count, maxClauses.Value, parameterName));
        }

        return Result.Ok();
    }

    #endregion
}
