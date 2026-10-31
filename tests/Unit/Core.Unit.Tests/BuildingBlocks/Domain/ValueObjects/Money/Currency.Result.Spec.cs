using Core.BuildingBlocks.Domain.ValueObjects.Money;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Money;

public class CurrencyResultSpec
{
    [Fact]
    public void CodeRequired_ShouldExposeStableCodeAndMessage()
    {
        // Arrange & Act
        var error = CurrencyOutcome.Failure.CodeRequired;

        // Assert
        error.Code.Should().Be("Currency.Code.Required");
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CodeInvalid_ShouldExposeStableCodeAndMessage()
    {
        // Arrange & Act
        var error = CurrencyOutcome.Failure.CodeInvalid;

        // Assert
        error.Code.Should().Be("Currency.Code.Invalid");
        error.Message.Should().NotBeNullOrWhiteSpace();
    }
}