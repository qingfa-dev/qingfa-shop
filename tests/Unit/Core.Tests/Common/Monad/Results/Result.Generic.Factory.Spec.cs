namespace Core.UnitTests.Common.Monad.Results;

/// <summary>Tests for <see cref="Result{TValue}"/> factory methods.</summary>
[Trait("Category", "Unit")]
public class ResultGenericFactorySpec
{
    #region Success

    /// <summary>Success factories should use their standard status.</summary>
    [Theory]
    [InlineData(ResultConstant.StatusCode.Ok, 0)]
    [InlineData(ResultConstant.StatusCode.Created, 1)]
    [InlineData(ResultConstant.StatusCode.Accepted, 2)]
    public void SuccessFactories_ShouldUseTheirStandardStatus(int expected, int factory)
    {
        // Act
        var result = factory switch
        {
            0 => Result<int>.Ok(1),
            1 => Result<int>.Created(2),
            2 => Result<int>.Accepted(3),
            _ => throw new ArgumentOutOfRangeException(nameof(factory)),
        };

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(expected);
    }

    /// <summary>Success with explicit status should use the provided status.</summary>
    [Fact]
    public void SuccessWithExplicitStatus_ShouldUseProvidedStatus()
    {
        // Act
        var result = Result<int>.Ok(1, ResultConstant.StatusCode.Accepted);

        // Assert
        result.StatusCode.Should().Be(ResultConstant.StatusCode.Accepted);
        result.Value.Should().Be(1);
    }

    #endregion

    #region Failure

    /// <summary>Failure without explicit status should resolve status from error.</summary>
    [Fact]
    public void FailureWithoutExplicitStatus_ShouldResolveStatusFromError()
    {
        // Arrange
        var result = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        result.TryGetValue(out _).Should().BeFalse();
    }

    /// <summary>Failure with explicit status should use the provided status.</summary>
    [Fact]
    public void FailureWithExplicitStatus_ShouldUseProvidedStatus()
    {
        // Arrange
        var result = Result<int>.Fail(
            new List<Error> { ResultStub.NotFoundError() },
            ResultConstant.StatusCode.InternalServerError);

        // Assert
        result.StatusCode.Should().Be(ResultConstant.StatusCode.InternalServerError);
    }

    /// <summary>Factory validation should reject invalid input.</summary>
    [Theory]
    [InlineData("success-with-failure-status")]
    [InlineData("failure-with-success-status")]
    [InlineData("failure-with-empty-errors")]
    public void FactoryValidation_ShouldRejectInvalidInput(string scenario)
    {
        // Act
        Action act = scenario switch
        {
            "success-with-failure-status"
                => () => { _ = Result<int>.Success(1, ResultConstant.StatusCode.NotFound); }
            ,
            "failure-with-success-status"
                => () => { _ = Result<int>.Fail(new List<Error> { ResultStub.NotFoundError() }, ResultConstant.StatusCode.Ok); }
            ,
            "failure-with-empty-errors"
                => () => { _ = Result.Fail(new List<Error>()); }
            ,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>Failure enumerable overloads should create failures.</summary>
    [Fact]
    public void FailureEnumerableOverloads_ShouldCreateFailures()
    {
        // Arrange
        var errors = new List<Error> { ResultStub.NotFoundError() };

        // Act
        var resolved = Result<string>.Failure(errors);
        var explicitStatus = Result<string>.Failure(errors, ResultConstant.StatusCode.ServiceUnavailable);

        // Assert
        // Assert
        resolved.IsFailure.Should().BeTrue();
        resolved.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);

        explicitStatus.IsFailure.Should().BeTrue();
        explicitStatus.StatusCode.Should().Be(ResultConstant.StatusCode.ServiceUnavailable);
        explicitStatus.Errors.Should().Equal(errors);
    }

    /// <summary>Generic fail with non-list enumerable should materialize errors.</summary>
    [Fact]
    public void GenericFailWithNonListEnumerable_ShouldMaterializeErrors()
    {
        // Act
        var result = Result<string>.Fail(Enumerate(ResultStub.NotFoundError()).ToList());

        // Assert
        result.Errors.Count.Should().Be(1);
        result.Errors[0].Code.Should().Be("test.not_found");
    }

    #endregion

    #region Custom

    /// <summary>Custom factory without errors should keep success defaults.</summary>
    [Fact]
    public void CustomWithoutErrors_ShouldKeepSuccessDefaults()
    {
        // Act
        var result = Result<int>.Custom(isSuccess: true, value: 42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Errors.Should().BeEmpty();
        result.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
    }

    /// <summary>Custom factory with errors should forward status and materialize errors.</summary>
    [Fact]
    public void CustomWithErrors_ShouldForwardStatusAndMaterializeErrors()
    {
        // Arrange
        var errors = new List<Error> { ResultStub.NotFoundError() };

        // Act
        var result = Result<int>.Custom(
            isSuccess: false,
            value: 0,
            errors: errors,
            statusCode: ResultConstant.StatusCode.NotFound);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeSameAs(errors);
        result.Errors.Should().Equal(errors);
        result.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    #endregion

    private static IEnumerable<Error> Enumerate(Error error)
    {
        yield return error;
    }
}