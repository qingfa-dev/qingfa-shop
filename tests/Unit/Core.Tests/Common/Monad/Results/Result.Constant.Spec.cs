namespace Core.UnitTests.Common.Monad.Results;

/// <summary>Tests for <see cref="ResultConstant"/> constraint and default values.</summary>
[Trait("Category", "Unit")]
public class ResultConstantSpec
{
    #region Constraints

    /// <summary>Constraints should expose status and collection limits.</summary>
    [Theory]
    [InlineData("Constraint.Status.Min", ResultConstant.Constraint.Status.Min, 100)]
    [InlineData("Constraint.Status.Max", ResultConstant.Constraint.Status.Max, 599)]
    [InlineData("Constraint.Status.Success.Min", ResultConstant.Constraint.Status.Success.Min, 200)]
    [InlineData("Constraint.Status.Success.Max", ResultConstant.Constraint.Status.Success.Max, 299)]
    [InlineData("Constraint.Status.Failure.Min", ResultConstant.Constraint.Status.Failure.Min, 400)]
    [InlineData("Constraint.Status.Failure.Max", ResultConstant.Constraint.Status.Failure.Max, 599)]
    [InlineData("Constraint.Errors.MaxCount", ResultConstant.Constraint.Errors.MaxCount, 50)]
    [InlineData("Constraint.Metadata.MaxEntries", ResultConstant.Constraint.Metadata.MaxEntries, 50)]
    public void Constraints_ShouldExposeStatusAndCollectionLimits(string name, int actual, int expected)
    {
        // Act & Assert
        actual.Should().Be(expected, name);
    }

    #endregion

    #region Status Codes

    /// <summary>Status codes should match their HTTP values.</summary>
    [Theory]
    [InlineData(ResultConstant.StatusCode.Ok, 200)]
    [InlineData(ResultConstant.StatusCode.Created, 201)]
    [InlineData(ResultConstant.StatusCode.Accepted, 202)]
    [InlineData(ResultConstant.StatusCode.NoContent, 204)]
    [InlineData(ResultConstant.StatusCode.NotFound, 404)]
    [InlineData(ResultConstant.StatusCode.InternalServerError, 500)]
    public void StatusCodes_ShouldMatchTheirHttpValues(int actual, int expected)
    {
        // Act & Assert
        actual.Should().Be(expected);
    }

    #endregion

    #region Defaults

    /// <summary>Defaults should expose internal server error status and empty errors.</summary>
    [Fact]
    public void Default_ShouldExposeInternalServerErrorStatusAndEmptyErrors()
    {
        // Act & Assert
        ResultConstant.Default.FailureStatus.Should().Be(500);
        ResultConstant.Default.EmptyErrors.Should().BeEmpty();
    }

    #endregion

    #region Failure Constants

    /// <summary>Failure constants should provide stable codes and messages.</summary>
    [Fact]
    public void FailureConstants_ShouldProvideStableCodesAndMessages()
    {
        // Act & Assert
        ResultOutcome.Failure.Errors.MixedStatusCodes.Code.Should().Be("result.errors.mixed_status_codes");
        ResultOutcome.Failure.Errors.MixedStatusCodes.Message.Should().NotBeNullOrWhiteSpace();
        ResultOutcome.Failure.Value.Required.Code.Should().Be("result.value.required");
        ResultOutcome.Failure.Status.InvalidFailureStatus(500).Message.Should().Contain("500");
    }

    /// <summary>Exception mapping missing should carry a stable code and message.</summary>
    [Fact]
    public void ExceptionMappingMissing_ShouldCarryAStableCodeAndMessage()
    {
        // Arrange
        var error = ResultOutcome.Failure.Flow.ExceptionMappingMissing(nameof(ResultExtension));

        // Act & Assert
        error.Code.Should().Be("result.flow.exception_mapping_missing");
        error.Message.Should().Contain(nameof(ResultExtension));
    }

    #endregion
}