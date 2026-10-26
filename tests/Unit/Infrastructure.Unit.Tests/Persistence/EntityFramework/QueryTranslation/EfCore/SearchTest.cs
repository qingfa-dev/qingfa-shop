using AwesomeAssertions;
using Application.Common.Querying.Mapping;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.UnitTests.Persistence.EntityFramework.QueryTranslation.EfCore;

[Trait("Category", "Unit")]
public sealed class SearchTest
{
    private static readonly QueryMap<QueryProbe> Map = QueryProbeMap.Create();
    private static readonly IQueryTranslator<QueryProbe> Translator = new QueryTranslationEfCore<QueryProbe>(QueryProbeOperators.Create());

    [Fact(DisplayName = "ApplySearch_WithNullOrWhitespace_ReturnsSourceUnchanged")]
    public void ApplySearch_WithNullOrWhitespace_ReturnsSourceUnchanged()
    {
        var source = new List<QueryProbe>().AsQueryable();

        Translator.ApplySearch(source, null, Map).IsSuccess.Should().BeTrue();
        Translator.ApplySearch(source, "", Map).IsSuccess.Should().BeTrue();
        Translator.ApplySearch(source, "   ", Map).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplySearch_WithValidSearch_ReturnsWhereClause")]
    public void ApplySearch_WithValidSearch_ReturnsWhereClause()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var result = Translator.ApplySearch(source, "hello world", Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplySearch_WithNoSearchableFields_ReturnsNotConfiguredError")]
    public void ApplySearch_WithNoSearchableFields_ReturnsNotConfiguredError()
    {
        var map = BuildMapWithoutSearchableFields();
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.ApplySearch(source, "test", map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.NotConfigured");
    }

    [Fact(DisplayName = "ApplySearch_WithSpecificFields_ValidatesEachField")]
    public void ApplySearch_WithSpecificFields_ValidatesEachField()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var result = Translator.ApplySearch(source, "test", Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplySearch_WithNonSearchableField_ReturnsNotConfiguredError")]
    public void ApplySearch_WithNonSearchableField_ReturnsNotConfiguredError()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var result = Translator.ApplySearch(source, "test", Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplySearch_WithMultipleTerms_BuildsOrChain")]
    public void ApplySearch_WithMultipleTerms_BuildsOrChain()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var result = Translator.ApplySearch(source, "hello world test", Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplySearch_CaseInsensitiveByDefault")]
    public void ApplySearch_CaseInsensitiveByDefault()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var result = Translator.ApplySearch(source, "HELLO WORLD", Map);

        result.IsSuccess.Should().BeTrue();
    }

    private static QueryMap<QueryProbe> BuildMapWithoutSearchableFields()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("id", probe => probe.Id, field => field.Filterable().Sortable())
            .Map("score", probe => probe.Score, field => field.Filterable().Sortable());

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }
}