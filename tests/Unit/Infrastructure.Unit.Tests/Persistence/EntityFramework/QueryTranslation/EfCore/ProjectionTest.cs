using AwesomeAssertions;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Projection;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.UnitTests.Persistence.EntityFramework.QueryTranslation.EfCore;

[Trait("Category", "Unit")]
public sealed class ProjectionTest
{
    private static readonly QueryMap<QueryProbe> Map = QueryProbeMap.Create();
    private static readonly IQueryTranslator<QueryProbe> Translator = new QueryTranslationEfCore<QueryProbe>();

    [Fact(DisplayName = "BuildSelectPlan_WithValidSelectFields_ReturnsTabularPlan")]
    public void BuildSelectPlan_WithValidSelectFields_ReturnsTabularPlan()
    {
        var select = new[]
        {
            new SelectField("name", "title"),
            new SelectField("score", "points")
        };
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.BuildSelectPlan(source, select, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().NotBeNull();
        result.Value.Columns.Should().HaveCount(2);
    }

    [Fact(DisplayName = "BuildSelectPlan_WithEmptySelect_ReturnsEmptyPlan")]
    public void BuildSelectPlan_WithEmptySelect_ReturnsEmptyPlan()
    {
        var select = Array.Empty<SelectField>();
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.BuildSelectPlan(source, select, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().BeEmpty();
    }

    [Fact(DisplayName = "BuildSelectPlan_WithNonSelectableField_ReturnsNotSelectableError")]
    public void BuildSelectPlan_WithNonSelectableField_ReturnsNotSelectableError()
    {
        var select = new[] { new SelectField("tags", "tags") };
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.BuildSelectPlan(source, select, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSelectable");
    }

    [Fact(DisplayName = "BuildSelectPlan_WithUnknownField_ReturnsNotMappedError")]
    public void BuildSelectPlan_WithUnknownField_ReturnsNotMappedError()
    {
        var select = new[] { new SelectField("unknown", "alias") };
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.BuildSelectPlan(source, select, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "BuildSelectPlan_WithNonSelectableFlag_ReturnsNotSelectableError")]
    public void BuildSelectPlan_WithNonSelectableFlag_ReturnsNotSelectableError()
    {
        var map = BuildMapWithNonSelectableField();
        var select = new[] { new SelectField("internal", "alias") };
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.BuildSelectPlan(source, select, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSelectable");
    }

    [Fact(DisplayName = "BuildSelectPlan_WithIndexedField_ReturnsNotSelectableError")]
    public void BuildSelectPlan_WithIndexedField_ReturnsNotSelectableError()
    {
        var map = BuildMapWithIndexedField();
        var select = new[] { new SelectField("indexedField", "alias") };
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.BuildSelectPlan(source, select, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSelectable");
    }

    [Fact(DisplayName = "BuildSelectPlan_WithCollectionField_ReturnsNotSelectableError")]
    public void BuildSelectPlan_WithCollectionField_ReturnsNotSelectableError()
    {
        var map = BuildMapWithCollectionField();
        var select = new[] { new SelectField("tags", "alias") };
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.BuildSelectPlan(source, select, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSelectable");
    }

    [Fact(DisplayName = "BuildSelectPlan_WithAlias_UsesAliasInColumns")]
    public void BuildSelectPlan_WithAlias_UsesAliasInColumns()
    {
        var select = new[] { new SelectField("name", "title") };
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.BuildSelectPlan(source, select, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle(c => c.Name == "title");
    }

    [Fact(DisplayName = "BuildSelectPlan_WithoutAlias_UsesFieldNameAsAlias")]
    public void BuildSelectPlan_WithoutAlias_UsesFieldNameAsAlias()
    {
        var select = new[] { new SelectField("name") };
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.BuildSelectPlan(source, select, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle(c => c.Name == "name");
    }

    private static QueryMap<QueryProbe> BuildMapWithNonSelectableField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("internal", probe => probe.Id, field => field.Selectable(false));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithIndexedField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("indexedField", probe => probe.Score, field => field
                .Selectable()
                .IsIndexed(true));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithCollectionField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("tags", probe => probe.Tags!, field => field
                .Selectable()
                .IsCollection(true));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }
}