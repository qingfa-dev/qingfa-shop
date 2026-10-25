using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Parsing;

namespace Application.Tests.Common.Querying.Parsing;


public sealed class ParserStateTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    private static QueryMap<Target> Map() =>
        Build(QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name)
            .Map("score", x => x.Score));

    private static QueryMap<T> Build<T>(QueryMapBuilder<T> builder)
    {
        Result<QueryMap<T>> map = builder.Build();
        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    [Fact(DisplayName = "Constructor Initializes Counters To Zero")]
    public void Constructor_InitializesCountersToZero()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);

        state.Conditions.Should().Be(0);
        state.Depth.Should().Be(0);
    }

    [Fact(DisplayName = "Field With Mapped Name Returns Field")]
    public void Field_WithMappedName_ReturnsField()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);

        Result<QueryField> result = state.Field("name");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("name");
    }

    [Fact(DisplayName = "Field With Unmapped Name Returns Failure")]
    public void Field_WithUnmappedName_ReturnsFailure()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);

        Result<QueryField> result = state.Field("unknown");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Enter Depth Within Limit Returns Success")]
    public void EnterDepth_WithinLimit_ReturnsSuccess()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);

        Result result = state.EnterDepth();

        result.IsSuccess.Should().BeTrue();
        state.Depth.Should().Be(1);
    }

    [Fact(DisplayName = "Enter Depth At Limit Returns Success")]
    public void EnterDepth_AtLimit_ReturnsSuccess()
    {
        var limits = new QueryParseLimits { MaxFilterDepth = 3 };
        var state = new ParserState<Target>(Map(), limits);

        for (int i = 0; i < 3; i++)
        {
            state.EnterDepth().IsSuccess.Should().BeTrue();
        }

        state.Depth.Should().Be(3);
    }

    [Fact(DisplayName = "Enter Depth Exceeds Limit Returns Failure")]
    public void EnterDepth_ExceedsLimit_ReturnsFailure()
    {
        var limits = new QueryParseLimits { MaxFilterDepth = 2 };
        var state = new ParserState<Target>(Map(), limits);

        state.EnterDepth().IsSuccess.Should().BeTrue();
        state.EnterDepth().IsSuccess.Should().BeTrue();
        Result result = state.EnterDepth();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.DepthExceeded");
    }

    [Fact(DisplayName = "Exit Depth Decrements Counter")]
    public void ExitDepth_DecrementsCounter()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);

        state.EnterDepth();
        state.EnterDepth();
        state.ExitDepth();

        state.Depth.Should().Be(1);
    }

    [Fact(DisplayName = "Add Condition Within Limit Returns Success")]
    public void AddCondition_WithinLimit_ReturnsSuccess()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);

        Result result = state.AddCondition();

        result.IsSuccess.Should().BeTrue();
        state.Conditions.Should().Be(1);
    }

    [Fact(DisplayName = "Add Condition At Limit Returns Success")]
    public void AddCondition_AtLimit_ReturnsSuccess()
    {
        var limits = new QueryParseLimits { MaxConditions = 3 };
        var state = new ParserState<Target>(Map(), limits);

        for (int i = 0; i < 3; i++)
        {
            state.AddCondition().IsSuccess.Should().BeTrue();
        }

        state.Conditions.Should().Be(3);
    }

    [Fact(DisplayName = "Add Condition Exceeds Limit Returns Failure")]
    public void AddCondition_ExceedsLimit_ReturnsFailure()
    {
        var limits = new QueryParseLimits { MaxConditions = 2 };
        var state = new ParserState<Target>(Map(), limits);

        state.AddCondition().IsSuccess.Should().BeTrue();
        state.AddCondition().IsSuccess.Should().BeTrue();
        Result result = state.AddCondition();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.TooManyConditions");
    }

    [Fact(DisplayName = "Options Are Accessible")]
    public void Options_AreAccessible()
    {
        var limits = new QueryParseLimits { MaxFilterLength = 100 };
        var state = new ParserState<Target>(Map(), limits);

        state.Options.MaxFilterLength.Should().Be(100);
    }

    [Fact(DisplayName = "Map Is Accessible")]
    public void Map_IsAccessible()
    {
        var map = Map();
        var state = new ParserState<Target>(map, QueryParseLimits.Default);

        state.Map.Should().BeSameAs(map);
    }
}