namespace Core.Tests.Common.Monad.Results;

/// <summary>Tests for the generic <see cref="Result{TValue}"/> record.</summary>
[Trait("Category", "Unit")]
public class ResultGenericSpec
{
    #region CopyWith

    /// <summary>CopyWith without changes should preserve value and status.</summary>
    [Fact]
    public void CopyWithWithoutChanges_ShouldPreserveValueAndStatus()
    {
        // Arrange
        var original = Result<int>.Ok(5);

        // Act
        var copy = original.CopyWith();

        // Assert
        copy.Value.Should().Be(5);
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
        copy.Metadata.Should().NotBeSameAs(original.Metadata);
    }

    /// <summary>CopyWith with explicit status should use the provided status.</summary>
    [Fact]
    public void CopyWith_WhenExplicitStatus_ShouldUseProvidedStatus()
    {
        // Act
        var copy = Result<int>.Ok(5).CopyWith(statusCode: ResultConstant.StatusCode.Created);

        // Assert
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Created);
    }

    /// <summary>CopyWith errors on failure should keep status and use new errors.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyWithErrorsOnFailure_ShouldPreserveStatusAndUseNewErrors(bool withValue)
    {
        // Arrange
        var failure = Result<int>.Fail(ResultStub.NotFoundError());
        var errors = new List<Error> { ResultStub.ConflictError() };

        // Act
        var copy = withValue
            ? failure.CopyWith(value: 0, errors: errors)
            : failure.CopyWith(errors: errors);

        // Assert
        copy.IsFailure.Should().BeTrue();
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        copy.Errors[0].Code.Should().Be("test.conflict");
    }

    /// <summary>CopyWith value with explicit status and metadata should apply them.</summary>
    [Fact]
    public void CopyWithValueWithExplicitStatusAndMetadata_ShouldApplyThem()
    {
        // Arrange
        var original = Result<int>.Ok(5);
        var replacement = DefaultMetadata.Create().With("k", "v");

        // Act
        var copy = original.CopyWith(
            value: 7,
            statusCode: ResultConstant.StatusCode.Created,
            metadata: replacement);

        // Assert
        copy.Value.Should().Be(7);
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Created);
        copy.Metadata.Should().NotBeSameAs(replacement);
        copy.Metadata["k"].Should().Be("v");
    }

    /// <summary>CopyWith to failure should use new errors and keep the value unless one is provided.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyWithToFailure_ShouldUseErrorsAndKeepValue_WhenNotProvided(bool withValue)
    {
        // Arrange
        var original = Result<int>.Ok(5);
        var errors = new List<Error> { ResultStub.NotFoundError() };

        // Act
        var copy = withValue
            ? original.CopyWith(value: 0, isSuccess: false, errors: errors)
            : original.CopyWith(isSuccess: false, errors: errors);

        // Assert
        copy.IsFailure.Should().BeTrue();
        copy.Value.Should().Be(withValue ? default : 5);
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        copy.TryGetValue(out _).Should().BeFalse();
    }

    /// <summary>CopyWith errors on success should preserve status and value.</summary>
    [Theory]
    [InlineData(true, 7)]
    [InlineData(false, 5)]
    public void CopyWithErrorsOnSuccess_ShouldPreserveStatusAndValue(bool withValue, int expectedValue)
    {
        // Arrange
        var success = Result<int>.Ok(5);
        var errors = new List<Error> { ResultStub.NotFoundError() };

        // Act
        var copy = withValue
            ? success.CopyWith(value: 7, errors: errors)
            : success.CopyWith(errors: errors);

        // Assert
        copy.IsSuccess.Should().BeTrue();
        copy.Value.Should().Be(expectedValue);
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
        copy.Errors.Count.Should().Be(1);
        copy.Errors[0].Code.Should().Be("test.not_found");
    }

    /// <summary>CopyWith value on failure without errors should preserve errors and status.</summary>
    [Fact]
    public void CopyWithValueOnFailureWithoutErrors_ShouldPreserveErrorsAndStatus()
    {
        // Arrange
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act
        var copy = failure.CopyWith(value: 0);

        // Assert
        copy.IsFailure.Should().BeTrue();
        copy.Value.Should().Be(default);
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        copy.Errors.Count.Should().Be(1);
        copy.Errors[0].Code.Should().Be("test.not_found");
    }

    /// <summary>CopyWith with metadata should clone the provided bag.</summary>
    [Fact]
    public void CopyWith_WhenMetadata_ShouldCloneProvidedBag()
    {
        // Arrange
        var replacement = DefaultMetadata.Create().With("k", "v");

        // Act
        var copy = Result<int>.Ok(5).CopyWith(metadata: replacement);

        // Assert
        copy.Metadata.Should().NotBeSameAs(replacement);
        copy.Metadata["k"].Should().Be("v");
    }

    /// <summary>CopyWith should replace value and clone metadata.</summary>
    [Fact]
    public void CopyWith_ShouldReplaceValueAndCloneMetadata()
    {
        // Arrange
        var original = Result<int>.Ok(5).WithMetadata("source", "original");

        // Act
        var copy = original.CopyWith(value: 10);

        // Assert
        copy.Value.Should().Be(10);
        copy.StatusCode.Should().Be(original.StatusCode);
        copy.Metadata.Should().NotBeSameAs(original.Metadata);
        copy.Metadata["source"].Should().Be("original");
    }

    /// <summary>CopyWith to failure without a value should retain the value but block TryGetValue.</summary>
    [Fact]
    public void CopyWith_WhenChangingToFailure_ShouldRetainValueAndBlockTryGetValue()
    {
        // Arrange
        var original = Result<int>.Ok(5);
        var errors = new List<Error> { ResultStub.NotFoundError() };

        // Act
        var copy = original.CopyWith(isSuccess: false, errors: errors);

        // Assert
        copy.IsFailure.Should().BeTrue();
        copy.Value.Should().Be(5);
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        copy.TryGetValue(out _).Should().BeFalse();
    }

    /// <summary>CopyWith should require replacement value when changing failure to success.</summary>
    [Fact]
    public void CopyWith_ShouldRequireReplacementValue_WhenChangingFailureToSuccess()
    {
        // Arrange
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act
        var act = () => failure.CopyWith(isSuccess: true);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>CopyWith with replacement value should allow failure to success transition.</summary>
    [Fact]
    public void CopyWith_WhenReplacementValue_ShouldAllowFailureToSuccessTransition()
    {
        // Arrange
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act
        var copy = failure.CopyWith(value: 42, isSuccess: true);

        // Assert
        copy.IsSuccess.Should().BeTrue();
        copy.Value.Should().Be(42);
        copy.Errors.Should().BeEmpty();
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
    }

    #endregion

    #region Equality

    /// <summary>Generic result equality should compare state and value.</summary>
    [Fact]
    public void ResultGenericEquality_ShouldCompareStateAndValue()
    {
        // Arrange
        var left = Result<int>.Ok(5);
        var right = Result<int>.Ok(5);

        // Act & Assert
        left.Equals(right).Should().BeTrue();
        (left == right).Should().BeTrue();
        left.Equals(Result<int>.Ok(6)).Should().BeFalse();
        left.Equals(Result<int>.Fail(ResultStub.NotFoundError())).Should().BeFalse();
        left.Equals((Result<int>?)null).Should().BeFalse();
    }

    /// <summary>Generic result GetHashCode should match for same state.</summary>
    [Fact]
    public void ResultGenericGetHashCodeSameState_ShouldMatch()
    {
        // Arrange
        var left = Result<int>.Fail(ResultStub.NotFoundError());
        var right = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    #endregion

    #region Value Access

    /// <summary>TryGetValue and ToString should reflect result state.</summary>
    [Fact]
    public void TryGetValueAndToString_ShouldReflectResultState()
    {
        // Arrange
        var success = Result<int>.Ok(42);
        var failure = Result<int>.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.TryGetValue(out var value).Should().BeTrue();
        value.Should().Be(42);
        success.ToString().Should().Be("Success (200): 42");

        failure.TryGetValue(out _).Should().BeFalse();
        failure.ToString().Should().Contain("Failure (404):");
        failure.ToString().Should().Contain("test.not_found");
    }

    #endregion

    #region Constructor

    /// <summary>Constructor should reject a success without a value.</summary>
    [Fact]
    public void Constructor_ShouldRejectASuccessWithoutAValue()
    {
        // Act
        var act = () => new Result<string>(isSuccess: true, value: null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion
}