namespace Core.Unit.Tests.BuildingBlocks.Functional.Results;

/// <summary>Tests for <see cref="ResultGuard"/> validation methods.</summary>
[Trait("Category", "Unit")]
public class ResultGuardSpec
{
    #region ValidateStatusCode

    /// <summary>ValidateStatusCode should reject out-of-range and wrong-kind statuses and accept valid ones.</summary>
    [Theory]
    [InlineData(99, true, "result.status.invalid_status_code")]
    [InlineData(600, false, "result.status.invalid_status_code")]
    [InlineData(100, true, "result.status.invalid_success_status")]
    [InlineData(199, true, "result.status.invalid_success_status")]
    [InlineData(300, true, "result.status.invalid_success_status")]
    [InlineData(599, true, "result.status.invalid_success_status")]
    [InlineData(404, true, "result.status.invalid_success_status")]
    [InlineData(200, false, "result.status.invalid_failure_status")]
    [InlineData(399, false, "result.status.invalid_failure_status")]
    [InlineData(200, true, null)]
    [InlineData(201, true, null)]
    [InlineData(299, true, null)]
    [InlineData(400, false, null)]
    [InlineData(404, false, null)]
    [InlineData(599, false, null)]
    public void ValidateStatusCode_ShouldEnforceRangeAndResultStateRules(
        int statusCode,
        bool isSuccess,
        string? expectedCode)
    {
        // Act
        var error = ResultGuard.ValidateStatusCode(statusCode, isSuccess);

        // Assert
        if (expectedCode is null)
        {
            error.Should().BeNull();
        }
        else
        {
            error.Should().NotBeNull();
            error.Code.Should().Be(expectedCode);
        }
    }

    #endregion

    #region ValidateErrors

    /// <summary>ValidateErrors should require errors only for failures and enforce the count limit.</summary>
    [Theory]
    [MemberData(nameof(ValidateErrorsCases))]
    public void ValidateErrors_ShouldRequireErrorsOnlyForFailuresAndEnforceLimit(
        int errorCount,
        bool isSuccess,
        string? expectedCode)
    {
        // Arrange
        var errors = Enumerable.Repeat(ResultStub.NotFoundError(), errorCount).ToList();

        // Act
        var error = ResultGuard.ValidateErrors(errors, isSuccess);

        // Assert
        if (expectedCode is null)
        {
            error.Should().BeNull();
        }
        else
        {
            error.Should().NotBeNull();
            error.Code.Should().Be(expectedCode);
        }
    }

    public static TheoryData<int, bool, string?> ValidateErrorsCases()
    {
        var data = new TheoryData<int, bool, string?>();
        data.Add(0, false, "result.errors.empty");
        data.Add(0, true, null);
        data.Add(ResultConstant.Constraint.Errors.MaxCount, false, null);
        data.Add(ResultConstant.Constraint.Errors.MaxCount + 1, false, "result.errors.exceeds_max_count");
        return data;
    }

    #endregion

    #region ValidateValue

    /// <summary>ValidateValue should require a value for success and allow failures without one.</summary>
    [Theory]
    [MemberData(nameof(ValidateValueCases))]
    public void ValidateValue_ShouldEnforceSuccessAndFailureValueRules(
        bool isSuccess,
        string? value,
        string? expectedCode)
    {
        // Act
        var error = ResultGuard.ValidateValue<string>(isSuccess, value!);

        // Assert
        if (expectedCode is null)
        {
            error.Should().BeNull();
        }
        else
        {
            error.Should().NotBeNull();
            error.Code.Should().Be(expectedCode);
        }
    }

    public static TheoryData<bool, string?, string?> ValidateValueCases()
    {
        var data = new TheoryData<bool, string?, string?>();
        data.Add(true, null, "result.value.required");
        data.Add(false, "not allowed", null);
        data.Add(false, null, null);
        data.Add(true, "value", null);
        return data;
    }

    #endregion

    #region ValidateResultConsistency

    /// <summary>ValidateResultConsistency should reject mixed failure codes and accept consistent ones.</summary>
    [Theory]
    [MemberData(nameof(ValidateResultConsistencyCases))]
    public void ValidateResultConsistency_ShouldEnforceFailureAndSuccessRules(
        bool isSuccess,
        List<Error> errors,
        string? expectedCode)
    {
        // Act
        var error = ResultGuard.ValidateResultConsistency(isSuccess, errors);

        // Assert
        if (expectedCode is null)
        {
            error.Should().BeNull();
        }
        else
        {
            error.Should().NotBeNull();
            error.Code.Should().Be(expectedCode);
        }
    }

    public static TheoryData<bool, List<Error>, string?> ValidateResultConsistencyCases()
    {
        var data = new TheoryData<bool, List<Error>, string?>();
        data.Add(false, new List<Error>(), null);
        data.Add(
            false,
            new List<Error> { ResultStub.NotFoundError(), ResultStub.ConflictError() },
            ResultOutcome.Failure.Errors.MixedStatusCodes.Code);
        data.Add(false, new List<Error> { ResultStub.NotFoundError(), ResultStub.NotFoundError() }, null);
        data.Add(true, new List<Error>(), null);
        return data;
    }

    #endregion
}