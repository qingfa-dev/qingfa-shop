using Application.Common.Querying;
using Application.Common.Querying.Aggregation;

namespace Application.Tests.Common.Querying.Aggregation;


public sealed class AggregateFunctionTest
{
    [Theory(DisplayName = "Enum Has Expected Values")]
    [InlineData(AggregateFunction.Sum)]
    [InlineData(AggregateFunction.Average)]
    [InlineData(AggregateFunction.Minimum)]
    [InlineData(AggregateFunction.Maximum)]
    [InlineData(AggregateFunction.Count)]
    [InlineData(AggregateFunction.DistinctCount)]
    public void Enum_HasExpectedValues(AggregateFunction fn)
    {
        Enum.IsDefined(fn).Should().BeTrue();
    }

    [Fact(DisplayName = "Values Are Sequential")]
    public void Values_AreSequential()
    {
        ((int)AggregateFunction.Sum).Should().Be(0);
        ((int)AggregateFunction.Average).Should().Be(1);
        ((int)AggregateFunction.Minimum).Should().Be(2);
        ((int)AggregateFunction.Maximum).Should().Be(3);
        ((int)AggregateFunction.Count).Should().Be(4);
        ((int)AggregateFunction.DistinctCount).Should().Be(5);
    }

    [Fact(DisplayName = "Parse Valid String Returns Correct Enum")]
    public void Parse_ValidString_ReturnsCorrectEnum()
    {
        Enum.Parse<AggregateFunction>("Sum", ignoreCase: true).Should().Be(AggregateFunction.Sum);
        Enum.Parse<AggregateFunction>("Average", ignoreCase: true).Should().Be(AggregateFunction.Average);
        Enum.Parse<AggregateFunction>("Minimum", ignoreCase: true).Should().Be(AggregateFunction.Minimum);
        Enum.Parse<AggregateFunction>("Maximum", ignoreCase: true).Should().Be(AggregateFunction.Maximum);
        Enum.Parse<AggregateFunction>("Count", ignoreCase: true).Should().Be(AggregateFunction.Count);
        Enum.Parse<AggregateFunction>("DistinctCount", ignoreCase: true).Should().Be(AggregateFunction.DistinctCount);
    }

    [Fact(DisplayName = "To String Returns Expected Names")]
    public void ToString_ReturnsExpectedNames()
    {
        AggregateFunction.Sum.ToString().Should().Be("Sum");
        AggregateFunction.Average.ToString().Should().Be("Average");
        AggregateFunction.Minimum.ToString().Should().Be("Minimum");
        AggregateFunction.Maximum.ToString().Should().Be("Maximum");
        AggregateFunction.Count.ToString().Should().Be("Count");
        AggregateFunction.DistinctCount.ToString().Should().Be("DistinctCount");
    }

    [Fact(DisplayName = "Constant Names Match Enum")]
    public void ConstantNames_MatchEnum()
    {
        // Validate: parser constants match enum for consistency
        QueryConstant.Functions.Sum.Should().Be("sum");
        QueryConstant.Functions.Average.Should().Be("avg");
        QueryConstant.Functions.Minimum.Should().Be("min");
        QueryConstant.Functions.Maximum.Should().Be("max");
        QueryConstant.Functions.Count.Should().Be("count");
        QueryConstant.Functions.DistinctCount.Should().Be("distinctcount");
    }
}