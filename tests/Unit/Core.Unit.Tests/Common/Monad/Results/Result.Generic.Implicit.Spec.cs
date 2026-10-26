namespace Core.Tests.Common.Monad.Results;

/// <summary>Tests for implicit conversion operators on <see cref="Result{TValue}"/>.</summary>
[Trait("Category", "Unit")]
public class ResultGenericImplicitSpec
{
    #region Success Conversion

    /// <summary>TValue should implicitly convert to generic success.</summary>
    [Theory]
    [InlineData("value")]
    [InlineData("")]
    [InlineData("another value")]
    public void TValue_ShouldImplicitlyConvertToGenericSuccess(string value)
    {
        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
        result.Errors.Should().BeEmpty();
    }

    /// <summary>Value types should implicitly convert to generic success.</summary>
    [Theory]
    [InlineData(42)]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValueType_ShouldImplicitlyConvertToGenericSuccess(int value)
    {
        // Act
        Result<int> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
        result.Errors.Should().BeEmpty();
    }

    /// <summary>A null reference value should throw when converted to generic success.</summary>
    [Fact]
    public void NullValue_ShouldThrow_WhenConvertedToGenericSuccess()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () =>
        {
            Result<string> result = value;
            _ = result.Value;
        };

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Failure Conversion

    /// <summary>Error should implicitly convert to generic failure.</summary>
    [Theory]
    [MemberData(nameof(SingleErrorFailureCases))]
    public void Error_ShouldImplicitlyConvertToGenericFailure(
        Error error,
        int expectedStatusCode)
    {
        // Act
        Result<string> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(new[] { error });
        result.StatusCode.Should().Be(expectedStatusCode);
        result.Value.Should().BeNull();
    }

    public static TheoryData<Error, int> SingleErrorFailureCases()
    {
        var data = new TheoryData<Error, int>();

        data.Add(
            ResultStub.NotFoundError(),
            ResultConstant.StatusCode.NotFound);

        data.Add(
            ResultStub.ConflictError(),
            ResultConstant.StatusCode.Conflict);

        return data;
    }

    /// <summary>Error array should implicitly convert to generic failure.</summary>
    [Theory]
    [MemberData(nameof(StringErrorArrayCases))]
    public void ErrorArray_ShouldImplicitlyConvertToGenericFailure(
        Error[] errors,
        int expectedStatusCode)
    {
        // Act
        Result<string> result = errors;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(errors);
        result.StatusCode.Should().Be(expectedStatusCode);
        result.Value.Should().BeNull();
    }

    public static TheoryData<Error[], int> StringErrorArrayCases()
    {
        var data = new TheoryData<Error[], int>();

        data.Add(
            [ResultStub.NotFoundError()],
            ResultConstant.StatusCode.NotFound);

        data.Add(
            [ResultStub.ConflictError()],
            ResultConstant.StatusCode.Conflict);

        return data;
    }

    /// <summary>
    /// Error lists and multiple errors should implicitly convert to generic failure.
    /// </summary>
    [Theory]
    [MemberData(nameof(IntErrorCollectionCases))]
    public void ErrorListAndMultipleErrors_ShouldImplicitlyConvertToGenericFailure(
        bool useList,
        Error[] errors,
        int expectedCount)
    {
        // Arrange
        List<Error> list = [.. errors];

        // Act
        Result<int> result;

        if (useList)
        {
            result = list;
        }
        else
        {
            result = errors;
        }

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(expectedCount);
        result.Errors.Should().BeEquivalentTo(errors);
        result.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        result.Value.Should().Be(default);
    }

    public static TheoryData<bool, Error[], int> IntErrorCollectionCases()
    {
        var data = new TheoryData<bool, Error[], int>();

        data.Add(
            true,
            [ResultStub.NotFoundError()],
            1);

        data.Add(
            false,
            [
                ResultStub.NotFoundError(),
                Error.NotFound(
                    "test.other",
                    "Another missing resource")
            ],
            2);

        return data;
    }

    #endregion

    #region Failure Conversion Guards

    /// <summary>Invalid error collections should throw when converted.</summary>
    [Theory]
    [InlineData("empty-array")]
    [InlineData("empty-list")]
    [InlineData("mixed-array")]
    public void InvalidErrorCollections_ShouldThrow_WhenConverted(
        string collectionKind)
    {
        // Arrange
        Error[] array = [];

        if (collectionKind == "mixed-array")
        {
            array =
            [
                ResultStub.NotFoundError(),
                ResultStub.ConflictError()
            ];
        }

        List<Error> list = [];

        // Act
        Action act = () =>
        {
            if (collectionKind == "empty-list")
            {
                Result<int> result = list;
                _ = result.StatusCode;
            }
            else
            {
                Result<int> result = array;
                _ = result.StatusCode;
            }
        };

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>A null error list should throw when converted.</summary>
    [Fact]
    public void NullErrorList_ShouldThrow_WhenConverted()
    {
        // Arrange
        List<Error> errors = null!;

        // Act
        Action act = () =>
        {
            Result<int> result = errors;
            _ = result.StatusCode;
        };

        // Assert
        var exception = act.Should().Throw<ArgumentNullException>();
        exception.Which.ParamName.Should().Be("errors");
    }

    #endregion

    #region Direct Returns

    /// <summary>Direct returns should convert values and errors to generic results.</summary>
    [Fact]
    public void DirectReturns_ShouldConvertValuesAndErrorsToGenericResults()
    {
        // Act
        var success = ReturnValue();
        var failure = ReturnError();
        var listFailure = ReturnErrorList();

        // Assert
        success.IsSuccess.Should().BeTrue();
        success.Value.Should().Be(42);

        failure.IsFailure.Should().BeTrue();
        failure.Errors.Should().Contain(
            error => error.Code == "test.not_found");

        listFailure.IsFailure.Should().BeTrue();
        listFailure.Errors.Should().Contain(
            error => error.Code == "test.not_found");

        static Result<int> ReturnValue() => 42;

        static Result<int> ReturnError() =>
            ResultStub.NotFoundError();

        static Result<int> ReturnErrorList() =>
            new List<Error> { ResultStub.NotFoundError() };
    }

    #endregion
}