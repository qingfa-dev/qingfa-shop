using Core.BuildingBlocks.Domain.ValueObjects.Money;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Money;

public class MoneyConstantSpec
{
    [Fact]
    public void Constraints_Currency_ShouldRequireExactlyThreeCharacters()
    {
        // Arrange, Act & Assert
        MoneyConstant.Constraints.Currency.MinLength.Should().Be(3);
        MoneyConstant.Constraints.Currency.MaxLength.Should().Be(3);
    }

    [Fact]
    public void Defaults_DefaultCurrencyCode_ShouldBeUsd()
    {
        // Arrange, Act & Assert
        MoneyConstant.Defaults.DefaultCurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Patterns_CurrencyCode_ShouldReuseCurrencyPattern()
    {
        // Arrange, Act & Assert
        MoneyConstant.Patterns.CurrencyCode.Should().Be(CurrencyConstant.Patterns.Code);
        MoneyConstant.Patterns.CurrencyCode.Should().Be("^[A-Z]{3}$");
    }
}