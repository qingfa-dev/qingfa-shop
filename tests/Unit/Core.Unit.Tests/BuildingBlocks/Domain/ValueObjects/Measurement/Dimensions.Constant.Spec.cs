using Core.BuildingBlocks.Domain.ValueObjects.Measurement;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Measurement;

public class DimensionsConstantSpec
{
    [Fact]
    public void Constraints_LengthMinValue_ShouldBeZero()
    {
        // Arrange, Act & Assert
        DimensionsConstant.Constraints.Length.MinValue.Should().Be(0m);
    }

    [Fact]
    public void Constraints_WidthMinValue_ShouldBeZero()
    {
        // Arrange, Act & Assert
        DimensionsConstant.Constraints.Width.MinValue.Should().Be(0m);
    }

    [Fact]
    public void Constraints_HeightMinValue_ShouldBeZero()
    {
        // Arrange, Act & Assert
        DimensionsConstant.Constraints.Height.MinValue.Should().Be(0m);
    }
}