namespace Core.Unit.Tests.Common.Monad.Results;

/// <summary>Tests for implicit conversion operators on <see cref="Result"/>.</summary>
[Trait("Category", "Unit")]
public class ResultImplicitSpec
{
    #region Non-generic Failure

    /// <summary>
    /// Error and error arrays should implicitly convert to non-generic failures.
    /// </summary>
    [Theory]
    [MemberData(nameof(NonGenericFailureCases))]
    public void Errors_ShouldImplicitlyConvertToNonGenericFailure(
        bool singleError,
        Error[] errors)
    {
        // Act
        Result result;

        if (singleError)
        {
            result = errors[0];
        }
        else
        {
            result = errors;
        }

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(errors);
        result.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    public static TheoryData<bool, Error[]> NonGenericFailureCases()
    {
        var data = new TheoryData<bool, Error[]>
        {
            { true, [ResultStub.NotFoundError()] },
            { false, [ResultStub.NotFoundError()] },
            {
                false,
                [
                    ResultStub.NotFoundError(),
                    ResultStub.NotFoundError()
                ]
            }
        };

        return data;
    }

    #endregion

    #region Error Collection

    /// <summary>
    /// Error collections should use the Fail factory for non-generic results.
    /// </summary>
    [Fact]
    public void ErrorCollection_ShouldUseFailFactoryForNonGenericResults()
    {
        // Arrange
        List<Error> errors = [ResultStub.NotFoundError()];

        // Act
        Result result = Result.Fail(errors);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(errors);
        result.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    #endregion

    #region Implicit List Conversion

    /// <summary>
    /// Error lists should implicitly convert to non-generic failures.
    /// </summary>
    [Fact]
    public void ErrorList_ShouldImplicitlyConvertToNonGenericFailure()
    {
        // Arrange
        List<Error> errors = [ResultStub.NotFoundError()];

        // Act
        Result result = errors;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(errors);
        result.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
    }

    #endregion
}