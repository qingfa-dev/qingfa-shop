using Application.Common.Querying.Grouping;

namespace Application.Unit.Tests.Common.Querying.Grouping;


public sealed class GroupByFieldTest
{
    [Fact(DisplayName = "Constructor With Field Sets Property")]
    public void Constructor_WithField_SetsProperty()
    {
        var field = new GroupByField("kind");

        field.Field.Should().Be("kind");
    }

    [Theory(DisplayName = "Constructor Various Fields All Valid")]
    [InlineData("kind")]
    [InlineData("category")]
    [InlineData("nested.field")]
    public void Constructor_VariousFields_AllValid(string fieldName)
    {
        var field = new GroupByField(fieldName);

        field.Field.Should().Be(fieldName);
    }

    [Fact(DisplayName = "Record Equality Same Values Are Equal")]
    public void RecordEquality_SameValues_AreEqual()
    {
        var f1 = new GroupByField("kind");
        var f2 = new GroupByField("kind");

        f1.Should().Be(f2);
    }

    [Fact(DisplayName = "Record Equality Different Values Are Not Equal")]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var f1 = new GroupByField("kind");
        var f2 = new GroupByField("category");

        f1.Should().NotBe(f2);
    }

    [Fact(DisplayName = "With Expression Creates New Record With Changes")]
    public void WithExpression_CreatesNewRecordWithChanges()
    {
        var original = new GroupByField("kind");
        var modified = original with { Field = "category" };

        modified.Field.Should().Be("category");
        original.Field.Should().Be("kind");
    }

    [Fact(DisplayName = "Deconstruction Works Correctly")]
    public void Deconstruction_WorksCorrectly()
    {
        var field = new GroupByField("kind");
        string fieldName = field.Field;

        fieldName.Should().Be("kind");
    }
}