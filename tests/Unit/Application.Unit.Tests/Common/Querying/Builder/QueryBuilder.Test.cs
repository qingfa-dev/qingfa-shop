using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;

namespace Application.Unit.Tests.Common.Querying.Builder;

public sealed class QueryBuilderTest
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

    [Fact(DisplayName = "Create should produce an empty entity query")]
    public void Create_ShouldProduceEmptyQuery()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeNull();
        result.Value.Shape.Should().Be(QueryShape.Entity);
    }

    [Fact(DisplayName = "Where text should add a filter")]
    public void Where_Text_ShouldAddFilter()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Where("name=test")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>();
    }

    [Fact(DisplayName = "Where expression should add a filter")]
    public void Where_Expression_ShouldAddFilter()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Where(item => item.Active && item.Score > 5)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterGroup>();
    }

    [Fact(DisplayName = "Two Where calls should combine with AND")]
    public void Where_Twice_ShouldCombineWithAnd()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Where("active=true")
            .Where("score>5")
            .Build();

        result.IsSuccess.Should().BeTrue();
        var group = result.Value.Filter.Should().BeOfType<FilterGroup>().Subject;
        group.Logic.Should().Be(FilterLogic.And);
        group.Children.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TryWhere should return a failure for an unmapped field")]
    public void TryWhere_Unmapped_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryWhere("missing=1");

        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "FromParameters should preserve parsed parts")]
    public void FromParameters_ShouldPreserveParts()
    {
        var parameters = new QueryParameters
        {
            Filter = "active=true",
            OrderBy = "name",
            Page = 1,
            PageSize = 10,
        };

        var result = QueryBuilder<Item>.FromParameters(CreateMap(), parameters).Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().NotBeNull();
        result.Value.OrderBy.Should().ContainSingle();
        result.Value.Page.Should().Be(new PageRequest(1, 10));
    }
}