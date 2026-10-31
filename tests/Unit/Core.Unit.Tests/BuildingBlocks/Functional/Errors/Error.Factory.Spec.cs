namespace Core.Unit.Tests.BuildingBlocks.Functional.Errors;

/// <summary>Tests for <see cref="Error"/> factory methods.</summary>
/// <remarks>
/// Verifies that <see cref="Error.Custom"/> maps all properties,
/// and that category factories apply the correct status/type/severity defaults.
/// </remarks>
[Trait("Category", "Unit")]
public class ErrorFactorySpec
{
    #region Custom

    [Fact]
    public void Custom_WhenAllValuesProvided_ShouldMapEveryProperty()
    {
        // Arrange
        // Act
        var error = Error.Custom(
            code: "order.custom",
            message: "Custom failure",
            status: 409,
            type: "https://errors.kernel/custom",
            instance: "/orders/42",
            severity: ErrorSeverity.Warning);

        // Assert
        error.Code.Should().Be("order.custom");
        error.Message.Should().Be("Custom failure");
        error.Status.Should().Be(409);
        error.Type.Should().Be("https://errors.kernel/custom");
        error.Instance.Should().Be("/orders/42");
        error.Severity.Should().Be(ErrorSeverity.Warning);
    }

    [Fact]
    public void Custom_WhenDefaults_ShouldApplyContractDefaults()
    {
        // Arrange
        // Act
        var error = Error.Custom("code", "message");

        // Assert
        error.Status.Should().BeNull();
        error.Type.Should().BeNull();
        error.Instance.Should().BeNull();
        error.Severity.Should().Be(ErrorSeverity.Error);
    }

    #endregion

    #region Category factories

    [Theory]
    [InlineData(nameof(Error.BadRequest), 400, "https://errors.kernel/bad-request", ErrorSeverity.Warning)]
    [InlineData(nameof(Error.Unauthorized), 401, "https://errors.kernel/unauthorized", ErrorSeverity.Error)]
    [InlineData(nameof(Error.Forbidden), 403, "https://errors.kernel/forbidden", ErrorSeverity.Error)]
    [InlineData(nameof(Error.NotFound), 404, "https://errors.kernel/not-found", ErrorSeverity.Warning)]
    [InlineData(nameof(Error.Conflict), 409, "https://errors.kernel/conflict", ErrorSeverity.Warning)]
    [InlineData(nameof(Error.UnprocessableEntity), 422, "https://errors.kernel/unprocessable-entity", ErrorSeverity.Warning)]
    [InlineData(nameof(Error.InternalServerError), 500, "https://errors.kernel/internal-server-error", ErrorSeverity.Critical)]
    [InlineData(nameof(Error.ServiceUnavailable), 503, "https://errors.kernel/service-unavailable", ErrorSeverity.Critical)]
    public void CategoryFactory_ShouldApplyCategoryShape(
       string factory,
       int status,
       string type,
       ErrorSeverity severity)
    {
        // Arrange
        // Act
        var error = InvokeFactory(factory);

        // Assert
        error.Code.Should().Be("test.code");
        error.Message.Should().Be("test message");
        error.Status.Should().Be(status);
        error.Type.Should().Be(type);
        error.Severity.Should().Be(severity);
        error.Instance.Should().BeNull();
    }

    #endregion

    private static Error InvokeFactory(string factory) => factory switch
    {
        nameof(Error.BadRequest) => Error.BadRequest("test.code", "test message"),
        nameof(Error.Unauthorized) => Error.Unauthorized("test.code", "test message"),
        nameof(Error.Forbidden) => Error.Forbidden("test.code", "test message"),
        nameof(Error.NotFound) => Error.NotFound("test.code", "test message"),
        nameof(Error.Conflict) => Error.Conflict("test.code", "test message"),
        nameof(Error.UnprocessableEntity) => Error.UnprocessableEntity("test.code", "test message"),
        nameof(Error.InternalServerError) => Error.InternalServerError("test.code", "test message"),
        nameof(Error.ServiceUnavailable) => Error.ServiceUnavailable("test.code", "test message"),
        _ => throw new ArgumentOutOfRangeException(nameof(factory), factory, null),
    };
}