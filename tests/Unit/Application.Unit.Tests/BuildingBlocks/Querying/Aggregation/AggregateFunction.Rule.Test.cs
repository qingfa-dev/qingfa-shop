using Application.BuildingBlocks.Querying.Aggregation;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Aggregation;

public sealed class AggregateFunctionRuleTest
{
    [Theory(DisplayName = "Count should support every type")]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(DateTimeOffset))]
    public void IsSupported_Count_ShouldReturnTrue(Type type)
    {
        AggregateFunctionRule.IsSupported(AggregateFunction.Count, type).Should().BeTrue();
    }

    [Theory(DisplayName = "Sum should require a numeric type")]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(DateTimeOffset), false)]
    public void IsSupported_Sum_ShouldMatchType(Type type, bool expected)
    {
        AggregateFunctionRule.IsSupported(AggregateFunction.Sum, type).Should().Be(expected);
    }

    [Fact(DisplayName = "Minimum should require IComparable")]
    public void IsSupported_Minimum_ShouldRequireComparable()
    {
        AggregateFunctionRule.IsSupported(AggregateFunction.Minimum, typeof(string)).Should().BeTrue();
        AggregateFunctionRule.IsSupported(AggregateFunction.Minimum, typeof(object)).Should().BeFalse();
    }
}