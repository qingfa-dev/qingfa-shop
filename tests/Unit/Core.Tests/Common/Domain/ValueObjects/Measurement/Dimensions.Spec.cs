using Core.Common.Domain.ValueObjects.Measurement;

using MeasurementUnit = Core.Common.Domain.ValueObjects.Measurement.Unit;

namespace Core.Tests.Common.Domain.ValueObjects.Measurement;

public class DimensionsSpec
{
    [Fact]
    public void Create_NonNegativeMeasurements_ShouldSucceed()
    {
        // Arrange
        var unit = MeasurementUnit.Create("cm").Value;

        // Act
        var result = Dimensions.Create(1m, 2m, 3m, unit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Length.Should().Be(1m);
        result.Value.Width.Should().Be(2m);
        result.Value.Height.Should().Be(3m);
    }

    [Fact]
    public void Create_NegativeLength_ShouldFailWithLengthNegative()
    {
        // Arrange
        var unit = MeasurementUnit.Create("cm").Value;

        // Act
        var result = Dimensions.Create(-1m, 2m, 3m, unit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Dimensions.Length.Negative");
    }

    [Fact]
    public void Create_NegativeWidth_ShouldFailWithWidthNegative()
    {
        // Arrange
        var unit = MeasurementUnit.Create("cm").Value;

        // Act
        var result = Dimensions.Create(1m, -2m, 3m, unit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Dimensions.Width.Negative");
    }

    [Fact]
    public void Create_NegativeHeight_ShouldFailWithHeightNegative()
    {
        // Arrange
        var unit = MeasurementUnit.Create("cm").Value;

        // Act
        var result = Dimensions.Create(1m, 2m, -3m, unit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Dimensions.Height.Negative");
    }

    [Fact]
    public void Create_ZeroMeasurements_ShouldSucceed()
    {
        // Arrange
        var unit = MeasurementUnit.Create("cm").Value;

        // Act
        var result = Dimensions.Create(0m, 0m, 0m, unit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Length.Should().Be(0m);
        result.Value.Width.Should().Be(0m);
        result.Value.Height.Should().Be(0m);
    }

    [Fact]
    public void Create_InvalidUnit_ShouldFailWithUnitInvalid()
    {
        // Arrange
        var unit = new MeasurementUnit("123");

        // Act
        var result = Dimensions.Create(1m, 2m, 3m, unit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Dimensions.Unit.Invalid");
    }

    [Fact]
    public void RecordEquality_ShouldCompareAllMeasurementComponents()
    {
        // Arrange
        var unit = MeasurementUnit.Create("cm").Value;
        var dimensions = Dimensions.Create(1m, 2m, 3m, unit).Value;
        var equivalent = Dimensions.Create(1m, 2m, 3m, unit).Value;
        var changed = Dimensions.Create(1m, 2m, 4m, unit).Value;

        // Act & Assert
        dimensions.Should().Be(equivalent);
        dimensions.Should().NotBe(changed);
    }
}