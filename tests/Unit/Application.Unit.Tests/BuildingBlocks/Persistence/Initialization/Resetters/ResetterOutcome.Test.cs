using Application.BuildingBlocks.Persistence.Initialization.Resetters;

namespace Application.Unit.Tests.BuildingBlocks.Persistence.Initialization.Resetters;

[Trait("Category", "Unit")]
public sealed class ResetterOutcomeTest
{
    #region Failure Properties

    public static TheoryData<string, string, object[]> FailureTestCases()
    {
        var data = new TheoryData<string, string, object[]>
        {
            { "OrderNegative", ResetterOutcome.Codes.OrderNegative, new object[] { "testParam" } },
            { "CountNegative", ResetterOutcome.Codes.CountNegative, new object[] { "testParam" } },
            { "CountExceedsMax", ResetterOutcome.Codes.CountExceedsMax, new object[] { "testParam" } },
            { "ScopeNull", ResetterOutcome.Codes.ScopeNull, new object[] { "testParam" } },
            { "ScopeInvalid", ResetterOutcome.Codes.ScopeInvalid, new object[] { "testParam" } },
        };
        return data;
    }

    [Theory(DisplayName = "Failure Properties Should Return Errors With Correct Codes")]
    [MemberData(nameof(FailureTestCases))]
    public void Failure_ShouldReturnErrorWithCorrectCode(string propertyName, string expectedCode, object[] parameters)
    {
        // Arrange
        var property = typeof(ResetterOutcome.Failure)
            .GetMethod(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        property.Should().NotBeNull($"Failure.{propertyName} should exist");

        // Act
        var error = (Error)property!.Invoke(null, parameters)!;

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
        var error = ResetterOutcome.Failure.OrderNegative("test");

        // Assert
        error.Severity.Should().Be(ErrorSeverity.Warning);
        error.Status.Should().Be(422); // Validation status
    }

    [Fact(DisplayName = "ScopeInvalid Should Include Valid Scopes In Message")]
    public void ScopeInvalid_ShouldIncludeValidScopesInMessage()
    {
        // Act
        var error = ResetterOutcome.Failure.ScopeInvalid("testScope");

        // Assert
        error.Message.Should().Contain(ResetterConstant.Scope.Data);
        error.Message.Should().Contain(ResetterConstant.Scope.Schema);
        error.Message.Should().Contain(ResetterConstant.Scope.All);
    }

    #endregion

    #region Codes

    [Fact(DisplayName = "Codes Should Be Unique")]
    public void Codes_ShouldBeUnique()
    {
        // Arrange
        var codes = typeof(ResetterOutcome.Codes)
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
        var codes = typeof(ResetterOutcome.Codes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().AllSatisfy(c => c.Should().StartWith("Resetter."));
    }

    #endregion
}