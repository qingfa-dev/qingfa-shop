using Core.Common.Domain.ValueObjects.Percentage;

namespace Core.Tests.Common.Domain.ValueObjects.Percentage;

public class PercentageValidatorSpec
{
    [Fact]
    public void Validate_InRange_ShouldSucceed()
    {
        // Arrange
        var candidate = new global::Core.Common.Domain.ValueObjects.Percentage.Percentage(42.5m);

        // Act
        var result = PercentageValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(42.5m);
    }

    [Fact]
    public void Validate_MinBoundary_ShouldSucceed()
    {
        // Arrange
        var candidate = new global::Core.Common.Domain.ValueObjects.Percentage.Percentage(0m);

        // Act
        var result = PercentageValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(0m);
    }

    [Fact]
    public void Validate_MaxBoundary_ShouldSucceed()
    {
        // Arrange
        var candidate = new global::Core.Common.Domain.ValueObjects.Percentage.Percentage(100m);

        // Act
        var result = PercentageValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(100m);
    }

    [Fact]
    public void Validate_JustBelowMin_ShouldFailWithOutOfRange()
    {
        // Arrange
        var candidate = new global::Core.Common.Domain.ValueObjects.Percentage.Percentage(-0.01m);

        // Act
        var result = PercentageValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Percentage.Value.OutOfRange");
    }

    [Fact]
    public void Validate_JustOverMax_ShouldFailWithOutOfRange()
    {
        // Arrange
        var candidate = new global::Core.Common.Domain.ValueObjects.Percentage.Percentage(100.01m);

        // Act
        var result = PercentageValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Percentage.Value.OutOfRange");
    }
}