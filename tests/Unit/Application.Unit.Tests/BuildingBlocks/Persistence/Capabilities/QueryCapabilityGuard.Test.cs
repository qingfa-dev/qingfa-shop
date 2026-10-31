using Application.BuildingBlocks.Persistence.Capabilities;

namespace Application.Unit.Tests.BuildingBlocks.Persistence.Capabilities;

[Trait("Category", "Unit")]
public sealed class QueryCapabilityGuardTest
{
    #region ValidateFilterLength

    [Theory(DisplayName = "ValidateFilterLength Should Accept Valid Lengths")]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("short filter")]
    public void ValidateFilterLength_ShouldAcceptValidLengths(string filter)
    {
        var result = QueryCapabilityGuard.ValidateFilterLength(filter);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateFilterLength Should Accept Null")]
    public void ValidateFilterLength_ShouldAcceptNull()
    {
        var result = QueryCapabilityGuard.ValidateFilterLength(null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateFilterLength Should Accept MaxLength")]
    public void ValidateFilterLength_ShouldAcceptMaxLength()
    {
        var filter = new string('a', QueryCapabilityConstant.Filter.MaxLength);
        var result = QueryCapabilityGuard.ValidateFilterLength(filter);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateFilterLength Should Reject Over MaxLength")]
    public void ValidateFilterLength_ShouldRejectOverMaxLength()
    {
        var filter = new string('a', QueryCapabilityConstant.Filter.MaxLength + 1);
        var result = QueryCapabilityGuard.ValidateFilterLength(filter);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.FilterTooLong);
    }

    [Fact(DisplayName = "ValidateFilterLength Should Use Custom Parameter Name")]
    public void ValidateFilterLength_ShouldUseCustomParameterName()
    {
        var filter = new string('a', QueryCapabilityConstant.Filter.MaxLength + 1);
        var result = QueryCapabilityGuard.ValidateFilterLength(filter, "customFilter");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("customFilter"));
    }

    #endregion

    #region ValidateFilterDepth

    [Theory(DisplayName = "ValidateFilterDepth Should Accept Valid Depths")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(32)]  // MaxDepth
    public void ValidateFilterDepth_ShouldAcceptValidDepths(int depth)
    {
        var result = QueryCapabilityGuard.ValidateFilterDepth(depth);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateFilterDepth Should Reject Depths Above Maximum")]
    [InlineData(33)]  // MaxDepth is 32
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void ValidateFilterDepth_ShouldRejectDepthsAboveMaximum(int depth)
    {
        var result = QueryCapabilityGuard.ValidateFilterDepth(depth);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.FilterTooDeep);
    }

    [Fact(DisplayName = "ValidateFilterDepth Should Accept Negative Depth")]
    public void ValidateFilterDepth_ShouldAcceptNegativeDepth()
    {
        var result = QueryCapabilityGuard.ValidateFilterDepth(-1);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ValidateFilterConditionCount

    [Theory(DisplayName = "ValidateFilterConditionCount Should Accept Valid Counts")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]  // MaxConditions
    public void ValidateFilterConditionCount_ShouldAcceptValidCounts(int count)
    {
        var result = QueryCapabilityGuard.ValidateFilterConditionCount(count);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateFilterConditionCount Should Reject Counts Above Maximum")]
    [InlineData(101)]  // MaxConditions is 100
    [InlineData(200)]
    [InlineData(int.MaxValue)]
    public void ValidateFilterConditionCount_ShouldRejectCountsAboveMaximum(int count)
    {
        var result = QueryCapabilityGuard.ValidateFilterConditionCount(count);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.FilterTooManyConditions);
    }

    #endregion

    #region ValidateFilterOperator

    [Theory(DisplayName = "ValidateFilterOperator Should Accept Valid Operators")]
    [InlineData("=")]
    [InlineData("!=")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData(">=")]
    [InlineData("<=")]
    [InlineData("=*")]
    [InlineData("!*")]
    [InlineData("^")]
    [InlineData("!^")]
    [InlineData("$")]
    [InlineData("!$")]
    public void ValidateFilterOperator_ShouldAcceptValidOperators(string op)
    {
        var result = QueryCapabilityGuard.ValidateFilterOperator(op);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateFilterOperator Should Accept Null")]
    public void ValidateFilterOperator_ShouldAcceptNull()
    {
        var result = QueryCapabilityGuard.ValidateFilterOperator(null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateFilterOperator Should Accept Empty String")]
    public void ValidateFilterOperator_ShouldAcceptEmptyString()
    {
        var result = QueryCapabilityGuard.ValidateFilterOperator("");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateFilterOperator Should Accept Custom Operators")]
    public void ValidateFilterOperator_ShouldAcceptCustomOperators()
    {
        var result = QueryCapabilityGuard.ValidateFilterOperator("#custom");
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateFilterOperator Should Reject Invalid Operators")]
    [InlineData("??")]
    [InlineData("@@")]
    [InlineData("~~")]
    public void ValidateFilterOperator_ShouldRejectInvalidOperators(string op)
    {
        var result = QueryCapabilityGuard.ValidateFilterOperator(op);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.FilterInvalidOperator);
    }

    #endregion

    #region ValidateOrderFieldCount

    [Theory(DisplayName = "ValidateOrderFieldCount Should Accept Valid Counts")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]  // MaxFields
    public void ValidateOrderFieldCount_ShouldAcceptValidCounts(int count)
    {
        var result = QueryCapabilityGuard.ValidateOrderFieldCount(count);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateOrderFieldCount Should Reject Counts Above Maximum")]
    [InlineData(9)]  // MaxFields is 8
    [InlineData(16)]
    [InlineData(int.MaxValue)]
    public void ValidateOrderFieldCount_ShouldRejectCountsAboveMaximum(int count)
    {
        var result = QueryCapabilityGuard.ValidateOrderFieldCount(count);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.OrderTooManyFields);
    }

    #endregion

    #region ValidateOrderDirection

    [Theory(DisplayName = "ValidateOrderDirection Should Accept Valid Directions")]
    [InlineData("asc")]
    [InlineData("ASC")]
    [InlineData("Asc")]
    [InlineData("desc")]
    [InlineData("DESC")]
    [InlineData("Desc")]
    public void ValidateOrderDirection_ShouldAcceptValidDirections(string direction)
    {
        var result = QueryCapabilityGuard.ValidateOrderDirection(direction);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateOrderDirection Should Accept Null")]
    public void ValidateOrderDirection_ShouldAcceptNull()
    {
        var result = QueryCapabilityGuard.ValidateOrderDirection(null);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateOrderDirection Should Reject Invalid Directions")]
    [InlineData("ascending")]
    [InlineData("descending")]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("invalid")]
    public void ValidateOrderDirection_ShouldRejectInvalidDirections(string direction)
    {
        var result = QueryCapabilityGuard.ValidateOrderDirection(direction);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.OrderInvalidDirection);
    }

    #endregion

    #region ValidateSelectColumnCount

    [Theory(DisplayName = "ValidateSelectColumnCount Should Accept Valid Counts")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(32)]  // MaxColumns
    public void ValidateSelectColumnCount_ShouldAcceptValidCounts(int count)
    {
        var result = QueryCapabilityGuard.ValidateSelectColumnCount(count);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateSelectColumnCount Should Reject Counts Above Maximum")]
    [InlineData(33)]  // MaxColumns is 32
    [InlineData(64)]
    [InlineData(int.MaxValue)]
    public void ValidateSelectColumnCount_ShouldRejectCountsAboveMaximum(int count)
    {
        var result = QueryCapabilityGuard.ValidateSelectColumnCount(count);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.SelectTooManyColumns);
    }

    #endregion

    #region ValidateGroupKeyCount

    [Theory(DisplayName = "ValidateGroupKeyCount Should Accept Valid Counts")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]  // MaxKeys
    public void ValidateGroupKeyCount_ShouldAcceptValidCounts(int count)
    {
        var result = QueryCapabilityGuard.ValidateGroupKeyCount(count);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateGroupKeyCount Should Reject Counts Above Maximum")]
    [InlineData(9)]  // MaxKeys is 8
    [InlineData(16)]
    [InlineData(int.MaxValue)]
    public void ValidateGroupKeyCount_ShouldRejectCountsAboveMaximum(int count)
    {
        var result = QueryCapabilityGuard.ValidateGroupKeyCount(count);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.GroupTooManyKeys);
    }

    #endregion

    #region ValidateAggregateCount

    [Theory(DisplayName = "ValidateAggregateCount Should Accept Valid Counts")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(16)]  // MaxCount
    public void ValidateAggregateCount_ShouldAcceptValidCounts(int count)
    {
        var result = QueryCapabilityGuard.ValidateAggregateCount(count);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateAggregateCount Should Reject Counts Above Maximum")]
    [InlineData(17)]  // MaxCount is 16
    [InlineData(32)]
    [InlineData(int.MaxValue)]
    public void ValidateAggregateCount_ShouldRejectCountsAboveMaximum(int count)
    {
        var result = QueryCapabilityGuard.ValidateAggregateCount(count);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.AggregateTooMany);
    }

    #endregion

    #region ValidateAggregateFunction

    [Theory(DisplayName = "ValidateAggregateFunction Should Accept Valid Functions")]
    [InlineData("sum")]
    [InlineData("SUM")]
    [InlineData("avg")]
    [InlineData("AVG")]
    [InlineData("min")]
    [InlineData("max")]
    [InlineData("count")]
    [InlineData("distinctcount")]
    public void ValidateAggregateFunction_ShouldAcceptValidFunctions(string function)
    {
        var result = QueryCapabilityGuard.ValidateAggregateFunction(function);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateAggregateFunction Should Accept Null")]
    public void ValidateAggregateFunction_ShouldAcceptNull()
    {
        var result = QueryCapabilityGuard.ValidateAggregateFunction(null);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateAggregateFunction Should Reject Invalid Functions")]
    [InlineData("invalid")]
    [InlineData("median")]
    [InlineData("mode")]
    [InlineData("stddev")]
    public void ValidateAggregateFunction_ShouldRejectInvalidFunctions(string function)
    {
        var result = QueryCapabilityGuard.ValidateAggregateFunction(function);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.AggregateInvalidFunction);
    }

    #endregion

    #region ValidateAggregateExpression

    [Theory(DisplayName = "ValidateAggregateExpression Should Accept Valid Expressions")]
    [InlineData("sum(price)")]
    [InlineData("avg(quantity)")]
    [InlineData("min(date)")]
    [InlineData("max(score)")]
    [InlineData("count(id)")]
    [InlineData("distinctcount(email)")]
    public void ValidateAggregateExpression_ShouldAcceptValidExpressions(string expression)
    {
        var result = QueryCapabilityGuard.ValidateAggregateExpression(expression);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateAggregateExpression Should Accept Null")]
    public void ValidateAggregateExpression_ShouldAcceptNull()
    {
        var result = QueryCapabilityGuard.ValidateAggregateExpression(null);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateAggregateExpression Should Reject Invalid Expressions")]
    [InlineData("invalid(price)")]
    [InlineData("sum")]
    [InlineData("sum()")]
    [InlineData("sum(price")]
    [InlineData("sum price)")]
    public void ValidateAggregateExpression_ShouldRejectInvalidExpressions(string expression)
    {
        var result = QueryCapabilityGuard.ValidateAggregateExpression(expression);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.AggregateInvalidExpression);
    }

    #endregion

    #region ValidateSearchLength

    [Theory(DisplayName = "ValidateSearchLength Should Accept Valid Lengths")]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("short search")]
    public void ValidateSearchLength_ShouldAcceptValidLengths(string search)
    {
        var result = QueryCapabilityGuard.ValidateSearchLength(search);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateSearchLength Should Accept Null")]
    public void ValidateSearchLength_ShouldAcceptNull()
    {
        var result = QueryCapabilityGuard.ValidateSearchLength(null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateSearchLength Should Accept MaxLength")]
    public void ValidateSearchLength_ShouldAcceptMaxLength()
    {
        var search = new string('a', QueryCapabilityConstant.Search.MaxLength);
        var result = QueryCapabilityGuard.ValidateSearchLength(search);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateSearchLength Should Reject Over MaxLength")]
    public void ValidateSearchLength_ShouldRejectOverMaxLength()
    {
        var search = new string('a', QueryCapabilityConstant.Search.MaxLength + 1);
        var result = QueryCapabilityGuard.ValidateSearchLength(search);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.SearchTooLong);
    }

    #endregion

    #region ValidateSearchTermCount

    [Theory(DisplayName = "ValidateSearchTermCount Should Accept Valid Counts")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]  // MaxTerms
    public void ValidateSearchTermCount_ShouldAcceptValidCounts(int count)
    {
        var result = QueryCapabilityGuard.ValidateSearchTermCount(count);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateSearchTermCount Should Reject Counts Above Maximum")]
    [InlineData(9)]  // MaxTerms is 8
    [InlineData(16)]
    [InlineData(int.MaxValue)]
    public void ValidateSearchTermCount_ShouldRejectCountsAboveMaximum(int count)
    {
        var result = QueryCapabilityGuard.ValidateSearchTermCount(count);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.SearchTooManyTerms);
    }

    #endregion

    #region ValidateSearchFieldCount

    [Theory(DisplayName = "ValidateSearchFieldCount Should Accept Valid Counts")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]  // MaxFields
    public void ValidateSearchFieldCount_ShouldAcceptValidCounts(int count)
    {
        var result = QueryCapabilityGuard.ValidateSearchFieldCount(count);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateSearchFieldCount Should Reject Counts Above Maximum")]
    [InlineData(9)]  // MaxFields is 8
    [InlineData(16)]
    [InlineData(int.MaxValue)]
    public void ValidateSearchFieldCount_ShouldRejectCountsAboveMaximum(int count)
    {
        var result = QueryCapabilityGuard.ValidateSearchFieldCount(count);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.SearchTooManyFields);
    }

    #endregion

    #region ValidateSearchMode

    [Theory(DisplayName = "ValidateSearchMode Should Accept Valid Modes")]
    [InlineData("contains")]
    [InlineData("CONTAINS")]
    [InlineData("startswith")]
    [InlineData("exact")]
    [InlineData("fulltext")]
    [InlineData("fuzzy")]
    public void ValidateSearchMode_ShouldAcceptValidModes(string mode)
    {
        var result = QueryCapabilityGuard.ValidateSearchMode(mode);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateSearchMode Should Accept Null")]
    public void ValidateSearchMode_ShouldAcceptNull()
    {
        var result = QueryCapabilityGuard.ValidateSearchMode(null);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateSearchMode Should Reject Invalid Modes")]
    [InlineData("invalid")]
    [InlineData("regex")]
    [InlineData("wildcard")]
    public void ValidateSearchMode_ShouldRejectInvalidModes(string mode)
    {
        var result = QueryCapabilityGuard.ValidateSearchMode(mode);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.SearchInvalidMode);
    }

    #endregion

    #region ValidateMapFieldNameLength

    [Theory(DisplayName = "ValidateMapFieldNameLength Should Accept Valid Lengths")]
    [InlineData("id")]
    [InlineData("name")]
    [InlineData("created_at")]
    public void ValidateMapFieldNameLength_ShouldAcceptValidLengths(string fieldName)
    {
        var result = QueryCapabilityGuard.ValidateMapFieldNameLength(fieldName);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateMapFieldNameLength Should Accept Null")]
    public void ValidateMapFieldNameLength_ShouldAcceptNull()
    {
        var result = QueryCapabilityGuard.ValidateMapFieldNameLength(null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateMapFieldNameLength Should Accept MaxLength")]
    public void ValidateMapFieldNameLength_ShouldAcceptMaxLength()
    {
        var fieldName = new string('a', QueryCapabilityConstant.Map.MaxFieldNameLength);
        var result = QueryCapabilityGuard.ValidateMapFieldNameLength(fieldName);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateMapFieldNameLength Should Reject Over MaxLength")]
    public void ValidateMapFieldNameLength_ShouldRejectOverMaxLength()
    {
        var fieldName = new string('a', QueryCapabilityConstant.Map.MaxFieldNameLength + 1);
        var result = QueryCapabilityGuard.ValidateMapFieldNameLength(fieldName);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.MapFieldNameTooLong);
    }

    #endregion

    #region ValidateCapabilitySupported

    [Fact(DisplayName = "ValidateCapabilitySupported Should Accept Supported Capability")]
    public void ValidateCapabilitySupported_ShouldAcceptSupportedCapability()
    {
        var result = QueryCapabilityGuard.ValidateCapabilitySupported("streaming", true);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateCapabilitySupported Should Reject Unsupported Capability")]
    public void ValidateCapabilitySupported_ShouldRejectUnsupportedCapability()
    {
        var result = QueryCapabilityGuard.ValidateCapabilitySupported("streaming", false, "Sqlite");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.CapabilityNotSupported);
        result.Errors.Should().ContainSingle(e => e.Message.Contains("streaming"));
        result.Errors.Should().ContainSingle(e => e.Message.Contains("Sqlite"));
    }

    #endregion

    #region ValidateSearchModeSupported

    [Fact(DisplayName = "ValidateSearchModeSupported Should Accept Supported Mode")]
    public void ValidateSearchModeSupported_ShouldAcceptSupportedMode()
    {
        var result = QueryCapabilityGuard.ValidateSearchModeSupported(
            SearchModeSupport.Contains | SearchModeSupport.Exact,
            SearchModeSupport.Contains);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateSearchModeSupported Should Accept Multiple Flags")]
    public void ValidateSearchModeSupported_ShouldAcceptMultipleFlags()
    {
        var result = QueryCapabilityGuard.ValidateSearchModeSupported(
            SearchModeSupport.All,
            SearchModeSupport.Contains | SearchModeSupport.Fuzzy);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateSearchModeSupported Should Reject Unsupported Mode")]
    public void ValidateSearchModeSupported_ShouldRejectUnsupportedMode()
    {
        var result = QueryCapabilityGuard.ValidateSearchModeSupported(
            SearchModeSupport.Contains,
            SearchModeSupport.Fuzzy,
            "Sqlite");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.SearchModeNotSupported);
    }

    [Fact(DisplayName = "ValidateSearchModeSupported Should Reject When None Supported")]
    public void ValidateSearchModeSupported_ShouldRejectWhenNoneSupported()
    {
        var result = QueryCapabilityGuard.ValidateSearchModeSupported(
            SearchModeSupport.None,
            SearchModeSupport.Contains);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.SearchModeNotSupported);
    }

    #endregion

    #region ValidatePageSizeWithinProviderLimit

    [Fact(DisplayName = "ValidatePageSizeWithinProviderLimit Should Accept When Within Limit")]
    public void ValidatePageSizeWithinProviderLimit_ShouldAcceptWhenWithinLimit()
    {
        var result = QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(50, 100);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidatePageSizeWithinProviderLimit Should Accept When Equal To Limit")]
    public void ValidatePageSizeWithinProviderLimit_ShouldAcceptWhenEqualToLimit()
    {
        var result = QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(100, 100);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidatePageSizeWithinProviderLimit Should Accept When Max Is Null")]
    public void ValidatePageSizeWithinProviderLimit_ShouldAcceptWhenMaxIsNull()
    {
        var result = QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(9999, null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidatePageSizeWithinProviderLimit Should Reject When Exceeds Limit")]
    public void ValidatePageSizeWithinProviderLimit_ShouldRejectWhenExceedsLimit()
    {
        var result = QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(150, 100);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.PageSizeExceedsProviderLimit);
    }

    #endregion

    #region ValidateExpressionDepthWithinProviderLimit

    [Fact(DisplayName = "ValidateExpressionDepthWithinProviderLimit Should Accept When Within Limit")]
    public void ValidateExpressionDepthWithinProviderLimit_ShouldAcceptWhenWithinLimit()
    {
        var result = QueryCapabilityGuard.ValidateExpressionDepthWithinProviderLimit(10, 32);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateExpressionDepthWithinProviderLimit Should Accept When Max Is Null")]
    public void ValidateExpressionDepthWithinProviderLimit_ShouldAcceptWhenMaxIsNull()
    {
        var result = QueryCapabilityGuard.ValidateExpressionDepthWithinProviderLimit(9999, null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateExpressionDepthWithinProviderLimit Should Reject When Exceeds Limit")]
    public void ValidateExpressionDepthWithinProviderLimit_ShouldRejectWhenExceedsLimit()
    {
        var result = QueryCapabilityGuard.ValidateExpressionDepthWithinProviderLimit(50, 32);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.ExpressionDepthExceedsProviderLimit);
    }

    #endregion

    #region ValidateOrderByClausesWithinProviderLimit

    [Fact(DisplayName = "ValidateOrderByClausesWithinProviderLimit Should Accept When Within Limit")]
    public void ValidateOrderByClausesWithinProviderLimit_ShouldAcceptWhenWithinLimit()
    {
        var result = QueryCapabilityGuard.ValidateOrderByClausesWithinProviderLimit(4, 8);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateOrderByClausesWithinProviderLimit Should Accept When Max Is Null")]
    public void ValidateOrderByClausesWithinProviderLimit_ShouldAcceptWhenMaxIsNull()
    {
        var result = QueryCapabilityGuard.ValidateOrderByClausesWithinProviderLimit(9999, null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateOrderByClausesWithinProviderLimit Should Reject When Exceeds Limit")]
    public void ValidateOrderByClausesWithinProviderLimit_ShouldRejectWhenExceedsLimit()
    {
        var result = QueryCapabilityGuard.ValidateOrderByClausesWithinProviderLimit(16, 8);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.OrderByClausesExceedProviderLimit);
    }

    #endregion
}
