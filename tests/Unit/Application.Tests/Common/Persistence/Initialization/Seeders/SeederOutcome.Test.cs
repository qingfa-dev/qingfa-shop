using Application.Common.Persistence.Initialization.Seeders;
using Core.Common.Monad.Results;

namespace Application.Tests.Common.Persistence.Initialization.Seeders;

[Trait("Category", "Unit")]
public sealed class SeederOutcomeTest
{
    #region Failure Properties

    public static TheoryData<string, string, object[]> FailureTestCases()
    {
        var data = new TheoryData<string, string, object[]>
        {
            { "OrderNegative", SeederOutcome.Codes.OrderNegative, new object[] { "testParam" } },
            { "CountNegative", SeederOutcome.Codes.CountNegative, new object[] { "testParam" } },
            { "CountExceedsMax", SeederOutcome.Codes.CountExceedsMax, new object[] { "testParam" } },
            { "BatchSizeTooSmall", SeederOutcome.Codes.BatchSizeTooSmall, new object[] { "testParam" } },
            { "BatchSizeTooLarge", SeederOutcome.Codes.BatchSizeTooLarge, new object[] { "testParam" } },
        };
        return data;
    }

    [Theory(DisplayName = "Failure Properties Should Return Errors With Correct Codes")]
    [MemberData(nameof(FailureTestCases))]
    public void Failure_ShouldReturnErrorWithCorrectCode(string propertyName, string expectedCode, object[] parameters)
    {
        // Arrange
        var property = typeof(SeederOutcome.Failure)
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
        var error = SeederOutcome.Failure.OrderNegative("test");

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
        var codes = typeof(SeederOutcome.Codes)
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
        var codes = typeof(SeederOutcome.Codes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().AllSatisfy(c => c.Should().StartWith("Seeder."));
    }

    #endregion
}