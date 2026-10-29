using Application.Common.Validation.FluentValidation;

using FluentValidation;

namespace Application.Unit.Tests.Common.Validation.FluentValidation;

/// <summary>Specification for <see cref="ErrorMapper"/>.</summary>
[Trait("Category", "Unit")]
public sealed class ErrorMapperTest
{
    #region Property Name

    [Fact]
    public void ToValidationFailure_ExplicitPropertyName_ShouldOverrideMetadataField()
    {
        // Arrange:
        var error = new Error("todo.name.required", "Name is required.")
            .WithField("MetadataField");

        // Act:
        var failure = error.ToValidationFailure("Request.Name");

        // Assert:
        failure.PropertyName.Should().Be("Request.Name");
    }

    [Fact]
    public void ToValidationFailure_WithoutPropertyName_ShouldUseMetadataField()
    {
        // Arrange:
        var error = new Error("todo.name.required", "Name is required.")
            .WithField("Name");

        // Act:
        var failure = error.ToValidationFailure();

        // Assert:
        failure.PropertyName.Should().Be("Name");
    }

    [Fact]
    public void ToValidationFailure_WithoutFieldMetadata_ShouldUseEmptyPropertyName()
    {
        // Arrange:
        var error = new Error("todo.failed", "The operation failed.");

        // Act:
        var failure = error.ToValidationFailure();

        // Assert:
        failure.PropertyName.Should().BeEmpty();
    }

    [Fact]
    public void ToValidationFailure_WithWhitespaceField_ShouldUseEmptyPropertyName()
    {
        // Arrange:
        var error = new Error("todo.failed", "The operation failed.")
            .WithField("   ");

        // Act:
        var failure = error.ToValidationFailure();

        // Assert:
        failure.PropertyName.Should().BeEmpty();
    }

    #endregion

    #region Preserved Values

    [Fact]
    public void ToValidationFailure_ShouldPreserveCodeMessageAndOriginalError()
    {
        // Arrange:
        var error = new Error(
            code: "todo.name.required",
            message: "Name is required.",
            status: 422,
            type: "https://errors.qingfa/todo/name",
            instance: "/todos/42",
            severity: ErrorSeverity.Critical)
            .WithField("Name");

        // Act:
        var failure = error.ToValidationFailure();

        // Assert:
        failure.ErrorCode.Should().Be("todo.name.required");
        failure.ErrorMessage.Should().Be("Name is required.");
        failure.Severity.Should().Be(Severity.Error);

        var state = failure.CustomState.Should().BeOfType<ValidationErrorState>().Subject;
        state.Error.Should().BeSameAs(error);
        state.Status.Should().Be(422);
        state.Severity.Should().Be(ErrorSeverity.Critical);
        state.Metadata.Should().BeSameAs(error.Metadata);
    }

    [Theory]
    [InlineData(ErrorSeverity.Info, Severity.Info)]
    [InlineData(ErrorSeverity.Warning, Severity.Warning)]
    [InlineData(ErrorSeverity.Error, Severity.Error)]
    [InlineData(ErrorSeverity.Critical, Severity.Error)]
    public void ToValidationFailure_ShouldMapSeverity(
        ErrorSeverity severity,
        Severity expected)
    {
        // Arrange:
        var error = new Error("todo.failed", "The operation failed.", severity: severity);

        // Act:
        var failure = error.ToValidationFailure();

        // Assert:
        failure.Severity.Should().Be(expected);
    }

    #endregion

    #region Guards

    [Fact]
    public void ToValidationFailure_NullError_ShouldThrow()
    {
        // Act:
        Action act = () => ErrorMapper.ToValidationFailure(null!);

        // Assert:
        act.Should().Throw<ArgumentNullException>().WithParameterName("error");
    }

    #endregion
}