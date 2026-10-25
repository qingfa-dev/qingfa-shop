namespace Core.Tests.Common.Monad.Results;

/// <summary>Tests for the non-generic <see cref="Result"/> record.</summary>
[Trait("Category", "Unit")]
public class ResultSpec
{
    #region Constructor

    /// <summary>Constructor should default status by state when status is null.</summary>
    [Theory]
    [InlineData(true, ResultConstant.StatusCode.Ok)]
    [InlineData(false, ResultConstant.StatusCode.InternalServerError)]
    public void ConstructorWithNullStatus_ShouldDefaultByState(
        bool isSuccess,
        int expectedStatus)
    {
        // Arrange
        List<Error>? errors = isSuccess
            ? null
            : [new Error("test.generic", "A failure without an explicit status")];

        // Act
        var result = new Result(isSuccess, errors!, statusCode: null);

        // Assert
        result.IsSuccess.Should().Be(isSuccess);
        result.StatusCode.Should().Be(expectedStatus);

        if (isSuccess)
        {
            result.Errors.Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Equal(errors!);
        }
    }

    #endregion

    #region Equality

    /// <summary>Results with same state should be equal.</summary>
    [Fact]
    public void ResultEqualitySameState_ShouldBeEqual()
    {
        // Arrange
        var error = ResultStub.NotFoundError();
        var left = Result.Fail(error);
        var right = Result.Fail(error);

        // Act & Assert
        left.Equals(right).Should().BeTrue();
        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
        left.Equals((Result?)null).Should().BeFalse();
    }

    /// <summary>Results with different state should not be equal.</summary>
    [Fact]
    public void ResultEquality_WhenStateDiffers_ShouldNotBeEqual()
    {
        // Arrange
        var failure = Result.Fail(ResultStub.NotFoundError());
        var single = Result.Fail(ResultStub.ConflictError());
        var doubled = Result.Fail(
            ResultStub.ConflictError(),
            ResultStub.ConflictError());

        // Act & Assert
        failure.Equals(Result.Ok()).Should().BeFalse();
        failure.Equals(single).Should().BeFalse();
        single.Equals(doubled).Should().BeFalse();
    }

    /// <summary>Equality should exclude metadata but include status code.</summary>
    [Fact]
    public void ResultEquality_ShouldExcludeMetadataAndIncludeStatus()
    {
        // Arrange
        var error = ResultStub.NotFoundError();
        var left = Result.Fail(error).WithMetadata("k", "v");
        var right = Result.Fail(error);
        var otherStatus = Result.Fail(
            new List<Error> { error },
            ResultConstant.StatusCode.InternalServerError);

        // Act & Assert
        left.Metadata.Should().NotBeSameAs(right.Metadata);
        left.Equals(right).Should().BeTrue();
        left.Equals(otherStatus).Should().BeFalse();
    }

    /// <summary>Same state should produce the same hash code.</summary>
    [Fact]
    public void ResultGetHashCodeSameState_ShouldMatch()
    {
        // Arrange
        var error = ResultStub.NotFoundError();
        var left = Result.Fail(error);
        var right = Result.Fail(error);

        // Act & Assert
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    #endregion

    #region CopyWith

    /// <summary>CopyWith with no changes should preserve state.</summary>
    [Fact]
    public void CopyWithWithoutChanges_ShouldPreserveState()
    {
        // Arrange
        var original = Result.Ok();

        // Act
        var copy = original.CopyWith();

        // Assert
        copy.IsSuccess.Should().BeTrue();
        copy.StatusCode.Should().Be(original.StatusCode);
        copy.Metadata.Should().NotBeSameAs(original.Metadata);
    }

    /// <summary>CopyWith with explicit status should use the provided status.</summary>
    [Fact]
    public void CopyWith_WhenExplicitStatus_ShouldUseProvidedStatus()
    {
        // Arrange
        var original = Result.Ok();

        // Act
        var copy = original.CopyWith(
            statusCode: ResultConstant.StatusCode.Created);

        // Assert
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Created);
    }

    /// <summary>CopyWith to success on failure should reset status to 200.</summary>
    [Fact]
    public void CopyWithToSuccessOnFailure_ShouldResetStatusToOk()
    {
        // Arrange
        var failure = Result.Fail(ResultStub.NotFoundError());

        // Act
        var copy = failure.CopyWith(isSuccess: true);

        // Assert
        copy.IsSuccess.Should().BeTrue();
        copy.Errors.Should().BeEmpty();
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
    }

    /// <summary>CopyWith errors on failure should re-resolve status from new errors.</summary>
    [Fact]
    public void CopyWithErrorsOnFailure_ShouldReResolveStatusFromNewErrors()
    {
        // Arrange
        var failure = Result.Fail(ResultStub.NotFoundError());

        // Act
        var copy = failure.CopyWith(
            errors: new List<Error> { ResultStub.ConflictError() });

        // Assert
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Conflict);
        copy.Errors[0].Code.Should().Be("test.conflict");
    }

    /// <summary>CopyWith errors on success should preserve status.</summary>
    [Fact]
    public void CopyWithErrorsOnSuccess_ShouldPreserveStatus()
    {
        // Act
        var copy = Result.Ok().CopyWith(
            errors: new List<Error> { ResultStub.NotFoundError() });

        // Assert
        copy.IsSuccess.Should().BeTrue();
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.Ok);
        copy.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("test.not_found");
    }

    /// <summary>CopyWith with metadata should use the provided bag.</summary>
    [Fact]
    public void CopyWith_WhenMetadata_ShouldUseProvidedBag()
    {
        // Arrange
        var original = Result.Ok().WithMetadata("source", "original");
        var replacement = DefaultMetadata.Create()
            .With("source", "replacement");

        // Act
        var copy = original.CopyWith(metadata: replacement);

        // Assert
        copy.Metadata.Should().NotBeSameAs(replacement);
        copy.Metadata["source"].Should().Be("replacement");
    }

    /// <summary>CopyWith should copy failure state and clone errors and metadata.</summary>
    [Fact]
    public void CopyWith_ShouldCopyFailureStateAndCloneErrorsAndMetadata()
    {
        // Arrange
        var original = Result.Ok().WithMetadata("source", "original");
        var errors = new List<Error> { ResultStub.NotFoundError() };

        // Act
        var copy = original.CopyWith(isSuccess: false, errors: errors);

        // Assert
        copy.IsFailure.Should().BeTrue();
        copy.StatusCode.Should().Be(ResultConstant.StatusCode.NotFound);
        copy.Errors.Should().Equal(errors);
        copy.Errors.Should().NotBeSameAs(errors);
        copy.Metadata.Should().NotBeSameAs(original.Metadata);
        copy.Metadata["source"].Should().Be("original");

        errors[0] = ResultStub.ConflictError();
        copy.Errors[0].Code.Should().Be("test.not_found");
    }

    #endregion

    #region State

    /// <summary>Result should expose state and render success and failure.</summary>
    [Fact]
    public void Result_ShouldExposeStateAndRenderSuccessAndFailure()
    {
        // Arrange
        var success = Result.Ok();
        var failure = Result.Fail(ResultStub.NotFoundError());

        // Act & Assert
        success.HasValue.Should().BeTrue();
        success.IsSuccess.Should().BeTrue();
        success.IsFailure.Should().BeFalse();
        success.ToString().Should().Be("Success (200)");

        failure.HasValue.Should().BeFalse();
        failure.IsSuccess.Should().BeFalse();
        failure.IsFailure.Should().BeTrue();
        failure.ToString().Should().Contain("test.not_found");
    }

    #endregion

}