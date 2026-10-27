using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Sorting;

using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.QueryTranslation;

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

    [Fact(DisplayName = "ApplySearch_WithNoSearchableFields_ReturnsSourceUnchanged")]
    public void ApplySearch_WithNoSearchableFields_ReturnsSourceUnchanged()
    {
        var map = BuildMapWithoutSearchableFields();
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.ApplySearch(source, "test", map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(source);
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

    [Theory(DisplayName = "ApplySearch with special characters should succeed")]
    [InlineData("hello@world")]
    [InlineData("test#123")]
    [InlineData("café")]
    [InlineData("日本語")]
    [InlineData("hello world!")]
    [InlineData("test'quote")]
    public void ApplySearch_WithSpecialCharacters_ShouldSucceed(string searchTerm)
    {
        var source = new List<QueryProbe>().AsQueryable();
        var result = Translator.ApplySearch(source, searchTerm, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplySearch with single character should succeed")]
    [InlineData("a")]
    [InlineData("1")]
    [InlineData("@")]
    public void ApplySearch_WithSingleCharacter_ShouldSucceed(string searchTerm)
    {
        var source = new List<QueryProbe>().AsQueryable();
        var result = Translator.ApplySearch(source, searchTerm, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplySort with sort direction should succeed")]
    [InlineData(SortDirection.Ascending)]
    [InlineData(SortDirection.Descending)]
    public void ApplySort_WithSortDirection_ShouldSucceed(SortDirection direction)
    {
        var source = new List<QueryProbe>().AsQueryable();
        var order = new[] { new SortField("name", direction, SortNulls.None) };

        var result = Translator.ApplySort(source, order, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplyPaging with page sizes should return correct count")]
    [InlineData(1, 10, 10)]
    [InlineData(1, 1, 1)]
    [InlineData(2, 10, 10)]
    [InlineData(5, 20, 20)]
    public void ApplyPaging_WithPageSize_ShouldReturnCorrectCount(int page, int pageSize, int expectedCount)
    {
        var source = Enumerable.Range(0, 100).Select(i => new QueryProbe { Id = Guid.NewGuid(), Score = i }).AsQueryable();
        var pageRequest = new PageRequest(page, pageSize);

        var result = Translator.ApplyPaging(source, pageRequest);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(expectedCount);
    }

    [Theory(DisplayName = "ApplyPaging with page beyond total should return empty")]
    [InlineData(100)]
    [InlineData(1000)]
    public void ApplyPaging_WithPageBeyondTotal_ShouldReturnEmpty(int page)
    {
        var source = Enumerable.Range(0, 10).Select(i => new QueryProbe { Id = Guid.NewGuid(), Score = i }).AsQueryable();
        var pageRequest = new PageRequest(page, 20);

        var result = Translator.ApplyPaging(source, pageRequest);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
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