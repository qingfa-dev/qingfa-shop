using System.Linq.Expressions;
using Application.BuildingBlocks.Querying.Mapping;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Mapping;


public sealed class QueryFieldTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public List<string> Tags { get; set; } = [];
        public Target? Child { get; set; }
    }

    [Fact(DisplayName = "Properties When Built Via Builder All Accessible")]
    public void Properties_WhenBuiltViaBuilder_AllAccessible()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name, b => b
                .Filterable()
                .Sortable()
                .Selectable()
                .Groupable()
                .Searchable()
                .CaseInsensitive()
                .AllowNullSearch(false)));

        map.TryGet("name", out QueryField? field).Should().BeTrue();

        field!.Name.Should().Be("name");
        field.ValueType.Should().Be(typeof(string));
        field.Filterable.Should().BeTrue();
        field.Sortable.Should().BeTrue();
        field.Selectable.Should().BeTrue();
        field.Groupable.Should().BeTrue();
        field.Searchable.Should().BeTrue();
        field.CaseInsensitive.Should().BeTrue();
        field.AllowNullSearch.Should().BeFalse();
    }

    [Fact(DisplayName = "Is Collection When Field Is List Returns True")]
    public void IsCollection_WhenFieldIsList_ReturnsTrue()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("tags", x => x.Tags));

        map.TryGet("tags", out QueryField? field).Should().BeTrue();

        field!.IsCollection.Should().BeTrue();
        field.ElementType.Should().Be(typeof(string));
    }

    [Fact(DisplayName = "Is Collection When Field Is Not Collection Returns False")]
    public void IsCollection_WhenFieldIsNotCollection_ReturnsFalse()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name));

        map.TryGet("name", out QueryField? field).Should().BeTrue();

        field!.IsCollection.Should().BeFalse();
        field.ElementType.Should().BeNull();
    }

    [Fact(DisplayName = "Comparison Type When Collection Returns Element Type")]
    public void ComparisonType_WhenCollection_ReturnsElementType()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("tags", x => x.Tags));

        map.TryGet("tags", out QueryField? field).Should().BeTrue();

        field!.ComparisonType.Should().Be(typeof(string));
    }

    [Fact(DisplayName = "Comparison Type When Not Collection Returns Value Type")]
    public void ComparisonType_WhenNotCollection_ReturnsValueType()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("score", x => x.Score));

        map.TryGet("score", out QueryField? field).Should().BeTrue();

        field!.ComparisonType.Should().Be(typeof(int));
    }

    [Fact(DisplayName = "Is Indexed When Indexed Field Returns True")]
    public void IsIndexed_WhenIndexedField_ReturnsTrue()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .MapIndexed<int>("tag", (t, i) => t.Tags[i]));

        map.TryGet("tag", out QueryField? field).Should().BeTrue();

        field!.IsIndexed.Should().BeTrue();
        field.IndexType.Should().Be(typeof(int));
    }

    [Fact(DisplayName = "Is Indexed When Not Indexed Field Returns False")]
    public void IsIndexed_WhenNotIndexedField_ReturnsFalse()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name));

        map.TryGet("name", out QueryField? field).Should().BeTrue();

        field!.IsIndexed.Should().BeFalse();
        field.IndexType.Should().BeNull();
    }

    [Fact(DisplayName = "Member Path For Nested Property Captures Path")]
    public void MemberPath_ForNestedProperty_CapturesPath()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("child.name", x => x.Child!.Name));

        map.TryGet("child.name", out QueryField? field).Should().BeTrue();

        field!.MemberPath.Should().Be("Child.Name");
    }

    [Fact(DisplayName = "Member Path For Top Level Property Is Property Name")]
    public void MemberPath_ForTopLevelProperty_IsPropertyName()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name));

        map.TryGet("name", out QueryField? field).Should().BeTrue();

        field!.MemberPath.Should().Be("Name");
    }

    [Fact(DisplayName = "Expression Is Captured For Simple Property")]
    public void Expression_IsCapturedForSimpleProperty()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name));

        map.TryGet("name", out QueryField? field).Should().BeTrue();

        field!.Expression.Should().NotBeNull();
        field.Expression!.Body.Should().BeAssignableTo<MemberExpression>()
            .Which.Member.Name.Should().Be("Name");
    }

    [Fact(DisplayName = "Expression Is Captured For Indexed Field")]
    public void Expression_IsCapturedForIndexedField()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .MapIndexed<int>("tag", (t, i) => t.Tags[i]));

        map.TryGet("tag", out QueryField? field).Should().BeTrue();

        field!.Expression.Should().NotBeNull();
    }

    [Fact(DisplayName = "Expression Is Null For Composite Field")]
    public void Expression_IsNullForCompositeField()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .MapComposite("any", [x => x.Name, x => x.Child!.Name]));

        map.TryGet("any", out QueryField? field).Should().BeTrue();

        field!.Expression.Should().BeNull();
        field.Composite.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Composite When Composite Field Has Members")]
    public void Composite_WhenCompositeField_HasMembers()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .MapComposite("any", [x => x.Name, x => x.Child!.Name]));

        map.TryGet("any", out QueryField? field).Should().BeTrue();

        field!.Composite.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Converter When Configured Is Stored")]
    public void Converter_WhenConfigured_IsStored()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("score", x => x.Score, b => b.ConvertWith(raw =>
                Result<QueryValue>.Success(new QueryValue(int.Parse(raw) * 2)))));

        map.TryGet("score", out QueryField? field).Should().BeTrue();

        field!.Converter.Should().NotBeNull();
        field.Converter!("5").Value.Value.Should().Be(10);
    }

    [Fact(DisplayName = "Converter When Not Configured Is Null")]
    public void Converter_WhenNotConfigured_IsNull()
    {
        var map = Build(QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name));

        map.TryGet("name", out QueryField? field).Should().BeTrue();

        field!.Converter.Should().BeNull();
    }

    private static QueryMap<T> Build<T>(QueryMapBuilder<T> builder)
    {
        Result<QueryMap<T>> map = builder.Build();
        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }
}