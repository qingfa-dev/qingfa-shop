namespace Core.Unit.Tests.BuildingBlocks.Functional.Results;

/// <summary>Tests for the Combine extension in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultCombineSpec
{
    #region Combination

    /// <summary>Combine should succeed for empty and all-success input.</summary>
    [Fact]
    public void Combine_ShouldSucceedForEmptyAndAllSuccessInput()
    {
        // Act & Assert
        ResultExtension.Combine().IsSuccess.Should().BeTrue();
        ResultExtension.Combine(Result.Ok(), Result<int>.Ok(1)).IsSuccess.Should().BeTrue();
    }

    /// <summary>Combine should report a null element.</summary>
    [Fact]
    public void CombineWithNullElement_ShouldReportResultMissing()
    {
        // Act
        var combined = ResultExtension.Combine((Result)null!);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.Errors[0].Code.Should().Be("result.flow.result_missing");
    }

    /// <summary>Combine should merge same-status failures.</summary>
    [Fact]
    public void CombineWithSameStatusFailures_ShouldMergeErrors()
    {
        // Arrange
        var first = Result.Fail(ResultStub.NotFoundError());
        var second = Result.Fail(ResultStub.NotFoundError());

        // Act
        var combined = ResultExtension.Combine(first, second);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        combined.Errors.Count.Should().Be(2);
    }

    /// <summary>Combine should report mixed failure statuses.</summary>
    [Fact]
    public void CombineWithMixedStatusFailures_ShouldReportMixed()
    {
        // Arrange
        var first = Result.Fail(ResultStub.NotFoundError());
        var second = Result.Fail(ResultStub.ConflictError());

        // Act
        var combined = ResultExtension.Combine(first, second);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.Errors[0].Code.Should().Be("result.errors.mixed_status_codes");
    }

    #endregion
}
