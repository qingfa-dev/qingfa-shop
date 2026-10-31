using Application.BuildingBlocks.Persistence.UnitOfWorks;

namespace Application.Unit.Tests.BuildingBlocks.Persistence.UnitOfWorks;

[Trait("Category", "Unit")]
public sealed class TransactionOutcomeTest
{
    #region Failure Properties

    public static TheoryData<string, string, object[]> FailureTestCases()
    {
        var data = new TheoryData<string, string, object[]>
        {
            { "CommandTimeoutTooSmall", TransactionOutcome.Codes.CommandTimeoutTooSmall, new object[] { "testParam" } },
            { "CommandTimeoutTooLarge", TransactionOutcome.Codes.CommandTimeoutTooLarge, new object[] { "testParam" } },
            { "MaxRetryAttemptsNegative", TransactionOutcome.Codes.MaxRetryAttemptsNegative, new object[] { "testParam" } },
            { "MaxRetryAttemptsExceedsMax", TransactionOutcome.Codes.MaxRetryAttemptsExceedsMax, new object[] { "testParam" } },
            { "RetryBaseDelayNegative", TransactionOutcome.Codes.RetryBaseDelayNegative, new object[] { "testParam" } },
            { "RetryBaseDelayExceedsMax", TransactionOutcome.Codes.RetryBaseDelayExceedsMax, new object[] { "testParam" } },
            { "PoolMinSizeNegative", TransactionOutcome.Codes.PoolMinSizeNegative, new object[] { "testParam" } },
            { "PoolMaxSizeNegative", TransactionOutcome.Codes.PoolMaxSizeNegative, new object[] { "testParam" } },
            { "PoolMaxSizeExceedsMax", TransactionOutcome.Codes.PoolMaxSizeExceedsMax, new object[] { "testParam" } },
            { "PoolMinExceedsMax", TransactionOutcome.Codes.PoolMinExceedsMax, new object[] { "minParam", "maxParam" } },
            { "IsolationLevelInvalid", TransactionOutcome.Codes.IsolationLevelInvalid, new object[] { "testParam" } },
            { "StateNull", TransactionOutcome.Codes.StateNull, new object[] { "testParam" } },
            { "StateInvalid", TransactionOutcome.Codes.StateInvalid, new object[] { "testParam" } },
        };
        return data;
    }

    [Theory(DisplayName = "Failure Properties Should Return Errors With Correct Codes")]
    [MemberData(nameof(FailureTestCases))]
    public void Failure_ShouldReturnErrorWithCorrectCode(string propertyName, string expectedCode, object[] parameters)
    {
        // Arrange
        var property = typeof(TransactionOutcome.Failure)
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
        var error = TransactionOutcome.Failure.CommandTimeoutTooSmall("test");

        // Assert
        error.Severity.Should().Be(ErrorSeverity.Warning);
        error.Status.Should().Be(422); // Validation status
    }

    [Fact(DisplayName = "IsolationLevelInvalid Should Include All Valid Levels In Message")]
    public void IsolationLevelInvalid_ShouldIncludeAllValidLevelsInMessage()
    {
        // Act
        var error = TransactionOutcome.Failure.IsolationLevelInvalid("testLevel");

        // Assert
        error.Message.Should().Contain(TransactionConstant.IsolationLevel.ReadUncommitted);
        error.Message.Should().Contain(TransactionConstant.IsolationLevel.ReadCommitted);
        error.Message.Should().Contain(TransactionConstant.IsolationLevel.RepeatableRead);
        error.Message.Should().Contain(TransactionConstant.IsolationLevel.Serializable);
        error.Message.Should().Contain(TransactionConstant.IsolationLevel.Snapshot);
        error.Message.Should().Contain(TransactionConstant.IsolationLevel.Unspecified);
    }

    [Fact(DisplayName = "StateInvalid Should Include All Valid States In Message")]
    public void StateInvalid_ShouldIncludeAllValidStatesInMessage()
    {
        // Act
        var error = TransactionOutcome.Failure.StateInvalid("testState");

        // Assert
        error.Message.Should().Contain(TransactionConstant.State.Active);
        error.Message.Should().Contain(TransactionConstant.State.Committed);
        error.Message.Should().Contain(TransactionConstant.State.RolledBack);
        error.Message.Should().Contain(TransactionConstant.State.RollbackOnly);
        error.Message.Should().Contain(TransactionConstant.State.Failed);
    }

    #endregion

    #region Codes

    [Fact(DisplayName = "Codes Should Be Unique")]
    public void Codes_ShouldBeUnique()
    {
        // Arrange
        var codes = typeof(TransactionOutcome.Codes)
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
        var codes = typeof(TransactionOutcome.Codes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().AllSatisfy(c => c.Should().StartWith("Transaction."));
    }

    #endregion
}