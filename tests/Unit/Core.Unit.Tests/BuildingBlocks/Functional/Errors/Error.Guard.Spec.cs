namespace Core.Unit.Tests.BuildingBlocks.Functional.Errors;

/// <summary>Tests for <see cref="ErrorGuard"/> validation methods.</summary>
/// <remarks>
/// Covers null/whitespace/length validation for code, message, and status fields.
/// </remarks>
[Trait("Category", "Unit")]
public class ErrorGuardSpec
{
    #region ValidateCode

    [Fact]
    public void ValidateCode_WhenNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var act = () => ErrorGuard.ValidateCode(null);

        // Act
        // Assert
        var ex = act.Should().Throw<ArgumentNullException>().Which;

        ex.ParamName.Should().Be("code");
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Code.NullOrWhitespace.Code);
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Code.NullOrWhitespace.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ValidateCode_WhenWhitespace_ShouldThrowArgumentException(string? code)
    {
        // Arrange
        var act = () => ErrorGuard.ValidateCode(code);

        // Act
        // Assert
        var ex = act.Should().Throw<ArgumentException>().Which;

        ex.ParamName.Should().Be("code");
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Code.NullOrWhitespace.Code);
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Code.NullOrWhitespace.Message);
    }

    [Fact]
    public void ValidateCode_WhenAtMaxLength_ShouldNotThrow()
    {
        // Arrange
        var code = new string('c', ErrorConstant.Constraint.Code.MaxLength);

        // Act
        var act = () => ErrorGuard.ValidateCode(code);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateCode_WhenExceedsMaxLength_ShouldThrowRangeWithFormattedPattern()
    {
        // Arrange
        var code = new string('c', ErrorConstant.Constraint.Code.MaxLength + 1);

        // Act
        var act = () => ErrorGuard.ValidateCode(code);

        // Assert
        var ex = act.Should().Throw<ArgumentOutOfRangeException>().Which;

        ex.ParamName.Should().Be("code");
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Code.ExceedsMaxLength.Code);
        ex.Message.Should().Contain("'256'");
    }

    #endregion

    #region ValidateMessage

    [Fact]
    public void ValidateMessage_WhenNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var act = () => ErrorGuard.ValidateMessage(null);

        // Act
        // Assert
        var ex = act.Should().Throw<ArgumentNullException>().Which;

        ex.ParamName.Should().Be("message");
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Message.NullOrWhitespace.Code);
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Message.NullOrWhitespace.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ValidateMessage_WhenWhitespace_ShouldThrowArgumentException(string? message)
    {
        // Arrange
        var act = () => ErrorGuard.ValidateMessage(message);

        // Act
        // Assert
        var ex = act.Should().Throw<ArgumentException>().Which;

        ex.ParamName.Should().Be("message");
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Message.NullOrWhitespace.Code);
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Message.NullOrWhitespace.Message);
    }

    [Fact]
    public void ValidateMessage_WhenAtMaxLength_ShouldNotThrow()
    {
        // Arrange
        var message = new string('m', ErrorConstant.Constraint.Message.MaxLength);

        // Act
        var act = () => ErrorGuard.ValidateMessage(message);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateMessage_WhenExceedsMaxLength_ShouldThrowRangeWithFormattedPattern()
    {
        // Arrange
        var message = new string('m', ErrorConstant.Constraint.Message.MaxLength + 1);

        // Act
        var act = () => ErrorGuard.ValidateMessage(message);

        // Assert
        var ex = act.Should().Throw<ArgumentOutOfRangeException>().Which;

        ex.ParamName.Should().Be("message");
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Message.ExceedsMaxLength.Code);
        ex.Message.Should().Contain("'1024'");
    }

    #endregion

    #region ValidateStatus

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void ValidateStatus_WhenOutOfRange_ShouldThrowRangeWithFormattedPattern(int status)
    {
        // Arrange
        var act = () => ErrorGuard.ValidateStatus(status);

        // Act
        // Assert
        var ex = act.Should().Throw<ArgumentOutOfRangeException>().Which;

        ex.ParamName.Should().Be("status");
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Status.OutOfRange.Code);
        ex.Message.Should().Contain("100 - 599");
    }

    [Theory]
    [InlineData(100)]
    [InlineData(599)]
    public void ValidateStatus_WhenInRange_ShouldNotThrow(int status)
    {
        // Arrange
        var act = () => ErrorGuard.ValidateStatus(status);

        // Act
        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region ValidateInstance

    [Fact]
    public void ValidateInstance_WhenNull_ShouldNotThrow()
    {
        // Arrange
        var act = () => ErrorGuard.ValidateInstance(null);

        // Act
        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateInstance_WhenAtMaxLength_ShouldNotThrow()
    {
        // Arrange
        var instance = new string('i', ErrorConstant.Constraint.Instance.MaxLength);

        // Act
        var act = () => ErrorGuard.ValidateInstance(instance);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateInstance_WhenExceedsMaxLength_ShouldThrowRangeWithFormattedPattern()
    {
        // Arrange
        var instance = new string('i', ErrorConstant.Constraint.Instance.MaxLength + 1);

        // Act
        var act = () => ErrorGuard.ValidateInstance(instance);

        // Assert
        var ex = act.Should().Throw<ArgumentOutOfRangeException>().Which;

        ex.ParamName.Should().Be("instance");
        ex.Message.Should().Contain(ErrorConstant.Result.Failure.Instance.ExceedsMaxLength.Code);
        ex.Message.Should().Contain("'256'");
    }

    #endregion

}