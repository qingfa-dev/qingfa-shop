using Core.Common.Domain.ValueObjects.Measurement;

namespace Core.Tests.Domain.ValueObjects.Measurement;

public class QuantityConstantSpec
{
    [Fact]
    public void Constraints_ValueMinValue_ShouldBeZero()
    {
        // Arrange, Act & Assert
        QuantityConstant.Constraints.Value.MinValue.Should().Be(0m);
    }

    [Fact]
    public void Defaults_Value_ShouldBeZero()
    {
        // Arrange, Act & Assert
        QuantityConstant.Defaults.Value.Should().Be(0m);
    }
}