using Application.Common.Persistence.Initialization.Migrations;

namespace Application.Unit.Tests.Common.Persistence.Initialization.Migrations;

[Trait("Category", "Unit")]
public sealed class MigrationOutcomeTest
{
    #region Failure Properties

    public static TheoryData<string, string, object[]> FailureTestCases()
    {
        var data = new TheoryData<string, string, object[]>
        {
            { "NameNull", MigrationOutcome.Codes.NameNull, new object[] { "testParam" } },
            { "NameNullOrWhitespace", MigrationOutcome.Codes.NameNullOrWhitespace, new object[] { "testParam" } },
            { "NameExceedsMaxLength", MigrationOutcome.Codes.NameExceedsMaxLength, new object[] { "testParam" } },
            { "OrderNegative", MigrationOutcome.Codes.OrderNegative, new object[] { "testParam" } },
            { "CountNegative", MigrationOutcome.Codes.CountNegative, new object[] { "testParam" } },
            { "CountExceedsMax", MigrationOutcome.Codes.CountExceedsMax, new object[] { "testParam" } },
            { "VersionNull", MigrationOutcome.Codes.VersionNull, new object[] { "testParam" } },
            { "VersionNullOrWhitespace", MigrationOutcome.Codes.VersionNullOrWhitespace, new object[] { "testParam" } },
            { "VersionInvalidFormat", MigrationOutcome.Codes.VersionInvalidFormat, new object[] { "testParam" } },
            { "ChecksumNull", MigrationOutcome.Codes.ChecksumNull, new object[] { "testParam" } },
            { "ChecksumNullOrWhitespace", MigrationOutcome.Codes.ChecksumNullOrWhitespace, new object[] { "testParam" } },
            { "ChecksumInvalidFormat", MigrationOutcome.Codes.ChecksumInvalidFormat, new object[] { "testParam" } },
        };
        return data;
    }

    [Theory(DisplayName = "Failure Properties Should Return Errors With Correct Codes")]
    [MemberData(nameof(FailureTestCases))]
    public void Failure_ShouldReturnErrorWithCorrectCode(string propertyName, string expectedCode, object[] parameters)
    {
        // Arrange
        var property = typeof(MigrationOutcome.Failure)
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
        var error = MigrationOutcome.Failure.NameNull("test");

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
        var codes = typeof(MigrationOutcome.Codes)
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
        var codes = typeof(MigrationOutcome.Codes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().AllSatisfy(c => c.Should().StartWith("Migration."));
    }

    #endregion
}