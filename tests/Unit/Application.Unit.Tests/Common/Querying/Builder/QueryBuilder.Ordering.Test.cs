using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Sorting;

namespace Application.Unit.Tests.Common.Querying.Builder;

public sealed class QueryBuilderOrderingTest
{
    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; }
        public int Score { get; set; }
    }

    private static QueryMap<Item> CreateMap() =>
        QueryMapBuilder<Item>.Create()
            .Map("name", item => item.Name, field => field.Searchable())
            .Map("active", item => item.Active, field => field.Groupable())
            .Map("score", item => item.Score)
            .Build().Value;

    [Fact(DisplayName = "OrderBy expression should replace ordering")]
    public void OrderBy_Expression_ShouldSetOrdering()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy(item => item.Name)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().ContainSingle()
            .Which.Field.Should().Be("name");
    }

    [Fact(DisplayName = "ThenBy should append ordering")]
    public void ThenBy_ShouldAppend()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy(item => item.Name)
            .ThenByDescending(item => item.Score)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().HaveCount(2);
        result.Value.OrderBy[1].Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "Page should validate the page size")]
    public void Page_WithInvalidSize_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Page(1, 0)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.PageSize.Invalid");
    }

    [Fact(DisplayName = "Search should require a searchable field")]
    public void Search_WithoutSearchableFields_ShouldFail()
    {
        var map = QueryMapBuilder<Item>.Create()
            .Map("score", item => item.Score)
            .Build().Value;

        var result = QueryBuilder<Item>.Create(map)
            .Search("test")
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Search.NotConfigured");
    }

    [Fact(DisplayName = "Options should merge")]
    public void Options_ShouldMerge()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Distinct()
            .MaxRows(10)
            .AsNoTracking(false)
            .Include("Children")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Options.Distinct.Should().BeTrue();
        result.Value.Options.MaxRows.Should().Be(10);
        result.Value.Options.AsNoTracking.Should().BeFalse();
        result.Value.Options.Includes.Should().Contain("Children");
    }
}