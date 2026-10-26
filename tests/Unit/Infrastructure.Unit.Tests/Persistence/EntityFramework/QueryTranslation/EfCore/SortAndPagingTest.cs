using AwesomeAssertions;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Sorting;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.UnitTests.Persistence.EntityFramework.QueryTranslation.EfCore;

[Trait("Category", "Unit")]
public sealed class SortAndPagingTest
{
    private static readonly QueryMap<QueryProbe> Map = QueryProbeMap.Create();
    private static readonly IQueryTranslator<QueryProbe> Translator = new QueryTranslationEfCore<QueryProbe>(QueryProbeOperators.Create());

    [Fact(DisplayName = "ApplySort_WithEmptyOrder_ReturnsDefaultOrder")]
    public void ApplySort_WithEmptyOrder_ReturnsDefaultOrder()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var result = Translator.ApplySort(source, [], Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplySort_WithValidFields_BuildsOrderByChain")]
    public void ApplySort_WithValidFields_BuildsOrderByChain()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var order = new[]
        {
            new SortField("name", SortDirection.Ascending, SortNulls.None),
            new SortField("score", SortDirection.Descending, SortNulls.None)
        };

        var result = Translator.ApplySort(source, order, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplySort_WithInvalidField_ReturnsNotSortableError")]
    public void ApplySort_WithInvalidField_ReturnsNotSortableError()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var order = new[] { new SortField("tags", SortDirection.Ascending, SortNulls.None) };

        var result = Translator.ApplySort(source, order, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSortable");
    }

    [Fact(DisplayName = "ApplySort_WithNullStateOnNonNullableField_ReturnsSyntaxError")]
    public void ApplySort_WithNullStateOnNonNullableField_ReturnsSyntaxError()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var order = new[] { new SortField("score", SortDirection.Ascending, SortNulls.HasValue) };

        var result = Translator.ApplySort(source, order, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Theory(DisplayName = "ApplySort_WithValidNullState_ReturnsSuccess")]
    [InlineData(SortNulls.HasValue)]
    [InlineData(SortNulls.NotHasValue)]
    public void ApplySort_WithValidNullState_ReturnsSuccess(SortNulls nulls)
    {
        var source = new List<QueryProbe>().AsQueryable();
        var order = new[] { new SortField("name", SortDirection.Ascending, nulls) };

        var result = Translator.ApplySort(source, order, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplySort_WithMultipleFields_BuildsThenByChain")]
    public void ApplySort_WithMultipleFields_BuildsThenByChain()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var order = new[]
        {
            new SortField("name", SortDirection.Ascending, SortNulls.None),
            new SortField("score", SortDirection.Descending, SortNulls.NotHasValue),
            new SortField("createdAt", SortDirection.Ascending, SortNulls.HasValue)
        };

        var result = Translator.ApplySort(source, order, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplyPaging_WithValidPage_ReturnsSkipTake")]
    public void ApplyPaging_WithValidPage_ReturnsSkipTake()
    {
        var source = Enumerable.Range(0, 100).Select(i => new QueryProbe { Id = Guid.NewGuid(), Score = i }).AsQueryable();
        var page = new PageRequest(2, 20);

        var result = Translator.ApplyPaging(source, page);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(20);
    }

    [Fact(DisplayName = "ApplyPaging_WithFirstPage_ReturnsFirstPageSize")]
    public void ApplyPaging_WithFirstPage_ReturnsFirstPageSize()
    {
        var source = Enumerable.Range(0, 100).Select(i => new QueryProbe { Id = Guid.NewGuid(), Score = i }).AsQueryable();
        var page = new PageRequest(1, 25);

        var result = Translator.ApplyPaging(source, page);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(25);
    }

    [Fact(DisplayName = "ApplyDefaultOrder_WithIdProperty_OrdersById")]
    public void ApplyDefaultOrder_WithIdProperty_OrdersById()
    {
        var source = new List<QueryProbe>
        {
            new() { Id = Guid.NewGuid(), Score = 3 },
            new() { Id = Guid.NewGuid(), Score = 1 },
            new() { Id = Guid.NewGuid(), Score = 2 }
        }.AsQueryable();

        var result = new QueryTranslationEfCore<QueryProbe>().ApplyDefaultOrder(source);

        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "ApplyDefaultOrder_WithoutIdProperty_ReturnsSourceUnchanged")]
    public void ApplyDefaultOrder_WithoutIdProperty_ReturnsSourceUnchanged()
    {
        var source = new List<NoIdEntity> { new() { Value = 3 }, new() { Value = 1 } }.AsQueryable();

        var result = new QueryTranslationEfCore<NoIdEntity>().ApplyDefaultOrder(source);

        result.Should().BeSameAs(source);
    }

    private sealed class NoIdEntity
    {
        public int Value { get; init; }
    }
}