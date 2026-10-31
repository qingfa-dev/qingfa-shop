using Core.BuildingBlocks.Domain.ValueObjects.Measurement;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Measurement;

public class UnitResultSpec
{
    [Fact]
    public void Failure_CodeRequired_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        UnitOutcome.Failure.CodeRequired.Code.Should().Be("Unit.Code.Required");
    }

    [Fact]
    public void Failure_CodeTooLong_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        UnitOutcome.Failure.CodeTooLong.Code.Should().Be("Unit.Code.TooLong");
    }

    [Fact]
    public void Failure_CodeInvalid_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        UnitOutcome.Failure.CodeInvalid.Code.Should().Be("Unit.Code.Invalid");
    }
}