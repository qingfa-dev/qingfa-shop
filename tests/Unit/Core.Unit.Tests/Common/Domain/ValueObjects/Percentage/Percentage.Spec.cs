namespace Core.Unit.Tests.Common.Domain.ValueObjects.Percentage;

public class PercentageSpec
{
    [Fact]
    public void Create_ValueWithinRange_ShouldSucceed()
    {
        var result = global::Core.Common.Domain.ValueObjects.Percentage.Percentage.Create(42.5m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(42.5m);
    }

    [Fact]
    public void Create_ValueAboveRange_ShouldFailWithOutOfRange()
    {
        var result = global::Core.Common.Domain.ValueObjects.Percentage.Percentage.Create(101m);

        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Percentage.Value.OutOfRange");
    }

    [Fact]
    public void Create_MinBoundary_ShouldSucceed()
    {
        // Arrange & Act
        var result = global::Core.Common.Domain.ValueObjects.Percentage.Percentage.Create(0m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(0m);
    }

    [Fact]
    public void Create_MaxBoundary_ShouldSucceed()
    {
        // Arrange & Act
        var result = global::Core.Common.Domain.ValueObjects.Percentage.Percentage.Create(100m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(100m);
    }

    [Fact]
    public void Create_NegativeValue_ShouldFailWithOutOfRange()
    {
        // Arrange & Act
        var result = global::Core.Common.Domain.ValueObjects.Percentage.Percentage.Create(-0.01m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Percentage.Value.OutOfRange");
    }

    [Fact]
    public void Create_JustOverMax_ShouldFailWithOutOfRange()
    {
        // Arrange & Act
        var result = global::Core.Common.Domain.ValueObjects.Percentage.Percentage.Create(100.01m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Percentage.Value.OutOfRange");
    }
}