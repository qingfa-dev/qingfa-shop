using Core.Common.Domain.ValueObjects.Percentage;

namespace Core.Tests.Common.Domain.ValueObjects.Percentage;

public class PercentageConstantSpec
{
    [Fact]
    public void MinValue_ShouldBeZero()
    {
        PercentageConstant.Constraints.Value.MinValue.Should().Be(0m);
    }

    [Fact]
    public void MaxValue_ShouldBeOneHundred()
    {
        PercentageConstant.Constraints.Value.MaxValue.Should().Be(100m);
    }

    [Fact]
    public void Zero_ShouldBeZero()
    {
        PercentageConstant.Defaults.Zero.Should().Be(0m);
    }

    [Fact]
    public void Hundred_ShouldBeOneHundred()
    {
        PercentageConstant.Defaults.Hundred.Should().Be(100m);
    }
}