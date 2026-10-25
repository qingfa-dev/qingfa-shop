using Application.Common.Querying.Filtering;

namespace Application.Tests.Common.Querying.Filtering;

public sealed class FilterConditionTest
{
    [Fact]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        var condition = new FilterCondition(
            "name",
            FilterOperator.Equal,
            "John",
            "John",
            typeof(string),
            false,
            null,
            null);

        condition.Field.Should().Be("name");
        condition.Operator.Should().Be(FilterOperator.Equal);
        condition.RawValue.Should().Be("John");
        condition.Value.Should().Be("John");
        condition.ValueType.Should().Be(typeof(string));
        condition.CaseInsensitive.Should().BeFalse();
        condition.IndexValue.Should().BeNull();
        condition.CustomOperator.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCaseInsensitive_SetsFlag()
    {
        var condition = new FilterCondition(
            "name",
            FilterOperator.Contains,
            "john",
            "john",
            typeof(string),
            true,
            null,
            null);

        condition.CaseInsensitive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithIndexValue_SetsIndexValue()
    {
        var condition = new FilterCondition(
            "tags",
            FilterOperator.Equal,
            "urgent",
            "urgent",
            typeof(string),
            false,
            0,
            null);

        condition.IndexValue.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithCustomOperator_SetsCustomOperator()
    {
        var condition = new FilterCondition(
            "name",
            FilterOperator.Custom,
            "John;Jane",
            "John;Jane",
            typeof(string),
            false,
            null,
            "In");

        condition.Operator.Should().Be(FilterOperator.Custom);
        condition.CustomOperator.Should().Be("In");
    }

    [Theory]
    [InlineData(FilterOperator.Equal, "value", "value")]
    [InlineData(FilterOperator.NotEqual, "value", "value")]
    [InlineData(FilterOperator.Contains, "val", "val")]
    [InlineData(FilterOperator.GreaterThan, "10", 10)]
    [InlineData(FilterOperator.LessThan, "5", 5)]
    public void Constructor_VariousOperators_AllValid(FilterOperator op, string raw, object expectedValue)
    {
        var condition = new FilterCondition(
            "field",
            op,
            raw,
            expectedValue,
            expectedValue.GetType(),
            false,
            null,
            null);

        condition.Operator.Should().Be(op);
        condition.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var c1 = new FilterCondition("name", FilterOperator.Equal, "John", "John", typeof(string), false, null, null);
        var c2 = new FilterCondition("name", FilterOperator.Equal, "John", "John", typeof(string), false, null, null);

        c1.Should().Be(c2);
    }

    [Fact]
    public void RecordEquality_DifferentFields_AreNotEqual()
    {
        var c1 = new FilterCondition("name", FilterOperator.Equal, "a", "a", typeof(string), false, null, null);
        var c2 = new FilterCondition("score", FilterOperator.Equal, "a", "a", typeof(string), false, null, null);

        c1.Should().NotBe(c2);
    }

    [Fact]
    public void RecordEquality_DifferentOperators_AreNotEqual()
    {
        var c1 = new FilterCondition("name", FilterOperator.Equal, "a", "a", typeof(string), false, null, null);
        var c2 = new FilterCondition("name", FilterOperator.NotEqual, "a", "a", typeof(string), false, null, null);

        c1.Should().NotBe(c2);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var c1 = new FilterCondition("name", FilterOperator.Equal, "a", "a", typeof(string), false, null, null);
        var c2 = new FilterCondition("name", FilterOperator.Equal, "b", "b", typeof(string), false, null, null);

        c1.Should().NotBe(c2);
    }

    [Fact]
    public void WithExpression_CreatesNewRecordWithChanges()
    {
        var original = new FilterCondition("name", FilterOperator.Equal, "John", "John", typeof(string), false, null, null);
        var modified = original with { Operator = FilterOperator.NotEqual };

        modified.Operator.Should().Be(FilterOperator.NotEqual);
        original.Operator.Should().Be(FilterOperator.Equal);
    }

    [Fact]
    public void Deconstruction_WorksCorrectly()
    {
        var condition = new FilterCondition("name", FilterOperator.Equal, "John", "John", typeof(string), true, 0, "In");
        var (field, op, raw, value, vtype, ci, idx, custom) = condition;

        field.Should().Be("name");
        op.Should().Be(FilterOperator.Equal);
        raw.Should().Be("John");
        value.Should().Be("John");
        vtype.Should().Be(typeof(string));
        ci.Should().BeTrue();
        idx.Should().Be(0);
        custom.Should().Be("In");
    }

    [Fact]
    public void InheritsFromFilterNode()
    {
        var condition = new FilterCondition("name", FilterOperator.Equal, "John", "John", typeof(string), false, null, null);

        condition.Should().BeAssignableTo<FilterNode>();
    }

    [Fact]
    public void PropertyGetters_AllAccessible()
    {
        var condition = new FilterCondition("name", FilterOperator.Equal, "John", "John", typeof(string), true, 0, "In");

        _ = condition.Field;
        _ = condition.Operator;
        _ = condition.RawValue;
        _ = condition.Value;
        _ = condition.ValueType;
        _ = condition.CaseInsensitive;
        _ = condition.IndexValue;
        _ = condition.CustomOperator;
    }
}