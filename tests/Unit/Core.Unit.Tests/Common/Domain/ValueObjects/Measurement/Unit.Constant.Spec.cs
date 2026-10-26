using Core.Common.Domain.ValueObjects.Measurement;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Measurement;

public class UnitConstantSpec
{
    [Fact]
    public void Constraints_CodeMinLength_ShouldBeOne()
    {
        // Arrange, Act & Assert
        UnitConstant.Constraints.Code.MinLength.Should().Be(1);
    }

    [Fact]
    public void Constraints_CodeMaxLength_ShouldBeFour()
    {
        // Arrange, Act & Assert
        UnitConstant.Constraints.Code.MaxLength.Should().Be(4);
    }

    [Fact]
    public void Patterns_Code_ShouldAcceptOneToFourLowercaseLetters()
    {
        // Arrange, Act & Assert
        UnitConstant.Patterns.Code.Should().Be(@"^[a-z]{1,4}$");
    }
}