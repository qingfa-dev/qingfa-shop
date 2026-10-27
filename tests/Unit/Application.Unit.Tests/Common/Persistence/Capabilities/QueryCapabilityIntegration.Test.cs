using Application.Common.Persistence.Capabilities;

namespace Application.Tests.Common.Persistence.Capabilities;

[Trait("Category", "Unit")]
public sealed class QueryCapabilityIntegrationTest
{
    #region Test Implementations

    /// <summary>Full-featured provider that supports everything.</summary>
    private sealed class FullCapabilities : IQueryCapabilities
    {
        public bool SupportsExpressions => true;
        public bool SupportsGrouping => true;
        public bool SupportsAggregates => true;
        public bool SupportsJoins => true;
        public bool SupportsSubqueries => true;
        public bool SupportsDistinct => true;
        public bool SupportsUnion => true;
        public bool SupportsCaseInsensitive => true;
        public bool SupportsStreaming => true;
        public bool SupportsProjection => true;
        public bool SupportsChangeTracking => true;

        public SearchModeSupport SearchModes => SearchModeSupport.All;
        public bool SupportsRanking => true;
        public bool SupportsHighlighting => true;
        public int? MaxSearchTerms => 8;
        public int? MaxSearchFields => 8;

        public int? MaxPageSize => 1000;
        public int? MaxExpressionDepth => 64;
        public int? MaxOrderByClauses => 16;
    }

    /// <summary>Minimal provider with limited capabilities (e.g., SQLite).</summary>
    private sealed class MinimalCapabilities : IQueryCapabilities
    {
        public bool SupportsExpressions => true;
        public bool SupportsGrouping => true;
        public bool SupportsAggregates => true;
        public bool SupportsJoins => false;
        public bool SupportsSubqueries => false;
        public bool SupportsDistinct => true;
        public bool SupportsUnion => false;
        public bool SupportsCaseInsensitive => true;
        public bool SupportsStreaming => false;
        public bool SupportsProjection => true;
        public bool SupportsChangeTracking => false;

        public SearchModeSupport SearchModes => SearchModeSupport.Contains | SearchModeSupport.StartsWith | SearchModeSupport.Exact;
        public bool SupportsRanking => false;
        public bool SupportsHighlighting => false;
        public int? MaxSearchTerms => 4;
        public int? MaxSearchFields => 4;

        public int? MaxPageSize => 100;
        public int? MaxExpressionDepth => 16;
        public int? MaxOrderByClauses => 4;
    }

    /// <summary>No-op provider that supports nothing.</summary>
    private sealed class EmptyCapabilities : IQueryCapabilities
    {
        public bool SupportsExpressions => false;
        public bool SupportsGrouping => false;
        public bool SupportsAggregates => false;
        public bool SupportsJoins => false;
        public bool SupportsSubqueries => false;
        public bool SupportsDistinct => false;
        public bool SupportsUnion => false;
        public bool SupportsCaseInsensitive => false;
        public bool SupportsStreaming => false;
        public bool SupportsProjection => false;
        public bool SupportsChangeTracking => false;

        public SearchModeSupport SearchModes => SearchModeSupport.None;
        public bool SupportsRanking => false;
        public bool SupportsHighlighting => false;
        public int? MaxSearchTerms => null;
        public int? MaxSearchFields => null;

        public int? MaxPageSize => null;
        public int? MaxExpressionDepth => null;
        public int? MaxOrderByClauses => null;
    }

    #endregion

    #region Full Capabilities

    [Fact(DisplayName = "Full Capabilities Should Pass All Guard Checks")]
    public void FullCapabilities_ShouldPassAllGuardChecks()
    {
        var caps = new FullCapabilities();

        // All shape capabilities should pass
        QueryCapabilityGuard.ValidateCapabilitySupported("Expressions", caps.SupportsExpressions).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateCapabilitySupported("Grouping", caps.SupportsGrouping).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateCapabilitySupported("Aggregates", caps.SupportsAggregates).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateCapabilitySupported("Joins", caps.SupportsJoins).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateCapabilitySupported("Subqueries", caps.SupportsSubqueries).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateCapabilitySupported("Distinct", caps.SupportsDistinct).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateCapabilitySupported("Union", caps.SupportsUnion).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateCapabilitySupported("CaseInsensitive", caps.SupportsCaseInsensitive).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateCapabilitySupported("Streaming", caps.SupportsStreaming).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateCapabilitySupported("Projection", caps.SupportsProjection).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateCapabilitySupported("ChangeTracking", caps.SupportsChangeTracking).IsSuccess.Should().BeTrue();

        // All search modes should pass
        QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, SearchModeSupport.Contains).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, SearchModeSupport.Fuzzy).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, SearchModeSupport.FullText).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Full Capabilities Should Accept Large Page Sizes")]
    public void FullCapabilities_ShouldAcceptLargePageSizes()
    {
        var caps = new FullCapabilities();

        var result = QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(500, caps.MaxPageSize);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Minimal Capabilities

    [Fact(DisplayName = "Minimal Capabilities Should Reject Unsupported Features")]
    public void MinimalCapabilities_ShouldRejectUnsupportedFeatures()
    {
        var caps = new MinimalCapabilities();

        var joinsResult = QueryCapabilityGuard.ValidateCapabilitySupported("Joins", caps.SupportsJoins, "Sqlite");
        joinsResult.IsFailure.Should().BeTrue();
        joinsResult.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.CapabilityNotSupported);

        var streamingResult = QueryCapabilityGuard.ValidateCapabilitySupported("Streaming", caps.SupportsStreaming, "Sqlite");
        streamingResult.IsFailure.Should().BeTrue();
        streamingResult.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.CapabilityNotSupported);
    }

    [Fact(DisplayName = "Minimal Capabilities Should Reject Unsupported Search Modes")]
    public void MinimalCapabilities_ShouldRejectUnsupportedSearchModes()
    {
        var caps = new MinimalCapabilities();

        var fuzzyResult = QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, SearchModeSupport.Fuzzy, "Sqlite");
        fuzzyResult.IsFailure.Should().BeTrue();
        fuzzyResult.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.SearchModeNotSupported);

        var fullTextResult = QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, SearchModeSupport.FullText, "Sqlite");
        fullTextResult.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Minimal Capabilities Should Accept Supported Search Modes")]
    public void MinimalCapabilities_ShouldAcceptSupportedSearchModes()
    {
        var caps = new MinimalCapabilities();

        QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, SearchModeSupport.Contains).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, SearchModeSupport.StartsWith).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, SearchModeSupport.Exact).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Minimal Capabilities Should Enforce Provider Limits")]
    public void MinimalCapabilities_ShouldEnforceProviderLimits()
    {
        var caps = new MinimalCapabilities();

        // Page size exceeds provider limit
        var pageSizeResult = QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(200, caps.MaxPageSize);
        pageSizeResult.IsFailure.Should().BeTrue();
        pageSizeResult.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.PageSizeExceedsProviderLimit);

        // Expression depth exceeds provider limit
        var depthResult = QueryCapabilityGuard.ValidateExpressionDepthWithinProviderLimit(32, caps.MaxExpressionDepth);
        depthResult.IsFailure.Should().BeTrue();
        depthResult.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.ExpressionDepthExceedsProviderLimit);

        // Order-by clauses exceed provider limit
        var orderByResult = QueryCapabilityGuard.ValidateOrderByClausesWithinProviderLimit(8, caps.MaxOrderByClauses);
        orderByResult.IsFailure.Should().BeTrue();
        orderByResult.Errors.Should().ContainSingle(e => e.Code == QueryCapabilityOutcome.Codes.OrderByClausesExceedProviderLimit);
    }

    #endregion

    #region Empty Capabilities

    [Fact(DisplayName = "Empty Capabilities Should Reject All Features")]
    public void EmptyCapabilities_ShouldRejectAllFeatures()
    {
        var caps = new EmptyCapabilities();

        var features = new[]
        {
            ("Expressions", caps.SupportsExpressions),
            ("Grouping", caps.SupportsGrouping),
            ("Aggregates", caps.SupportsAggregates),
            ("Joins", caps.SupportsJoins),
            ("Subqueries", caps.SupportsSubqueries),
            ("Distinct", caps.SupportsDistinct),
            ("Union", caps.SupportsUnion),
            ("CaseInsensitive", caps.SupportsCaseInsensitive),
            ("Streaming", caps.SupportsStreaming),
            ("Projection", caps.SupportsProjection),
            ("ChangeTracking", caps.SupportsChangeTracking),
        };

        foreach (var (name, supported) in features)
        {
            var result = QueryCapabilityGuard.ValidateCapabilitySupported(name, supported, "Empty");
            result.IsFailure.Should().BeTrue($"Expected {name} to be unsupported");
        }
    }

    [Fact(DisplayName = "Empty Capabilities Should Reject All Search Modes")]
    public void EmptyCapabilities_ShouldRejectAllSearchModes()
    {
        var caps = new EmptyCapabilities();

        var modes = new[]
        {
            SearchModeSupport.Contains,
            SearchModeSupport.StartsWith,
            SearchModeSupport.Exact,
            SearchModeSupport.FullText,
            SearchModeSupport.Fuzzy,
        };

        foreach (var mode in modes)
        {
            var result = QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, mode, "Empty");
            result.IsFailure.Should().BeTrue($"Expected {mode} to be unsupported");
        }
    }

    [Fact(DisplayName = "Empty Capabilities Should Accept Any Provider Limit (Null)")]
    public void EmptyCapabilities_ShouldAcceptAnyProviderLimit()
    {
        var caps = new EmptyCapabilities();

        // Null limits mean no enforcement
        QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(9999, caps.MaxPageSize).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateExpressionDepthWithinProviderLimit(9999, caps.MaxExpressionDepth).IsSuccess.Should().BeTrue();
        QueryCapabilityGuard.ValidateOrderByClausesWithinProviderLimit(9999, caps.MaxOrderByClauses).IsSuccess.Should().BeTrue();
    }

    #endregion

    #region End-to-End Validation Pipeline

    [Fact(DisplayName = "Full Validation Pipeline Should Work for Valid Query")]
    public void FullValidationPipeline_ShouldWorkForValidQuery()
    {
        var caps = new FullCapabilities();

        // Simulate a valid query request
        var filter = "name=John,age>30";
        var search = "hello world";
        var mode = "contains";
        var pageSize = 50;
        var orderByCount = 3;
        var expressionDepth = 5;

        var results = new List<Result>
        {
            QueryCapabilityGuard.ValidateFilterLength(filter),
            QueryCapabilityGuard.ValidateSearchLength(search),
            QueryCapabilityGuard.ValidateSearchMode(mode),
            QueryCapabilityGuard.ValidateCapabilitySupported("Expressions", caps.SupportsExpressions),
            QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, SearchModeSupport.Contains),
            QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(pageSize, caps.MaxPageSize),
            QueryCapabilityGuard.ValidateOrderByClausesWithinProviderLimit(orderByCount, caps.MaxOrderByClauses),
            QueryCapabilityGuard.ValidateExpressionDepthWithinProviderLimit(expressionDepth, caps.MaxExpressionDepth),
        };

        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
    }

    [Fact(DisplayName = "Full Validation Pipeline Should Fail for Invalid Query")]
    public void FullValidationPipeline_ShouldFailForInvalidQuery()
    {
        var caps = new MinimalCapabilities();

        // Simulate an invalid query request that exceeds limits
        var filter = new string('a', QueryCapabilityConstant.Filter.MaxLength + 1);
        var mode = "fuzzy";  // Not supported by minimal
        var pageSize = 200;  // Exceeds provider limit
        var orderByCount = 8;  // Exceeds provider limit
        var expressionDepth = 32;  // Exceeds provider limit

        var results = new List<Result>
        {
            QueryCapabilityGuard.ValidateFilterLength(filter),
            QueryCapabilityGuard.ValidateSearchMode(mode),
            QueryCapabilityGuard.ValidateSearchModeSupported(caps.SearchModes, SearchModeSupport.Fuzzy),
            QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(pageSize, caps.MaxPageSize),
            QueryCapabilityGuard.ValidateOrderByClausesWithinProviderLimit(orderByCount, caps.MaxOrderByClauses),
            QueryCapabilityGuard.ValidateExpressionDepthWithinProviderLimit(expressionDepth, caps.MaxExpressionDepth),
        };

        var failures = results.Where(r => r.IsFailure).ToList();
        failures.Should().HaveCountGreaterThan(0);

        var codes = failures.SelectMany(r => r.Errors).Select(e => e.Code).ToList();
        codes.Should().Contain(QueryCapabilityOutcome.Codes.FilterTooLong);
        codes.Should().Contain(QueryCapabilityOutcome.Codes.SearchModeNotSupported);
        codes.Should().Contain(QueryCapabilityOutcome.Codes.PageSizeExceedsProviderLimit);
        codes.Should().Contain(QueryCapabilityOutcome.Codes.OrderByClausesExceedProviderLimit);
        codes.Should().Contain(QueryCapabilityOutcome.Codes.ExpressionDepthExceedsProviderLimit);
    }

    [Fact(DisplayName = "Aggregate Validation Pipeline Should Work End-to-End")]
    public void AggregateValidationPipeline_ShouldWorkEndToEnd()
    {
        var caps = new FullCapabilities();

        var aggregates = new[] { "sum(price)", "avg(quantity)", "count(id)" };

        var results = new List<Result>
        {
            QueryCapabilityGuard.ValidateCapabilitySupported("Aggregates", caps.SupportsAggregates),
            QueryCapabilityGuard.ValidateAggregateCount(aggregates.Length),
        };

        foreach (var agg in aggregates)
        {
            results.Add(QueryCapabilityGuard.ValidateAggregateExpression(agg));
        }

        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
    }

    [Fact(DisplayName = "Filter Validation Pipeline Should Work End-to-End")]
    public void FilterValidationPipeline_ShouldWorkEndToEnd()
    {
        var filter = "name=John,age>30";
        var operators = new[] { "=", ">" };

        var results = new List<Result>
        {
            QueryCapabilityGuard.ValidateFilterLength(filter),
            QueryCapabilityGuard.ValidateFilterDepth(1),
            QueryCapabilityGuard.ValidateFilterConditionCount(2),
        };

        foreach (var op in operators)
        {
            results.Add(QueryCapabilityGuard.ValidateFilterOperator(op));
        }

        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
    }

    #endregion

    #region Interface Contract

    [Fact(DisplayName = "IQueryCapabilities Should Have All Expected Properties")]
    public void IQueryCapabilities_ShouldHaveAllExpectedProperties()
    {
        var properties = typeof(IQueryCapabilities).GetProperties();

        var expectedNames = new[]
        {
            "SupportsExpressions",
            "SupportsGrouping",
            "SupportsAggregates",
            "SupportsJoins",
            "SupportsSubqueries",
            "SupportsDistinct",
            "SupportsUnion",
            "SupportsCaseInsensitive",
            "SupportsStreaming",
            "SupportsProjection",
            "SupportsChangeTracking",
            "SearchModes",
            "SupportsRanking",
            "SupportsHighlighting",
            "MaxSearchTerms",
            "MaxSearchFields",
            "MaxPageSize",
            "MaxExpressionDepth",
            "MaxOrderByClauses",
        };

        var actualNames = properties.Select(p => p.Name).ToList();
        expectedNames.Should().AllSatisfy(name => actualNames.Should().Contain(name));
    }

    [Fact(DisplayName = "IQueryCapabilities Boolean Properties Should Be Read-Only")]
    public void IQueryCapabilities_BooleanProperties_ShouldBeReadOnly()
    {
        var booleanProperties = typeof(IQueryCapabilities)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(bool))
            .ToList();

        booleanProperties.Should().AllSatisfy(p => p.CanWrite.Should().BeFalse());
    }

    #endregion
}
