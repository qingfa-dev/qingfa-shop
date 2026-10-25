using Core.Common.Domain.ValueObjects.Measurement;

namespace Core.Tests.Common.Domain.ValueObjects.Measurement;

public class UnitResultSpec
{
    [Fact]
    public void Failure_CodeRequired_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        UnitResult.Failure.CodeRequired.Code.Should().Be("Unit.Code.Required");
    }

    [Fact]
    public void Failure_CodeTooLong_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        UnitResult.Failure.CodeTooLong.Code.Should().Be("Unit.Code.TooLong");
    }

    [Fact]
    public void Failure_CodeInvalid_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        UnitResult.Failure.CodeInvalid.Code.Should().Be("Unit.Code.Invalid");
    }
}