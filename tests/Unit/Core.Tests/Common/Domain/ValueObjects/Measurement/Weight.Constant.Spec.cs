using Core.Common.Domain.ValueObjects.Measurement;

namespace Core.Tests.Common.Domain.ValueObjects.Measurement;

public class WeightConstantSpec
{
    [Fact]
    public void Constraints_ValueMinValue_ShouldBeZero()
    {
        // Arrange, Act & Assert
        WeightConstant.Constraints.Value.MinValue.Should().Be(0m);
    }

    [Fact]
    public void Defaults_Value_ShouldBeZero()
    {
        // Arrange, Act & Assert
        WeightConstant.Defaults.Value.Should().Be(0m);
    }
}