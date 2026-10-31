using Core.BuildingBlocks.Domain.ValueObjects.Money;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Money;

public class CurrencyConstantSpec
{
    [Fact]
    public void Constraints_Code_ShouldRequireExactlyThreeCharacters()
    {
        // Arrange, Act & Assert
        CurrencyConstant.Constraints.Code.MinLength.Should().Be(3);
        CurrencyConstant.Constraints.Code.MaxLength.Should().Be(3);
    }

    [Fact]
    public void Defaults_DefaultCode_ShouldBeUsd()
    {
        // Arrange, Act & Assert
        CurrencyConstant.Defaults.DefaultCode.Should().Be("USD");
    }

    [Fact]
    public void Patterns_Code_ShouldMatchExactlyThreeUppercaseLetters()
    {
        // Arrange, Act & Assert
        CurrencyConstant.Patterns.Code.Should().Be("^[A-Z]{3}$");
    }
}