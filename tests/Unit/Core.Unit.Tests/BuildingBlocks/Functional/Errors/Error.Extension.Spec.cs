namespace Core.Unit.Tests.BuildingBlocks.Functional.Errors;

/// <summary>Tests for <see cref="Error"/> extension methods.</summary>
/// <remarks>
/// Covers <see cref="Error.CopyWith"/> override semantics,
/// <see cref="Error.WithMetadata"/> mutation, well-known metadata helpers,
/// <see cref="Error.WithCode"/>/<see cref="Error.WithMessage"/> copy helpers,
/// and <see cref="Error.FromException"/> mapping.
/// </remarks>
[Trait("Category", "Unit")]
public class ErrorExtensionSpec
{
    #region CopyWith

    [Fact]
    public void CopyWith_WhenNoOverrides_ShouldPreserveAllValues()
    {
        // Arrange
        var original = new Error(
            code: "code",
            message: "message",
            status: 409,
            type: "https://errors.kernel/conflict",
            instance: "/orders/42",
            severity: ErrorSeverity.Warning);

        // Act
        var copy = original.CopyWith();

        // Assert
        copy.Should().NotBeSameAs(original);
        copy.Equals(original).Should().BeTrue();
        copy.Code.Should().Be("code");
        copy.Message.Should().Be("message");
        copy.Status.Should().Be(409);
        copy.Type.Should().Be("https://errors.kernel/conflict");
        copy.Instance.Should().Be("/orders/42");
        copy.Severity.Should().Be(ErrorSeverity.Warning);
    }

    [Fact]
    public void CopyWith_WhenOverridesProvided_ShouldReplaceOnlyThose()
    {
        // Arrange
        var original = new Error("code", "message", status: 409, severity: ErrorSeverity.Warning);

        // Act
        var copy = original.CopyWith(message: "changed", severity: ErrorSeverity.Critical);

        // Assert
        copy.Message.Should().Be("changed");
        copy.Severity.Should().Be(ErrorSeverity.Critical);
        copy.Code.Should().Be("code");
        copy.Status.Should().Be(409);
        original.Message.Should().Be("message");
        original.Severity.Should().Be(ErrorSeverity.Warning);
    }

    [Fact]
    public void CopyWith_WhenOverrideNull_ShouldKeepOriginalValue()
    {
        // Arrange
        var original = new Error("code", "message", status: 409);

        // Act
        var copy = original.CopyWith(code: null, message: null, status: null);

        // Assert
        copy.Code.Should().Be("code");
        copy.Message.Should().Be("message");
        copy.Status.Should().Be(409);
    }

    [Fact]
    public void CopyWith_WhenMetadataProvided_ShouldAssignProvidedBag()
    {
        // Arrange
        var original = new Error("code", "message");
        var metadata = DefaultMetadata.Create().With("trace.id", "abc");

        // Act
        var copy = original.CopyWith(metadata: metadata);

        // Assert
        copy.Metadata.Should().BeSameAs(metadata);
        original.Metadata.Should().NotBeSameAs(metadata);
    }

    [Fact]
    public void CopyWith_WhenMetadataNotProvided_ShouldShareOriginalBag()
    {
        // Arrange
        var original = new Error("code", "message");

        // Act
        var copy = original.CopyWith();

        // Assert
        copy.Metadata.Should().BeSameAs(original.Metadata);
    }

    #endregion

    #region WithMetadata

    [Fact]
    public void WithMetadataKeyValue_ShouldMutateAndReturnSameInstance()
    {
        // Arrange
        var error = new Error("code", "message");

        // Act
        var result = error.WithMetadata("trace.id", "abc");

        // Assert
        result.Should().BeSameAs(error);
        error.Metadata["trace.id"].Should().Be("abc");
    }

    [Fact]
    public void WithMetadataDictionary_ShouldReplaceBag()
    {
        // Arrange
        var error = new Error("code", "message");
        var metadata = DefaultMetadata.Create().With("attempt", 3);

        // Act
        var result = error.WithMetadata(metadata);

        // Assert
        result.Should().BeSameAs(error);
        error.Metadata.Should().BeSameAs(metadata);
        error.Metadata["attempt"].Should().Be(3);
    }

    [Fact]
    public void WithMetadata_WhenNullDictionary_ShouldFallBackToEmptyBag()
    {
        // Arrange
        var error = new Error("code", "message");

        // Act
        var result = error.WithMetadata((DefaultMetadata)null!);

        // Assert
        result.Should().BeSameAs(error);
        error.Metadata.Should().NotBeNull();
        error.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void WithMetadataHelpers_ShouldSetContractKeys()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);

        // Act
        var error = new Error("code", "message")
            .WithTraceId("trace-1")
            .WithTimestamp(timestamp)
            .WithResource("orders")
            .WithField("total")
            .WithAttempt(3);

        // Assert
        error.Metadata[ErrorConstant.Metadata.TraceId].Should().Be("trace-1");
        error.Metadata[ErrorConstant.Metadata.Timestamp].Should().Be(timestamp);
        error.Metadata[ErrorConstant.Metadata.Resource].Should().Be("orders");
        error.Metadata[ErrorConstant.Metadata.Field].Should().Be("total");
        error.Metadata[ErrorConstant.Metadata.Attempt].Should().Be(3);
    }

    #endregion

    #region With* copy helpers

    [Fact]
    public void WithExtensions_ShouldOverrideIndividualFieldsOnCopies()
    {
        // Arrange
        var original = new Error(
            code: "code",
            message: "message",
            status: 400,
            type: "https://errors.kernel/bad-request",
            instance: "/orders/42",
            severity: ErrorSeverity.Warning);

        // Act
        // Assert
        original.WithCode("code2").Code.Should().Be("code2");
        original.WithMessage("message2").Message.Should().Be("message2");
        original.WithType("type2").Type.Should().Be("type2");
        original.WithInstance("instance2").Instance.Should().Be("instance2");
        original.WithStatus(422).Status.Should().Be(422);
        original.WithSeverity(ErrorSeverity.Critical).Severity.Should().Be(ErrorSeverity.Critical);
        original.Code.Should().Be("code");
        original.Severity.Should().Be(ErrorSeverity.Warning);
    }

    #endregion

    #region FromException

    [Fact]
    public void FromException_WhenNull_ShouldThrowExceptionParam()
    {
        // Arrange
        var act = () => Error.FromException(null!);

        // Act & Assert
        var exception = act.Should()
            .Throw<ArgumentNullException>()
            .Which;

        exception.ParamName.Should().Be("exception");
    }

    [Fact]
    public void FromException_WhenProvided_ShouldMapToInternalServerErrorWithMetadata()
    {
        // Arrange
        var exception = new InvalidOperationException("boom");

        // Act
        var error = Error.FromException(exception);

        // Assert
        error.Code.Should().Be("general.error");
        error.Message.Should().Be("boom");
        error.Status.Should().Be(500);
        error.Type.Should().Be("https://errors.kernel/internal-server-error");
        error.Severity.Should().Be(ErrorSeverity.Critical);
        error.Metadata["exception.type"].Should().Be(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public void FromException_WhenCustomCodeAndSeverity_ShouldOverrideDefaults()
    {
        // Arrange
        var exception = new InvalidOperationException("boom");

        // Act
        var error = Error.FromException(exception, code: "db.failure", severity: ErrorSeverity.Warning);

        // Assert
        error.Code.Should().Be("db.failure");
        error.Severity.Should().Be(ErrorSeverity.Warning);
        error.Status.Should().Be(500);
        error.Message.Should().Be("boom");
    }

    #endregion
}