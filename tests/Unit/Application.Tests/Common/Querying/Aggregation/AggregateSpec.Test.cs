using Application.Common.Querying.Aggregation;

namespace Application.Tests.Common.Querying.Aggregation;

public sealed class AggregateSpecTest
{
    [Fact]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        var spec = new AggregateSpec(AggregateFunction.Sum, "score", "sum(score)");

        spec.Function.Should().Be(AggregateFunction.Sum);
        spec.Field.Should().Be("score");
        spec.Alias.Should().Be("sum(score)");
    }

    [Theory]
    [InlineData(AggregateFunction.Sum, "score", "sum(score)")]
    [InlineData(AggregateFunction.Average, "score", "avg(score)")]
    [InlineData(AggregateFunction.Minimum, "score", "min(score)")]
    [InlineData(AggregateFunction.Maximum, "score", "max(score)")]
    [InlineData(AggregateFunction.Count, "name", "count(name)")]
    [InlineData(AggregateFunction.DistinctCount, "name", "distinctcount(name)")]
    public void Constructor_AllFunctions_AllValid(AggregateFunction fn, string field, string alias)
    {
        var spec = new AggregateSpec(fn, field, alias);

        spec.Function.Should().Be(fn);
        spec.Field.Should().Be(field);
        spec.Alias.Should().Be(alias);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var s1 = new AggregateSpec(AggregateFunction.Sum, "score", "sum(score)");
        var s2 = new AggregateSpec(AggregateFunction.Sum, "score", "sum(score)");

        s1.Should().Be(s2);
    }

    [Theory]
    [InlineData(AggregateFunction.Sum, "score", "sum(score)", AggregateFunction.Average, "score", "avg(score)")]
    [InlineData(AggregateFunction.Sum, "score", "sum(score)", AggregateFunction.Sum, "name", "sum(name)")]
    [InlineData(AggregateFunction.Sum, "score", "sum(score)", AggregateFunction.Sum, "score", "total")]
    public void RecordEquality_DifferentValues_AreNotEqual(
        AggregateFunction fn1, string field1, string alias1,
        AggregateFunction fn2, string field2, string alias2)
    {
        var s1 = new AggregateSpec(fn1, field1, alias1);
        var s2 = new AggregateSpec(fn2, field2, alias2);

        s1.Should().NotBe(s2);
    }

    [Fact]
    public void WithExpression_CreatesNewRecordWithChanges()
    {
        var original = new AggregateSpec(AggregateFunction.Sum, "score", "sum(score)");
        var modified = original with { Function = AggregateFunction.Average };

        modified.Function.Should().Be(AggregateFunction.Average);
        original.Function.Should().Be(AggregateFunction.Sum);
    }

    [Fact]
    public void Deconstruction_WorksCorrectly()
    {
        var spec = new AggregateSpec(AggregateFunction.Sum, "score", "sum(score)");
        var (fn, field, alias) = spec;

        fn.Should().Be(AggregateFunction.Sum);
        field.Should().Be("score");
        alias.Should().Be("sum(score)");
    }

    [Fact]
    public void PropertyGetters_AllAccessible()
    {
        var spec = new AggregateSpec(AggregateFunction.Sum, "score", "sum(score)");

        _ = spec.Function;
        _ = spec.Field;
        _ = spec.Alias;
    }
}