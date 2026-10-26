using Core.Common.Domain.ValueObjects.Measurement;

using MeasurementUnit = Core.Common.Domain.ValueObjects.Measurement.Unit;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Measurement;

public class WeightSpec
{
    [Fact]
    public void Create_NonNegativeValue_ShouldSucceed()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;

        // Act
        var result = Weight.Create(3m, unit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(3m);
    }

    [Fact]
    public void Create_ZeroValue_ShouldSucceed()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;

        // Act
        var result = Weight.Create(0m, unit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(0m);
    }

    [Fact]
    public void Create_NegativeValue_ShouldFailWithValueNegative()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;

        // Act
        var result = Weight.Create(-1m, unit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Weight.Value.Negative");
    }

    [Fact]
    public void Create_InvalidUnit_ShouldFailWithUnitInvalid()
    {
        // Arrange
        var unit = new MeasurementUnit("123");

        // Act
        var result = Weight.Create(3m, unit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Weight.Unit.Invalid");
    }

    [Fact]
    public void RecordEquality_ShouldCompareValueAndUnit()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var weight = Weight.Create(3m, unit).Value;
        var equivalent = Weight.Create(3m, unit).Value;
        var changed = Weight.Create(4m, unit).Value;

        // Act & Assert
        weight.Should().Be(equivalent);
        weight.Should().NotBe(changed);
    }
}