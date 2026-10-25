using Core.Common.Domain.ValueObjects.Percentage;

namespace Core.Tests.Domain.ValueObjects.Percentage;

public class PercentageExtensionSpec
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(25, 0.25)]
    [InlineData(50, 0.5)]
    [InlineData(100, 1)]
    public void ToFraction_ShouldConvert(decimal value, decimal expected)
    {
        // Arrange
        var result = global::Core.Common.Domain.ValueObjects.Percentage.Percentage.Create(value);

        // Act
        var fraction = result.Value.ToFraction();

        // Assert
        result.IsSuccess.Should().BeTrue();
        fraction.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 200, 20)]
    [InlineData(0, 200, 0)]
    [InlineData(100, 200, 200)]
    [InlineData(12.5, 80, 10)]
    [InlineData(25, 0, 0)]
    [InlineData(50, 99.99, 49.995)]
    public void ApplyTo_ShouldCalculate(decimal percentValue, decimal amount, decimal expected)
    {
        // Arrange
        var result = global::Core.Common.Domain.ValueObjects.Percentage.Percentage.Create(percentValue);

        // Act
        var applied = result.Value.ApplyTo(amount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        applied.Should().Be(expected);
    }
}