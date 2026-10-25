namespace Core.Tests.Common.Monad.Results;

/// <summary>Tests for <see cref="Result"/> factory methods.</summary>
[Trait("Category", "Unit")]
public class ResultFactorySpec
{
    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(202)]
    [InlineData(204)]
    public void SuccessFactory_ShouldReturnExpectedStatus(int statusCode)
    {
        // Act
        var result = statusCode switch
        {
            200 => Result.Ok(),
            201 => Result.Created(),
            202 => Result.Accepted(),
            204 => Result.NoContent(),
            _ => throw new ArgumentOutOfRangeException(nameof(statusCode)),
        };

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(statusCode);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SuccessWithExplicitStatus_ShouldUseProvidedStatus()
    {
        // Act
        var result = Result.Success(ResultConstant.StatusCode.Accepted);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(ResultConstant.StatusCode.Accepted);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void FailureWithoutExplicitStatus_ShouldResolveStatusFromError()
    {
        // Arrange
        var error = ResultStub.NotFoundError();

        // Act
        var result = Result.Fail(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void FailureWithExplicitStatus_ShouldUseProvidedStatus()
    {
        // Arrange
        var error = ResultStub.NotFoundError();
        var expectedStatus = ResultConstant.StatusCode.InternalServerError;

        // Act
        var result = Result.Fail(
            new List<Error> { error },
            expectedStatus);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(expectedStatus);
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Theory]
    [InlineData("success-with-failure-status")]
    [InlineData("failure-with-success-status")]
    [InlineData("failure-with-empty-errors")]
    public void FactoryWithInvalidInput_ShouldThrowArgumentException(string scenario)
    {
        // Arrange
        Action act = scenario switch
        {
            "success-with-failure-status" =>
                () => { _ = Result.Success(ResultConstant.StatusCode.NotFound); }
            ,

            "failure-with-success-status" =>
                () =>
                {
                    _ = Result.Fail(
                    new List<Error> { ResultStub.NotFoundError() },
                    ResultConstant.StatusCode.Ok);
                }
            ,

            "failure-with-empty-errors" =>
                () => { _ = Result.Fail(new List<Error>()); }
            ,

            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        // Act & Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FailureEnumerableOverloads_ShouldCreateFailures()
    {
        // Arrange
        var error = ResultStub.NotFoundError();
        var errors = new List<Error> { error };

        // Act
        var resolved = Result.Failure(errors);

        var explicitStatus = Result.Failure(
            errors,
            ResultConstant.StatusCode.ServiceUnavailable);

        // Assert: default status is derived from the error
        resolved.IsFailure.Should().BeTrue();
        resolved.StatusCode.Should()
            .Be(ResultConstant.StatusCode.NotFound);

        resolved.Errors.Should().BeEquivalentTo(errors);
        resolved.Errors[0].Should().BeSameAs(error);

        // Assert: explicit status is preserved
        explicitStatus.IsFailure.Should().BeTrue();
        explicitStatus.StatusCode.Should()
            .Be(ResultConstant.StatusCode.ServiceUnavailable);

        explicitStatus.Errors.Should().BeEquivalentTo(errors);
        explicitStatus.Errors[0].Should().BeSameAs(error);
    }

    [Fact]
    public void FailWithEnumerable_ShouldMaterializeErrors()
    {
        // Arrange
        var error = ResultStub.NotFoundError();
        IEnumerable<Error> errors = Enumerate(error);

        // Act
        var result = Result.Failure(errors.ToList());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("test.not_found");
    }

    [Fact]
    public void Failure_WhenNoErrorCarriesStatus_ShouldDefaultToInternalServerError()
    {
        // Arrange
        var critical = Error.Custom(
            "test.critical",
            "critical",
            severity: ErrorSeverity.Critical);
        var info = Error.Custom(
            "test.info",
            "info",
            severity: ErrorSeverity.Info);

        // Act
        var result = Result.Fail(critical, info);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(ResultConstant.StatusCode.InternalServerError);
    }

    [Fact]
    public void FailureWithMixedStatusErrors_ShouldThrowArgumentException()
    {
        // Arrange
        var withStatus = ResultStub.NotFoundError();
        var criticalNoStatus = Error.Custom(
            "test.critical",
            "critical",
            severity: ErrorSeverity.Critical);
        var infoNoStatus = Error.Custom(
            "test.info",
            "info",
            severity: ErrorSeverity.Info);

        // Act
        var act = () => Result.Fail(withStatus, criticalNoStatus, infoNoStatus);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage($"*{ResultOutcome.Failure.Errors.MixedStatusCodes.Code}*");
    }

    [Fact]
    public void CustomSuccessWithoutExplicitStatus_ShouldUseOkStatus()
    {
        // Act
        var result = Result.Custom(isSuccess: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CustomFailureWithoutExplicitStatus_ShouldResolveStatusFromErrors()
    {
        // Arrange
        var error = ResultStub.NotFoundError();

        // Act
        var result = Result.Custom(
            isSuccess: false,
            errors: new List<Error> { error });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void CustomFailureWithoutErrors_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Result.Custom(isSuccess: false);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private static IEnumerable<Error> Enumerate(Error error)
    {
        yield return error;
    }

}