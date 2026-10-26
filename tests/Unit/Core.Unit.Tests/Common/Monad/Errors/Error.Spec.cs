namespace Core.Unit.Tests.Common.Monad.Errors;

/// <summary>
/// Tests for <see cref="Error"/> construction, validation, rendering, and value equality.
/// </summary>
/// <remarks>
/// Covers constructor defaults and optional values, argument validation,
/// status boundaries, string rendering, and equality semantics.
/// Metadata is intentionally excluded from value equality.
/// </remarks>
[Trait("Category", "Unit")]
public class ErrorSpec
{
    #region Construction

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        var error = new Error("order.not_found", "Order was not found");

        error.Code.Should().Be("order.not_found");
        error.Message.Should().Be("Order was not found");
        error.Type.Should().BeNull();
        error.Instance.Should().BeNull();
        error.Status.Should().BeNull();
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.Metadata.Should().NotBeNull();
        error.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldPreserveOptionalValues()
    {
        var error = new Error(
            code: "order.invalid",
            message: "Order is invalid",
            status: 422,
            type: "https://errors.kernel/custom",
            instance: "/orders/42",
            severity: ErrorSeverity.Critical);

        error.Code.Should().Be("order.invalid");
        error.Message.Should().Be("Order is invalid");
        error.Status.Should().Be(422);
        error.Type.Should().Be("https://errors.kernel/custom");
        error.Instance.Should().Be("/orders/42");
        error.Severity.Should().Be(ErrorSeverity.Critical);
    }

    [Fact]
    public void Error_ShouldImplementIError()
    {
        var error = new Error("code", "message");

        error.Should().BeAssignableTo<IError>();
    }

    #endregion

    #region Constructor validation

    [Fact]
    public void Constructor_WhenCodeIsNull_ShouldThrow()
    {
        var act = () => new Error(null!, "message");

        AssertValidationException<ArgumentNullException>(
            act,
            "code",
            ErrorConstant.Result.Failure.Code.NullOrWhitespace.Code,
            ErrorConstant.Result.Failure.Code.NullOrWhitespace.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Constructor_WhenCodeIsWhitespace_ShouldThrow(string code)
    {
        var act = () => new Error(code, "message");

        AssertValidationException<ArgumentException>(
            act,
            "code",
            ErrorConstant.Result.Failure.Code.NullOrWhitespace.Code,
            ErrorConstant.Result.Failure.Code.NullOrWhitespace.Message);
    }

    [Fact]
    public void Constructor_WhenCodeExceedsMaxLength_ShouldThrow()
    {
        var code = new string('c', ErrorConstant.Constraint.Code.MaxLength + 1);
        var act = () => new Error(code, "message");

        var exception = act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .Which;

        exception.ParamName.Should().Be("code");
        exception.Message.Should().Contain(
            ErrorConstant.Result.Failure.Code.ExceedsMaxLength.Code);
        exception.Message.Should().Contain(
            ErrorConstant.Constraint.Code.MaxLength.ToString());
    }

    [Fact]
    public void Constructor_WhenMessageIsNull_ShouldThrow()
    {
        var act = () => new Error("code", null!);

        AssertValidationException<ArgumentNullException>(
            act,
            "message",
            ErrorConstant.Result.Failure.Message.NullOrWhitespace.Code,
            ErrorConstant.Result.Failure.Message.NullOrWhitespace.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Constructor_WhenMessageIsWhitespace_ShouldThrow(string message)
    {
        var act = () => new Error("code", message);

        AssertValidationException<ArgumentException>(
            act,
            "message",
            ErrorConstant.Result.Failure.Message.NullOrWhitespace.Code,
            ErrorConstant.Result.Failure.Message.NullOrWhitespace.Message);
    }

    [Fact]
    public void Constructor_WhenMessageExceedsMaxLength_ShouldThrow()
    {
        var message = new string(
            'm',
            ErrorConstant.Constraint.Message.MaxLength + 1);

        var act = () => new Error("code", message);

        var exception = act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .Which;

        exception.ParamName.Should().Be("message");
        exception.Message.Should().Contain(
            ErrorConstant.Result.Failure.Message.ExceedsMaxLength.Code);
        exception.Message.Should().Contain(
            ErrorConstant.Constraint.Message.MaxLength.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(600)]
    [InlineData(1000)]
    public void Constructor_WhenStatusIsOutOfRange_ShouldThrow(int status)
    {
        var act = () => new Error("code", "message", status: status);

        var exception = act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .Which;

        exception.ParamName.Should().Be("status");
        exception.Message.Should().Contain(
            ErrorConstant.Result.Failure.Status.OutOfRange.Code);
        exception.Message.Should().Contain(
            ErrorConstant.Constraint.Status.Min.ToString());
        exception.Message.Should().Contain(
            ErrorConstant.Constraint.Status.Max.ToString());
    }

    [Theory]
    [InlineData(100)]
    [InlineData(101)]
    [InlineData(400)]
    [InlineData(500)]
    [InlineData(599)]
    public void Constructor_WhenStatusIsInRange_ShouldPreserveStatus(int status)
    {
        var error = new Error("code", "message", status: status);

        error.Status.Should().Be(status);
    }

    #endregion

    #region Rendering

    [Theory]
    [InlineData("order.not_found", "Order was not found",
        "order.not_found:Order was not found")]
    [InlineData("validation.failed", "Invalid input",
        "validation.failed:Invalid input")]
    [InlineData("code", "message", "code:message")]
    public void ToString_ShouldRenderCodeAndMessage(
        string code,
        string message,
        string expected)
    {
        var error = new Error(code, message);

        error.ToString().Should().Be(expected);
    }

    #endregion

    #region Value equality

    [Fact]
    public void Equality_WhenValuesAreEqual_ShouldReturnTrue()
    {
        var left = Error.BadRequest("order.not_found", "Order was not found");
        var right = Error.BadRequest("order.not_found", "Order was not found");

        left.Equals((IError?)right).Should().BeTrue();
        (left == right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Equality_ShouldIgnoreMetadata()
    {
        var left = new Error("code", "message")
            .WithMetadata("trace.id", "abc");

        var right = new Error("code", "message");

        left.Metadata.Should().NotBeSameAs(right.Metadata);
        left.Equals((IError?)right).Should().BeTrue();
        (left == right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Theory]
    [InlineData("Code")]
    [InlineData("Message")]
    [InlineData("Status")]
    [InlineData("Type")]
    [InlineData("Instance")]
    [InlineData("Severity")]
    public void Equality_WhenAValueDiffers_ShouldReturnFalse(string property)
    {
        var baseline = new Error("code", "message");

        var different = property switch
        {
            "Code" => new Error("other", "message"),
            "Message" => new Error("code", "other"),
            "Status" => new Error("code", "message", status: 500),
            "Type" => new Error("code", "message", type: "type"),
            "Instance" => new Error("code", "message", instance: "instance"),
            "Severity" => new Error(
                "code",
                "message",
                severity: ErrorSeverity.Warning),
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };

        baseline.Equals((IError?)different).Should().BeFalse();
        (baseline == different).Should().BeFalse();
        (baseline != different).Should().BeTrue();
    }

    [Fact]
    public void Equality_WhenComparedWithNull_ShouldReturnFalse()
    {
        var error = new Error("code", "message");

        error.Equals((IError?)null).Should().BeFalse();
        error.Equals((Error?)null).Should().BeFalse();
        (error == null).Should().BeFalse();
        (error != null).Should().BeTrue();
    }

    #endregion

    #region Assertion helpers

    private static void AssertValidationException<TException>(
      Func<Error> act,
      string expectedParamName,
      params string[] expectedMessages)
      where TException : ArgumentException
    {
        var exception = act.Should()
            .Throw<TException>()
            .Which;

        exception.ParamName.Should().Be(expectedParamName);

        foreach (var expectedMessage in expectedMessages)
        {
            exception.Message.Should().Contain(expectedMessage);
        }
    }

    #endregion

}