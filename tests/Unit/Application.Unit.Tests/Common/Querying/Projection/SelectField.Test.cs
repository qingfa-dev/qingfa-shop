using Application.Common.Querying.Projection;

namespace Application.Unit.Tests.Common.Querying.Projection;


public sealed class SelectFieldTest
{
    [Fact(DisplayName = "Constructor With Field And Alias Sets Properties")]
    public void Constructor_WithFieldAndAlias_SetsProperties()
    {
        var field = new SelectField("name", "title");

        field.Field.Should().Be("name");
        field.Alias.Should().Be("title");
    }

    [Fact(DisplayName = "Constructor With Same Field And Alias Sets Both")]
    public void Constructor_WithSameFieldAndAlias_SetsBoth()
    {
        var field = new SelectField("name", "name");

        field.Field.Should().Be("name");
        field.Alias.Should().Be("name");
    }

    [Theory(DisplayName = "Constructor Various Combinations All Valid")]
    [InlineData("name", "title")]
    [InlineData("score", "points")]
    [InlineData("nested.field", "nested")]
    [InlineData("field", "alias with spaces")]
    public void Constructor_VariousCombinations_AllValid(string fieldName, string alias)
    {
        var field = new SelectField(fieldName, alias);

        field.Field.Should().Be(fieldName);
        field.Alias.Should().Be(alias);
    }

    [Fact(DisplayName = "Record Equality Same Values Are Equal")]
    public void RecordEquality_SameValues_AreEqual()
    {
        var f1 = new SelectField("name", "title");
        var f2 = new SelectField("name", "title");

        f1.Should().Be(f2);
    }

    [Theory(DisplayName = "Record Equality Different Values Are Not Equal")]
    [InlineData("name", "title", "score", "title")]
    [InlineData("name", "title", "name", "points")]
    public void RecordEquality_DifferentValues_AreNotEqual(
        string field1, string alias1, string field2, string alias2)
    {
        var f1 = new SelectField(field1, alias1);
        var f2 = new SelectField(field2, alias2);

        f1.Should().NotBe(f2);
    }

    [Fact(DisplayName = "With Expression Creates New Record With Changes")]
    public void WithExpression_CreatesNewRecordWithChanges()
    {
        var original = new SelectField("name", "title");
        var modified = original with { Alias = "newTitle" };

        modified.Field.Should().Be("name");
        modified.Alias.Should().Be("newTitle");
        original.Alias.Should().Be("title");
    }

    [Fact(DisplayName = "Deconstruction Works Correctly")]
    public void Deconstruction_WorksCorrectly()
    {
        var field = new SelectField("name", "title");
        var (fieldName, alias) = field;

        fieldName.Should().Be("name");
        alias.Should().Be("title");
    }
}