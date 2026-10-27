using Application.Common.Persistence.Capabilities;

namespace Application.Tests.Common.Persistence.Capabilities;

[Trait("Category", "Unit")]
public sealed class QueryCapabilityOutcomeTest
{
    #region Failure Properties

    public static TheoryData<string, string, object[]> FailureTestCases()
    {
        var data = new TheoryData<string, string, object[]>
        {
            // ---- Filter ----
            { "FilterTooLong", QueryCapabilityOutcome.Codes.FilterTooLong, new object[] { "testFilter" } },
            { "FilterTooDeep", QueryCapabilityOutcome.Codes.FilterTooDeep, new object[] { "testFilter" } },
            { "FilterTooManyConditions", QueryCapabilityOutcome.Codes.FilterTooManyConditions, new object[] { "testFilter" } },
            { "FilterInvalidOperator", QueryCapabilityOutcome.Codes.FilterInvalidOperator, new object[] { "testFilter", "???" } },
            { "FilterInvalidSyntax", QueryCapabilityOutcome.Codes.FilterInvalidSyntax, new object[] { "testFilter", "unmatched paren" } },

            // ---- Order ----
            { "OrderTooManyFields", QueryCapabilityOutcome.Codes.OrderTooManyFields, new object[] { "testOrder" } },
            { "OrderInvalidField", QueryCapabilityOutcome.Codes.OrderInvalidField, new object[] { "testOrder", "badField" } },
            { "OrderInvalidDirection", QueryCapabilityOutcome.Codes.OrderInvalidDirection, new object[] { "testOrder", "invalid" } },

            // ---- Select ----
            { "SelectTooManyColumns", QueryCapabilityOutcome.Codes.SelectTooManyColumns, new object[] { "testSelect" } },
            { "SelectInvalidColumn", QueryCapabilityOutcome.Codes.SelectInvalidColumn, new object[] { "testSelect", "badCol" } },

            // ---- Group ----
            { "GroupTooManyKeys", QueryCapabilityOutcome.Codes.GroupTooManyKeys, new object[] { "testGroup" } },
            { "GroupInvalidKey", QueryCapabilityOutcome.Codes.GroupInvalidKey, new object[] { "testGroup", "badKey" } },

            // ---- Aggregate ----
            { "AggregateTooMany", QueryCapabilityOutcome.Codes.AggregateTooMany, new object[] { "testAgg" } },
            { "AggregateInvalidFunction", QueryCapabilityOutcome.Codes.AggregateInvalidFunction, new object[] { "testAgg", "badFn" } },
            { "AggregateInvalidExpression", QueryCapabilityOutcome.Codes.AggregateInvalidExpression, new object[] { "testAgg", "badExpr" } },

            // ---- Search ----
            { "SearchTooLong", QueryCapabilityOutcome.Codes.SearchTooLong, new object[] { "testSearch" } },
            { "SearchTooManyTerms", QueryCapabilityOutcome.Codes.SearchTooManyTerms, new object[] { "testSearch" } },
            { "SearchTooManyFields", QueryCapabilityOutcome.Codes.SearchTooManyFields, new object[] { "testSearch" } },
            { "SearchInvalidMode", QueryCapabilityOutcome.Codes.SearchInvalidMode, new object[] { "testSearch", "badMode" } },

            // ---- Map ----
            { "MapFieldNameTooLong", QueryCapabilityOutcome.Codes.MapFieldNameTooLong, new object[] { "testField" } },

            // ---- Capability Support ----
            { "CapabilityNotSupported", QueryCapabilityOutcome.Codes.CapabilityNotSupported, new object[] { "streaming", "Sqlite" } },
            { "SearchModeNotSupported", QueryCapabilityOutcome.Codes.SearchModeNotSupported, new object[] { "fuzzy", "Sqlite" } },
            { "PageSizeExceedsProviderLimit", QueryCapabilityOutcome.Codes.PageSizeExceedsProviderLimit, new object[] { 500, 100, "pageSize" } },
            { "ExpressionDepthExceedsProviderLimit", QueryCapabilityOutcome.Codes.ExpressionDepthExceedsProviderLimit, new object[] { 50, 32, "expression" } },
            { "OrderByClausesExceedProviderLimit", QueryCapabilityOutcome.Codes.OrderByClausesExceedProviderLimit, new object[] { 16, 8, "orderBy" } },
        };
        return data;
    }

    [Theory(DisplayName = "Failure Properties Should Return Errors With Correct Codes")]
    [MemberData(nameof(FailureTestCases))]
    public void Failure_ShouldReturnErrorWithCorrectCode(string propertyName, string expectedCode, object[] parameters)
    {
        // Arrange
        var method = typeof(QueryCapabilityOutcome.Failure)
            .GetMethod(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull($"Failure.{propertyName} should exist");

        // Act
        var error = (Error)method!.Invoke(null, parameters)!;

        // Assert
        error.Code.Should().Be(expectedCode);
        foreach (var param in parameters)
        {
            error.Message.Should().Contain(param.ToString()!);
        }
    }

    [Fact(DisplayName = "Failure Properties Should Return Validation Errors")]
    public void Failure_ShouldReturnValidationErrors()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.FilterTooLong("test");

        // Assert
        error.Severity.Should().Be(ErrorSeverity.Warning);
        error.Status.Should().Be(422); // Validation status
    }

    #endregion

    #region Codes

    [Fact(DisplayName = "Codes Should Be Unique")]
    public void Codes_ShouldBeUnique()
    {
        // Arrange
        var codes = typeof(QueryCapabilityOutcome.Codes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact(DisplayName = "Codes Should Follow Naming Convention")]
    public void Codes_ShouldFollowNamingConvention()
    {
        // Arrange
        var codes = typeof(QueryCapabilityOutcome.Codes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().AllSatisfy(c => c.Should().StartWith("QueryCapability."));
    }

    [Fact(DisplayName = "Codes Should Have Correct Category Prefixes")]
    public void Codes_ShouldHaveCorrectCategoryPrefixes()
    {
        // Arrange
        var codes = typeof(QueryCapabilityOutcome.Codes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().Contain(c => c == QueryCapabilityOutcome.Codes.FilterTooLong);
        codes.Should().Contain(c => c == QueryCapabilityOutcome.Codes.OrderTooManyFields);
        codes.Should().Contain(c => c == QueryCapabilityOutcome.Codes.SelectTooManyColumns);
        codes.Should().Contain(c => c == QueryCapabilityOutcome.Codes.GroupTooManyKeys);
        codes.Should().Contain(c => c == QueryCapabilityOutcome.Codes.AggregateTooMany);
        codes.Should().Contain(c => c == QueryCapabilityOutcome.Codes.SearchTooLong);
        codes.Should().Contain(c => c == QueryCapabilityOutcome.Codes.MapFieldNameTooLong);
        codes.Should().Contain(c => c == QueryCapabilityOutcome.Codes.CapabilityNotSupported);
    }

    #endregion

    #region Filter Error Messages

    [Fact(DisplayName = "FilterTooLong Should Include MaxLength In Message")]
    public void FilterTooLong_ShouldIncludeMaxLengthInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.FilterTooLong("myFilter");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Filter.MaxLength.ToString());
        error.Message.Should().Contain("myFilter");
    }

    [Fact(DisplayName = "FilterTooDeep Should Include MaxDepth In Message")]
    public void FilterTooDeep_ShouldIncludeMaxDepthInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.FilterTooDeep("myFilter");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Filter.MaxDepth.ToString());
    }

    [Fact(DisplayName = "FilterTooManyConditions Should Include MaxConditions In Message")]
    public void FilterTooManyConditions_ShouldIncludeMaxConditionsInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.FilterTooManyConditions("myFilter");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Filter.MaxConditions.ToString());
    }

    [Fact(DisplayName = "FilterInvalidOperator Should Include Operator Name When Provided")]
    public void FilterInvalidOperator_ShouldIncludeOperatorNameWhenProvided()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.FilterInvalidOperator("myFilter", "???");

        // Assert
        error.Message.Should().Contain("???");
    }

    [Fact(DisplayName = "FilterInvalidOperator Should Omit Operator Name When Null")]
    public void FilterInvalidOperator_ShouldOmitOperatorNameWhenNull()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.FilterInvalidOperator("myFilter", null);

        // Assert
        error.Message.Should().NotContain("Operator:");
    }

    #endregion

    #region Order Error Messages

    [Fact(DisplayName = "OrderTooManyFields Should Include MaxFields In Message")]
    public void OrderTooManyFields_ShouldIncludeMaxFieldsInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.OrderTooManyFields("myOrder");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Order.MaxFields.ToString());
    }

    #endregion

    #region Select Error Messages

    [Fact(DisplayName = "SelectTooManyColumns Should Include MaxColumns In Message")]
    public void SelectTooManyColumns_ShouldIncludeMaxColumnsInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.SelectTooManyColumns("mySelect");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Select.MaxColumns.ToString());
    }

    #endregion

    #region Group Error Messages

    [Fact(DisplayName = "GroupTooManyKeys Should Include MaxKeys In Message")]
    public void GroupTooManyKeys_ShouldIncludeMaxKeysInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.GroupTooManyKeys("myGroup");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Group.MaxKeys.ToString());
    }

    #endregion

    #region Aggregate Error Messages

    [Fact(DisplayName = "AggregateTooMany Should Include MaxCount In Message")]
    public void AggregateTooMany_ShouldIncludeMaxCountInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.AggregateTooMany("myAgg");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Aggregate.MaxCount.ToString());
    }

    #endregion

    #region Search Error Messages

    [Fact(DisplayName = "SearchTooLong Should Include MaxLength In Message")]
    public void SearchTooLong_ShouldIncludeMaxLengthInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.SearchTooLong("mySearch");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Search.MaxLength.ToString());
    }

    [Fact(DisplayName = "SearchTooManyTerms Should Include MaxTerms In Message")]
    public void SearchTooManyTerms_ShouldIncludeMaxTermsInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.SearchTooManyTerms("mySearch");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Search.MaxTerms.ToString());
    }

    [Fact(DisplayName = "SearchTooManyFields Should Include MaxFields In Message")]
    public void SearchTooManyFields_ShouldIncludeMaxFieldsInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.SearchTooManyFields("mySearch");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Search.MaxFields.ToString());
    }

    #endregion

    #region Map Error Messages

    [Fact(DisplayName = "MapFieldNameTooLong Should Include MaxFieldNameLength In Message")]
    public void MapFieldNameTooLong_ShouldIncludeMaxFieldNameLengthInMessage()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.MapFieldNameTooLong("myField");

        // Assert
        error.Message.Should().Contain(QueryCapabilityConstant.Map.MaxFieldNameLength.ToString());
    }

    #endregion

    #region Capability Support Error Messages

    [Fact(DisplayName = "CapabilityNotSupported Should Include Capability Name")]
    public void CapabilityNotSupported_ShouldIncludeCapabilityName()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.CapabilityNotSupported("streaming", "Sqlite");

        // Assert
        error.Message.Should().Contain("streaming");
        error.Message.Should().Contain("Sqlite");
    }

    [Fact(DisplayName = "CapabilityNotSupported Should Omit Provider When Null")]
    public void CapabilityNotSupported_ShouldOmitProviderWhenNull()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.CapabilityNotSupported("streaming", null);

        // Assert
        error.Message.Should().Contain("streaming");
        error.Message.Should().NotContain("Provider:");
    }

    [Fact(DisplayName = "SearchModeNotSupported Should Include Mode Name")]
    public void SearchModeNotSupported_ShouldIncludeModeName()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.SearchModeNotSupported("fuzzy", "Sqlite");

        // Assert
        error.Message.Should().Contain("fuzzy");
        error.Message.Should().Contain("Sqlite");
    }

    [Fact(DisplayName = "PageSizeExceedsProviderLimit Should Include Requested And Max Values")]
    public void PageSizeExceedsProviderLimit_ShouldIncludeRequestedAndMaxValues()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.PageSizeExceedsProviderLimit(500, 100, "pageSize");

        // Assert
        error.Message.Should().Contain("500");
        error.Message.Should().Contain("100");
    }

    [Fact(DisplayName = "ExpressionDepthExceedsProviderLimit Should Include Depth And Max Values")]
    public void ExpressionDepthExceedsProviderLimit_ShouldIncludeDepthAndMaxValues()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.ExpressionDepthExceedsProviderLimit(50, 32, "expr");

        // Assert
        error.Message.Should().Contain("50");
        error.Message.Should().Contain("32");
    }

    [Fact(DisplayName = "OrderByClausesExceedProviderLimit Should Include Count And Max Values")]
    public void OrderByClausesExceedProviderLimit_ShouldIncludeCountAndMaxValues()
    {
        // Act
        var error = QueryCapabilityOutcome.Failure.OrderByClausesExceedProviderLimit(16, 8, "orderBy");

        // Assert
        error.Message.Should().Contain("16");
        error.Message.Should().Contain("8");
    }

    #endregion
}
