namespace Core.Unit.Tests.BuildingBlocks.Functional.Results;

/// <summary>Tests for dropping a typed result's value.</summary>
[Trait("Category", "Unit")]
public sealed class ResultConversionTest
{
    [Fact]
    public void ToResult_OnSuccess_ShouldDropTheValue()
    {
        Result<string> typed = Result<string>.Ok("value");

        Result result = typed.ToResult();

        result.IsSuccess.Should().BeTrue();
        result.Metadata.Should().BeEquivalentTo(typed.Metadata);
    }

    [Fact]
    public void ToResult_OnFailure_ShouldKeepErrorsAndStatus()
    {
        Result<string> typed = Result<string>.Fail(
            Error.Conflict("test.conflict", "conflict"));

        Result result = typed.ToResult();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("test.conflict");
        result.StatusCode.Should().Be(typed.StatusCode);
    }
}
