namespace Core.UnitTests.Common.Monad.Results;

/// <summary>Tests for value extraction extensions in <see cref="ResultExtension"/>.</summary>
[Trait("Category", "Unit")]
public class ResultExtractionSpec
{
    #region Extraction

    /// <summary>ValueOr should return the value or the fallback.</summary>
    [Fact]
    public void ValueOr_ShouldReturnValueOrFallback()
    {
        // Arrange
        var success = Result<int>.Ok(5);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.ValueOr(9).Should().Be(5);
        failure.ValueOr(9).Should().Be(9);
        failure.ValueOr().Should().Be(default);
    }

    /// <summary>ValueOrThrow should return the value or throw a ResultException.</summary>
    [Fact]
    public void ValueOrThrow_ShouldReturnValueOrThrow()
    {
        // Arrange
        var success = Result<int>.Ok(5);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.ValueOrThrow().Should().Be(5);

        Action act = () =>
        {
            _ = failure.ValueOrThrow();
        };

        var exception = act.Should().Throw<ResultException>();
        exception.Which.Errors[0].Code.Should().Be("test.not_found");
        exception.Which.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    /// <summary>ThrowIfFailure should pass successes and throw on failures.</summary>
    [Fact]
    public void ThrowIfFailure_ShouldPassSuccessAndThrowOnFailure()
    {
        // Arrange
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());
        var genericSuccess = Result<int>.Ok(1);
        var genericFailure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.ThrowIfFailure().Should().BeSameAs(success);
        genericSuccess.ThrowIfFailure().Should().BeSameAs(genericSuccess);

        Action failureAction = () => failure.ThrowIfFailure();
        failureAction.Should().Throw<ResultException>();

        Action genericFailureAction = () => genericFailure.ThrowIfFailure();
        genericFailureAction.Should().Throw<ResultException>();
    }

    #endregion
}
