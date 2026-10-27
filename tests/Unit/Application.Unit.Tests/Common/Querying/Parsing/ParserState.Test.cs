using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Parsing;

namespace Application.Unit.Tests.Common.Querying.Parsing;


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

        var (newState, result) = state.EnterDepth();

        result.IsSuccess.Should().BeTrue();
        newState.Depth.Should().Be(1);
    }

    [Fact(DisplayName = "Enter Depth At Limit Returns Success")]
    public void EnterDepth_AtLimit_ReturnsSuccess()
    {
        var limits = new QueryParseLimits { MaxFilterDepth = 3 };
        var state = new ParserState<Target>(Map(), limits);

        for (int i = 0; i < 3; i++)
        {
            var (next, result) = state.EnterDepth();
            result.IsSuccess.Should().BeTrue();
            state = next;
        }

        state.Depth.Should().Be(3);
    }

    [Fact(DisplayName = "Enter Depth Exceeds Limit Returns Failure")]
    public void EnterDepth_ExceedsLimit_ReturnsFailure()
    {
        var limits = new QueryParseLimits { MaxFilterDepth = 2 };
        var state = new ParserState<Target>(Map(), limits);

        var (s1, r1) = state.EnterDepth();
        r1.IsSuccess.Should().BeTrue();
        state = s1;

        var (s2, r2) = state.EnterDepth();
        r2.IsSuccess.Should().BeTrue();
        state = s2;

        var (s3, r3) = state.EnterDepth();
        r3.IsFailure.Should().BeTrue();
        r3.Errors.Should().Contain(e => e.Code == "Query.Filter.DepthExceeded");
    }

    [Fact(DisplayName = "Exit Depth Decrements Counter")]
    public void ExitDepth_DecrementsCounter()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);

        var (s1, _) = state.EnterDepth();
        var (s2, _) = s1.EnterDepth();
        var s3 = s2.ExitDepth();

        s3.Depth.Should().Be(1);
    }

    [Fact(DisplayName = "Add Condition Within Limit Returns Success")]
    public void AddCondition_WithinLimit_ReturnsSuccess()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);

        var (newState, result) = state.AddCondition();

        result.IsSuccess.Should().BeTrue();
        newState.Conditions.Should().Be(1);
    }

    [Fact(DisplayName = "Add Condition At Limit Returns Success")]
    public void AddCondition_AtLimit_ReturnsSuccess()
    {
        var limits = new QueryParseLimits { MaxConditions = 3 };
        var state = new ParserState<Target>(Map(), limits);

        for (int i = 0; i < 3; i++)
        {
            var (next, result) = state.AddCondition();
            result.IsSuccess.Should().BeTrue();
            state = next;
        }

        state.Conditions.Should().Be(3);
    }

    [Fact(DisplayName = "Add Condition Exceeds Limit Returns Failure")]
    public void AddCondition_ExceedsLimit_ReturnsFailure()
    {
        var limits = new QueryParseLimits { MaxConditions = 2 };
        var state = new ParserState<Target>(Map(), limits);

        var (s1, r1) = state.AddCondition();
        r1.IsSuccess.Should().BeTrue();
        state = s1;

        var (s2, r2) = state.AddCondition();
        r2.IsSuccess.Should().BeTrue();
        state = s2;

        var (s3, r3) = state.AddCondition();
        r3.IsFailure.Should().BeTrue();
        r3.Errors.Should().Contain(e => e.Code == "Query.Filter.TooManyConditions");
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

    [Fact(DisplayName = "State Is Immutable - Original Unchanged After EnterDepth")]
    public void State_IsImmutable_OriginalUnchangedAfterEnterDepth()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);

        var (newState, _) = state.EnterDepth();

        state.Depth.Should().Be(0);
        newState.Depth.Should().Be(1);
    }

    [Fact(DisplayName = "State Is Immutable - Original Unchanged After AddCondition")]
    public void State_IsImmutable_OriginalUnchangedAfterAddCondition()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);

        var (newState, _) = state.AddCondition();

        state.Conditions.Should().Be(0);
        newState.Conditions.Should().Be(1);
    }

    [Fact(DisplayName = "State Is Immutable - Original Unchanged After ExitDepth")]
    public void State_IsImmutable_OriginalUnchangedAfterExitDepth()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var (s1, _) = state.EnterDepth();

        var s2 = s1.ExitDepth();

        s1.Depth.Should().Be(1);
        s2.Depth.Should().Be(0);
    }
}