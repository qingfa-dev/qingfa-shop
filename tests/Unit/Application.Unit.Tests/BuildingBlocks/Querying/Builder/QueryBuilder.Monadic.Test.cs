using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Pagination;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Builder;

public sealed class QueryBuilderMonadicTest
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

    [Fact(DisplayName = "Monadic chain should stop after the first failure")]
    public void Chain_AfterFailure_ShouldStayFailed()
    {
        var result = QueryBuilder<Item>.CreateResult(CreateMap())
            .Where("missing=1")
            .OrderBy(item => item.Name)
            .Page(1, 10)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().OnlyContain(error => error.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Monadic chain should build a successful query")]
    public void Chain_Success_ShouldBuild()
    {
        var result = QueryBuilder<Item>.CreateResult(CreateMap())
            .Where(item => item.Active)
            .OrderBy(item => item.Name)
            .Page(1, 10)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(new PageRequest(1, 10));
    }

    [Fact(DisplayName = "Monadic BuildTabular should preserve callers' shape error")]
    public void BuildTabular_ShouldUseCallerError()
    {
        var error = Error.BadRequest("Todos.Query.EntityUnsupported", "entity query");

        var result = QueryBuilder<Item>.CreateResult(CreateMap()).BuildTabular(error);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == "Todos.Query.EntityUnsupported");
    }
}