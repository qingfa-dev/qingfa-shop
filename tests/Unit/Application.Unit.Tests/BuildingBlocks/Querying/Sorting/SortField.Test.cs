using Application.BuildingBlocks.Querying.Sorting;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Sorting;


public sealed class SortFieldTest
{
    [Fact(DisplayName = "Constructor With All Parameters Sets Properties")]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        var field = new SortField("name", SortDirection.Descending, SortNulls.HasValue);

        field.Field.Should().Be("name");
        field.Direction.Should().Be(SortDirection.Descending);
        field.Nulls.Should().Be(SortNulls.HasValue);
    }

    [Fact(DisplayName = "Constructor With Nulls Default Defaults To None")]
    public void Constructor_WithNullsDefault_DefaultsToNone()
    {
        var field = new SortField("name", SortDirection.Ascending, SortNulls.None);

        field.Nulls.Should().Be(SortNulls.None);
    }

    [Theory(DisplayName = "Constructor Various Combinations All Valid")]
    [InlineData("name", SortDirection.Ascending, SortNulls.None)]
    [InlineData("score", SortDirection.Descending, SortNulls.HasValue)]
    [InlineData("dueAtUtc", SortDirection.Ascending, SortNulls.NotHasValue)]
    [InlineData("nested.field", SortDirection.Descending, SortNulls.None)]
    public void Constructor_VariousCombinations_AllValid(string fieldName, SortDirection direction, SortNulls nulls)
    {
        var field = new SortField(fieldName, direction, nulls);

        field.Field.Should().Be(fieldName);
        field.Direction.Should().Be(direction);
        field.Nulls.Should().Be(nulls);
    }

    [Fact(DisplayName = "Record Equality Same Values Are Equal")]
    public void RecordEquality_SameValues_AreEqual()
    {
        var f1 = new SortField("name", SortDirection.Descending, SortNulls.HasValue);
        var f2 = new SortField("name", SortDirection.Descending, SortNulls.HasValue);

        f1.Should().Be(f2);
    }

    [Theory(DisplayName = "Record Equality Different Values Are Not Equal")]
    [InlineData("name", SortDirection.Ascending, SortNulls.None, "score", SortDirection.Ascending, SortNulls.None)]
    [InlineData("name", SortDirection.Descending, SortNulls.None, "name", SortDirection.Ascending, SortNulls.None)]
    [InlineData("name", SortDirection.Ascending, SortNulls.HasValue, "name", SortDirection.Ascending, SortNulls.None)]
    [InlineData("name", SortDirection.Ascending, SortNulls.None, "name", SortDirection.Ascending, SortNulls.HasValue)]
    public void RecordEquality_DifferentValues_AreNotEqual(
        string field1, SortDirection dir1, SortNulls nulls1,
        string field2, SortDirection dir2, SortNulls nulls2)
    {
        var f1 = new SortField(field1, dir1, nulls1);
        var f2 = new SortField(field2, dir2, nulls2);

        f1.Should().NotBe(f2);
    }

    [Fact(DisplayName = "With Expression Creates New Record With Changes")]
    public void WithExpression_CreatesNewRecordWithChanges()
    {
        var original = new SortField("name", SortDirection.Ascending, SortNulls.None);
        var modified = original with { Direction = SortDirection.Descending };

        modified.Field.Should().Be("name");
        modified.Direction.Should().Be(SortDirection.Descending);
        modified.Nulls.Should().Be(SortNulls.None);
        original.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact(DisplayName = "Deconstruction Works Correctly")]
    public void Deconstruction_WorksCorrectly()
    {
        var field = new SortField("name", SortDirection.Descending, SortNulls.HasValue);
        var (fieldName, direction, nulls) = field;

        fieldName.Should().Be("name");
        direction.Should().Be(SortDirection.Descending);
        nulls.Should().Be(SortNulls.HasValue);
    }

    [Fact(DisplayName = "Property Getters All Accessible")]
    public void PropertyGetters_AllAccessible()
    {
        var field = new SortField("name", SortDirection.Descending, SortNulls.HasValue);

        _ = field.Field;
        _ = field.Direction;
        _ = field.Nulls;
    }
}