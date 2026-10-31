namespace Core.Unit.Tests.BuildingBlocks.Functional.Results;

/// <summary>Tests for <see cref="ResultException"/>.</summary>
[Trait("Category", "Unit")]
public class ResultExceptionSpec
{
    /// <summary>Exception should expose the wrapped result and its errors.</summary>
    [Fact]
    public void Constructor_WhenResultHasErrors_ShouldExposeResultAndErrors()
    {
        // Arrange
        var first = Error.InternalServerError("test.first", "first failed");
        var second = Error.InternalServerError("test.second", "second failed");
        var result = Result.Fail(first, second);

        // Act
        var exception = new ResultException(result);

        // Assert
        exception.Result.Should().BeSameAs(result);
        exception.Errors.Should().BeEquivalentTo(result.Errors);
        exception.StatusCode.Should().Be(result.StatusCode);
        exception.Message.Should().Contain(first.ToString());
        exception.Message.Should().Contain(second.ToString());
        exception.Message.Should().Contain(Environment.NewLine);
    }

    /// <summary>Exception without errors should describe the status.</summary>
    [Fact]
    public void Constructor_WhenResultHasNoErrors_ShouldDescribeStatus()
    {
        // Arrange
        var result = Result.Ok();

        // Act
        var exception = new ResultException(result);

        // Assert
        exception.Message.Should().Be(
            $"Result failed with status {ResultConstant.StatusCode.Ok}.");
    }

}