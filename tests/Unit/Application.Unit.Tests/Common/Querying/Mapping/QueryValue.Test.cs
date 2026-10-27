using Application.Common.Querying.Mapping;

namespace Application.Unit.Tests.Common.Querying.Mapping;
using Xunit;

public sealed class QueryValueTest
{
    [Fact(DisplayName = "Constructor With Null Value Creates Wrapper")]
    public void Constructor_WithNullValue_CreatesWrapper()
    {
        var value = new QueryValue(null);

        value.Value.Should().BeNull();
    }

    [Fact(DisplayName = "Constructor With String Value Creates Wrapper")]
    public void Constructor_WithStringValue_CreatesWrapper()
    {
        var value = new QueryValue("hello");

        value.Value.Should().Be("hello");
    }

    [Fact(DisplayName = "Constructor With Int Value Creates Wrapper")]
    public void Constructor_WithIntValue_CreatesWrapper()
    {
        var value = new QueryValue(42);

        value.Value.Should().Be(42);
    }

    [Fact(DisplayName = "Constructor With Guid Value Creates Wrapper")]
    public void Constructor_WithGuidValue_CreatesWrapper()
    {
        var guid = Guid.NewGuid();
        var value = new QueryValue(guid);

        value.Value.Should().Be(guid);
    }

    [Fact(DisplayName = "Record Equality Same Null Values Are Equal")]
    public void RecordEquality_SameNullValues_AreEqual()
    {
        var v1 = new QueryValue((object?)null);
        var v2 = new QueryValue((object?)null);

        v1.Should().Be(v2);
    }

    [Fact(DisplayName = "Record Equality Same String Values Are Equal")]
    public void RecordEquality_SameStringValues_AreEqual()
    {
        var v1 = new QueryValue("hello");
        var v2 = new QueryValue("hello");

        v1.Should().Be(v2);
    }

    [Fact(DisplayName = "Record Equality Different Values Are Not Equal")]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var v1 = new QueryValue("hello");
        var v2 = new QueryValue("world");

        v1.Should().NotBe(v2);
    }

    [Fact(DisplayName = "Record Equality Null Vs Non Null Are Not Equal")]
    public void RecordEquality_NullVsNonNull_AreNotEqual()
    {
        var v1 = new QueryValue((object?)null);
        var v2 = new QueryValue("hello");

        v1.Should().NotBe(v2);
    }

    [Fact(DisplayName = "With Expression Creates New Record With Changes")]
    public void WithExpression_CreatesNewRecordWithChanges()
    {
        var original = new QueryValue("hello");
        var modified = original with { Value = "world" };

        modified.Value.Should().Be("world");
        original.Value.Should().Be("hello");
    }

    [Fact(DisplayName = "Deconstruction Works Correctly")]
    public void Deconstruction_WorksCorrectly()
    {
        var value = new QueryValue(42);
        object? val = value.Value;

        val.Should().Be(42);
    }
}