namespace Core.Unit.Tests.BuildingBlocks.Functional.Results;

/// <summary>Tests for <see cref="IResult{TError}"/> and <see cref="IResult{TValue,TError}"/> contracts.</summary>
[Trait("Category", "Unit")]
public class ResultInterfaceSpec
{
    #region Non-generic

    /// <summary>Non-generic Result should implement IResult.</summary>
    [Fact]
    public void NonGenericResult_ShouldImplementIResult()
    {
        // Arrange
        IResult<Error> result = Result.Ok();

        // Act & Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.HasValue.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
    }

    #endregion

    #region Generic

    /// <summary>Generic Result should implement IResult and IResult of value.</summary>
    [Theory]
    [InlineData(42)]
    [InlineData(0)]
    [InlineData(-1)]
    public void GenericResult_ShouldImplementIResultAndIResultOfValue(int expectedValue)
    {
        // Arrange
        IResult<int, Error> result = Result<int>.Ok(expectedValue);

        // Act & Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedValue);
        result.TryGetValue(out var value).Should().BeTrue();
        value.Should().Be(expectedValue);
    }

    /// <summary>Generic failure should expose failed state through IResult contract.</summary>
    [Fact]
    public void GenericFailure_ShouldExposeFailedStateThroughIResultContract()
    {
        // Arrange
        IResult<int, Error> result = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        result.IsFailure.Should().BeTrue();
        result.HasValue.Should().BeFalse();
        result.TryGetValue(out var value).Should().BeFalse();
        value.Should().Be(default);
        result.Errors.Should().ContainSingle();
    }

    #endregion
}