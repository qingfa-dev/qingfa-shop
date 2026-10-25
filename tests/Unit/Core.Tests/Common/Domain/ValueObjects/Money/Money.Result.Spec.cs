using Core.Common.Domain.ValueObjects.Money;

namespace Core.Tests.Common.Domain.ValueObjects.Money;

public class MoneyResultSpec
{
    [Fact]
    public void CurrencyInvalid_ShouldExposeStableCodeAndMessage()
    {
        // Arrange & Act
        var error = MoneyOutcome.Failure.CurrencyInvalid;

        // Assert
        error.Code.Should().Be("Money.Currency.Invalid");
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CurrencyMismatch_ShouldExposeStableCodeAndMessage()
    {
        // Arrange & Act
        var error = MoneyOutcome.Failure.CurrencyMismatch;

        // Assert
        error.Code.Should().Be("Money.Currency.Mismatch");
        error.Message.Should().NotBeNullOrWhiteSpace();
    }
}