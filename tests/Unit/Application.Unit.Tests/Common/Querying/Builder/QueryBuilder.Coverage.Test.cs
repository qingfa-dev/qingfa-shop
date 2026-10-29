using Application.Common.Querying;
using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Projection;
using Application.Common.Querying.Searching;
using Application.Common.Querying.Sorting;

namespace Application.Unit.Tests.Common.Querying.Builder;

/// <summary>
/// Exercises the whole fluent and monadic builder surface so every overload keeps
/// its documented behaviour, including its typed failure path.
/// </summary>
public sealed class QueryBuilderCoverageTest
{
    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Score { get; set; }
        public bool Active { get; set; }
        public bool Flag { get; set; }
        public string Locked { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Extra { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string Ninth { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
    }

    private sealed record ProjectedItem(string Name);

    private sealed record CustomFilterNode : FilterNode;

    private static QueryMap<Item> CreateMap() =>
        QueryMapBuilder<Item>.Create()
            .Map("name", item => item.Name, field => field.Searchable().Groupable())
            .Map("description", item => item.Description, field => field.Searchable())
            .Map("score", item => item.Score, field => field.Groupable())
            .Map("active", item => item.Active, field => field.Groupable())
            .Map("flag", item => item.Flag, field => field.Filterable(false))
            .Map("locked", item => item.Locked, field => field.Filterable(false))
            .Map("secret", item => item.Secret, field => field.Selectable(false))
            .Map("code", item => item.Code, field => field.Sortable(false).Groupable(false))
            .Map("extra", item => item.Extra)
            .Map("note", item => item.Note)
            .Map("ninth", item => item.Ninth)
            .Map("tenantId", item => item.TenantId)
            .Map("createdAtUtc", item => item.CreatedAtUtc)
            .Build().Value;

    private static QueryMap<Item> CreateUnsearchableMap() =>
        QueryMapBuilder<Item>.Create()
            .Map("score", item => item.Score)
            .Build().Value;

    private static FilterCondition Condition(
        string field,
        FilterOperator op,
        object? value,
        Type type) =>
        new(field, op, value?.ToString(), value, type, false, null, null);

    private static FilterCondition ActiveCondition() =>
        Condition("active", FilterOperator.Equal, true, typeof(bool));

    private static QueryMapRegistry RegistryWith(QueryMap<Item> map)
    {
        var registry = new QueryMapRegistry();
        registry.Register(map).IsSuccess.Should().BeTrue();
        return registry;
    }

    // ---- Factories -----------------------------------------------------

    [Fact(DisplayName = "CreateResult should carry an empty entity query")]
    public void CreateResult_ShouldCarryEmptyQuery()
    {
        var result = QueryBuilder<Item>.CreateResult(CreateMap());

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Shape.Should().Be(QueryShape.Entity);
    }

    [Fact(DisplayName = "FromParameters with an unregistered provider should fail typed")]
    public void FromParameters_ProviderUnregistered_ShouldFail()
    {
        var result = QueryBuilder<Item>.FromParameters(
            new QueryMapRegistry(),
            new QueryParameters()).Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.MapNotRegistered);
    }

    [Fact(DisplayName = "FromParameters with a registered provider should parse the parameters")]
    public void FromParameters_ProviderRegistered_ShouldParse()
    {
        var result = QueryBuilder<Item>.FromParameters(
            RegistryWith(CreateMap()),
            new QueryParameters { Filter = "active=true" }).Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Field.Should().Be("active");
    }

    [Fact(DisplayName = "FromParametersResult with a map should carry the builder")]
    public void FromParametersResult_Map_ShouldCarryBuilder()
    {
        var result = QueryBuilder<Item>.FromParametersResult(
            CreateMap(),
            new QueryParameters { OrderBy = "name" });

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.OrderBy.Should().ContainSingle()
            .Which.Field.Should().Be("name");
    }

    [Fact(DisplayName = "FromParametersResult with a provider should carry the builder")]
    public void FromParametersResult_Provider_ShouldCarryBuilder()
    {
        var result = QueryBuilder<Item>.FromParametersResult(
            RegistryWith(CreateMap()),
            new QueryParameters { Page = 2, PageSize = 5 });

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Page.Should().Be(new PageRequest(2, 5));
    }

    [Fact(DisplayName = "AsResult should preserve a failed state")]
    public void AsResult_WhenFailed_ShouldPreserveErrors()
    {
        var failed = QueryBuilder<Item>.FromParameters(
            new QueryMapRegistry(),
            new QueryParameters());

        var result = failed.AsResult();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.MapNotRegistered);
    }

    // ---- Filters -------------------------------------------------------

    [Fact(DisplayName = "OrWhere text should combine with OR")]
    public void OrWhere_Text_ShouldCombineWithOr()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Where("active=true")
            .OrWhere("score>5")
            .Build();

        result.IsSuccess.Should().BeTrue();
        var group = result.Value.Filter.Should().BeOfType<FilterGroup>().Subject;
        group.Logic.Should().Be(FilterLogic.Or);
        group.Children.Should().HaveCount(2);
    }

    [Fact(DisplayName = "OrWhere node should combine with OR")]
    public void OrWhere_Node_ShouldCombineWithOr()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Where(ActiveCondition())
            .OrWhere(Condition("score", FilterOperator.GreaterThan, 5, typeof(int)))
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);
    }

    [Fact(DisplayName = "OrWhere expression should combine with OR")]
    public void OrWhere_Expression_ShouldCombineWithOr()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Where(item => item.Active)
            .OrWhere(item => item.Score > 5)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);
    }

    [Fact(DisplayName = "Where node should reject an unmapped field")]
    public void Where_NodeUnmapped_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Where(Condition("missing", FilterOperator.Equal, 1, typeof(int)))
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "Where node should reject a non-filterable field")]
    public void Where_NodeNotFilterable_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Where(Condition("locked", FilterOperator.Equal, "x", typeof(string)))
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotFilterable);
    }

    [Fact(DisplayName = "Where group should validate every child")]
    public void Where_GroupWithValidChildren_ShouldKeepGroup()
    {
        var group = new FilterGroup(FilterLogic.And,
        [
            Condition("tenantId", FilterOperator.Equal, Guid.Empty, typeof(Guid)),
            ActiveCondition(),
        ]);

        var result = QueryBuilder<Item>.Create(CreateMap()).Where(group).Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Children.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Where group should fail when a child is invalid")]
    public void Where_GroupWithInvalidChild_ShouldFail()
    {
        var group = new FilterGroup(FilterLogic.And,
        [
            ActiveCondition(),
            Condition("missing", FilterOperator.Equal, 1, typeof(int)),
        ]);

        var result = QueryBuilder<Item>.Create(CreateMap()).Where(group).Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "Where node should reject an unsupported node type")]
    public void Where_UnsupportedNode_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Where(new CustomFilterNode())
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "TryWhere should accept a typed node")]
    public void TryWhere_Node_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryWhere(ActiveCondition());

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Filter.Should().BeOfType<FilterCondition>();
    }

    [Fact(DisplayName = "TryWhere should reject an invalid typed node")]
    public void TryWhere_NodeInvalid_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryWhere(Condition("missing", FilterOperator.Equal, 1, typeof(int)));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "TryOrWhere should accept a typed node")]
    public void TryOrWhere_Node_ShouldSucceed()
    {
        var scoped = QueryBuilder<Item>.Create(CreateMap()).TryWhere(ActiveCondition());

        var result = scoped.Value.TryOrWhere(
            Condition("score", FilterOperator.GreaterThan, 5, typeof(int)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);
    }

    [Fact(DisplayName = "TryOrWhere text should combine with OR")]
    public void TryOrWhere_Text_ShouldCombineWithOr()
    {
        var scoped = QueryBuilder<Item>.Create(CreateMap()).TryWhere("active=true");

        var result = scoped.Value.TryOrWhere("score>5");

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);
    }

    [Fact(DisplayName = "TryWhere expression should add a filter")]
    public void TryWhere_Expression_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryWhere(item => item.Active);

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Filter.Should().BeOfType<FilterCondition>();
    }

    [Fact(DisplayName = "TryOrWhere expression should combine with OR")]
    public void TryOrWhere_Expression_ShouldSucceed()
    {
        var scoped = QueryBuilder<Item>.Create(CreateMap()).TryWhere(item => item.Active);

        var result = scoped.Value.TryOrWhere(item => item.Score > 5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);
    }

    [Fact(DisplayName = "TryWhere expression should surface a translation failure")]
    public void TryWhere_ExpressionNotFilterable_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryWhere(item => item.Flag);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotFilterable);
    }

    [Fact(DisplayName = "TryWhere should preserve a failed state without re-running the transform")]
    public void TryWhere_WhenStateFailed_ShouldPreserveFailure()
    {
        var failed = QueryBuilder<Item>.FromParameters(
            new QueryMapRegistry(),
            new QueryParameters());

        var result = failed.TryWhere("name=test");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.MapNotRegistered);
    }

    // ---- Monadic hooks -------------------------------------------------

    [Fact(DisplayName = "Bind should transform the query state")]
    public void Bind_ShouldTransform()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Bind(query => Result<Query<Item>>.Ok(query with { Page = new PageRequest(3, 7) }))
            .Build();

        result.Value.Page.Should().Be(new PageRequest(3, 7));
    }

    [Fact(DisplayName = "Bind should surface the binder failure")]
    public void Bind_WhenBinderFails_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Bind(_ => Result<Query<Item>>.Fail(QueryOutcome.Failure.PageInvalid))
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.PageInvalid);
    }

    [Fact(DisplayName = "Ensure should keep the query when the invariant holds")]
    public void Ensure_WhenInvariantHolds_ShouldKeepQuery()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Ensure(query => query.Page is null, Error.BadRequest("Test.Invariant", "paged"))
            .Build();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Ensure should fail when the invariant is violated")]
    public void Ensure_WhenInvariantViolated_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Ensure(_ => false, Error.BadRequest("Test.Invariant", "violated"))
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Test.Invariant");
    }

    [Fact(DisplayName = "TryBind should transform the query state")]
    public void TryBind_ShouldTransform()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryBind(query => Result<Query<Item>>.Ok(query with { Page = new PageRequest(2, 2) }));

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Page.Should().Be(new PageRequest(2, 2));
    }

    [Fact(DisplayName = "TryBind should surface the binder failure")]
    public void TryBind_WhenBinderFails_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryBind(_ => Result<Query<Item>>.Fail(QueryOutcome.Failure.PageInvalid));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.PageInvalid);
    }

    [Fact(DisplayName = "TryEnsure should keep a query that satisfies the invariant")]
    public void TryEnsure_WhenInvariantHolds_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryEnsure(_ => true, Error.BadRequest("Test.Invariant", "never"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "TryEnsure should fail when the invariant is violated")]
    public void TryEnsure_WhenInvariantViolated_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryEnsure(_ => false, Error.BadRequest("Test.Invariant", "violated"));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Test.Invariant");
    }

    // ---- Build ---------------------------------------------------------

    [Fact(DisplayName = "BuildEntity should succeed for an entity query")]
    public void BuildEntity_WhenEntity_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).BuildEntity();

        result.IsSuccess.Should().BeTrue();
        result.Value.Shape.Should().Be(QueryShape.Entity);
    }

    [Fact(DisplayName = "BuildEntity should report the caller supplied tabular error")]
    public void BuildEntity_WhenTabular_ShouldUseCallerError()
    {
        var error = Error.BadRequest("Todos.Query.TabularUnsupported", "tabular query");

        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select("name")
            .BuildEntity(error);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == "Todos.Query.TabularUnsupported");
    }

    // ---- Ordering ------------------------------------------------------

    [Fact(DisplayName = "OrderBy text should replace the ordering")]
    public void OrderBy_Text_ShouldReplaceOrdering()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy("name,-score")
            .Build();

        result.Value.OrderBy.Should().HaveCount(2);
        result.Value.OrderBy[0].Field.Should().Be("name");
        result.Value.OrderBy[1].Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "OrderBy text should fail for an unmapped field")]
    public void OrderBy_TextUnmapped_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy("missing")
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "OrderBy sort field should replace the ordering")]
    public void OrderBy_SortField_ShouldReplaceOrdering()
    {
        var field = new SortField("score", SortDirection.Descending, SortNulls.None);

        var result = QueryBuilder<Item>.Create(CreateMap()).OrderBy(field).Build();

        result.Value.OrderBy.Should().ContainSingle().Which.Should().Be(field);
    }

    [Fact(DisplayName = "OrderBy sort field should reject a non-sortable field")]
    public void OrderBy_SortFieldNotSortable_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy(new SortField("code", SortDirection.Ascending, SortNulls.None))
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotSortable);
    }

    [Fact(DisplayName = "OrderBy sort field should reject an unmapped field")]
    public void OrderBy_SortFieldUnmapped_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy(new SortField("missing", SortDirection.Ascending, SortNulls.None))
            .Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "OrderByDescending expression should sort descending")]
    public void OrderByDescending_Expression_ShouldSetDescending()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderByDescending(item => item.Score)
            .Build();

        result.Value.OrderBy.Should().ContainSingle()
            .Which.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "ThenBy text should append to the ordering")]
    public void ThenBy_Text_ShouldAppend()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy("name")
            .ThenBy("-score")
            .Build();

        result.Value.OrderBy.Should().HaveCount(2);
        result.Value.OrderBy[1].Field.Should().Be("score");
    }

    [Fact(DisplayName = "ThenBy sort field should append to the ordering")]
    public void ThenBy_SortField_ShouldAppend()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy("name")
            .ThenBy(new SortField("score", SortDirection.Ascending, SortNulls.None))
            .Build();

        result.Value.OrderBy.Should().HaveCount(2);
    }

    [Fact(DisplayName = "ThenBy expression should append to the ordering")]
    public void ThenBy_Expression_ShouldAppend()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Score)
            .Build();

        result.Value.OrderBy.Should().HaveCount(2);
        result.Value.OrderBy[1].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact(DisplayName = "ThenByDescending expression should append descending")]
    public void ThenByDescending_Expression_ShouldAppend()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy(item => item.Name)
            .ThenByDescending(item => item.Score)
            .Build();

        result.Value.OrderBy[1].Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "ThenBy should reject an ordering beyond the field limit")]
    public void ThenBy_ExceedingCapacity_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .OrderBy("name,description,score,active,locked,secret,extra,note")
            .ThenBy("ninth")
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.OrderSyntax);
    }

    [Fact(DisplayName = "TryOrderBy text should replace the ordering")]
    public void TryOrderBy_Text_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryOrderBy("name");

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.OrderBy.Should().ContainSingle();
    }

    [Fact(DisplayName = "TryOrderBy text should surface a parse failure")]
    public void TryOrderBy_TextUnmapped_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryOrderBy("missing");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "TryOrderBy expression should replace the ordering")]
    public void TryOrderBy_Expression_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryOrderBy(item => item.Name);

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.OrderBy.Should().ContainSingle()
            .Which.Field.Should().Be("name");
    }

    [Fact(DisplayName = "TryOrderBy sort field should replace the ordering")]
    public void TryOrderBy_SortField_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryOrderBy(new SortField("score", SortDirection.Ascending, SortNulls.None));

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.OrderBy.Should().ContainSingle()
            .Which.Field.Should().Be("score");
    }

    [Fact(DisplayName = "TryOrderByDescending expression should sort descending")]
    public void TryOrderByDescending_Expression_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryOrderByDescending(item => item.Score);

        result.Value.Build().Value.OrderBy.Should().ContainSingle()
            .Which.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "TryThenBy text should append to the ordering")]
    public void TryThenBy_Text_ShouldSucceed()
    {
        var ordered = QueryBuilder<Item>.Create(CreateMap()).TryOrderBy("name");

        var result = ordered.Value.TryThenBy("score");

        result.Value.Build().Value.OrderBy.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TryThenBy expression should append to the ordering")]
    public void TryThenBy_Expression_ShouldSucceed()
    {
        var ordered = QueryBuilder<Item>.Create(CreateMap()).TryOrderBy(item => item.Name);

        var result = ordered.Value.TryThenBy(item => item.Score);

        result.Value.Build().Value.OrderBy.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TryThenBy sort field should append to the ordering")]
    public void TryThenBy_SortField_ShouldSucceed()
    {
        var ordered = QueryBuilder<Item>.Create(CreateMap()).TryOrderBy("name");

        var result = ordered.Value.TryThenBy(
            new SortField("score", SortDirection.Ascending, SortNulls.None));

        result.Value.Build().Value.OrderBy.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TryThenByDescending expression should append descending")]
    public void TryThenByDescending_Expression_ShouldSucceed()
    {
        var ordered = QueryBuilder<Item>.Create(CreateMap()).TryOrderBy(item => item.Name);

        var result = ordered.Value.TryThenByDescending(item => item.Score);

        result.Value.Build().Value.OrderBy[1].Direction.Should().Be(SortDirection.Descending);
    }

    // ---- Paging --------------------------------------------------------

    [Fact(DisplayName = "Page should accept a typed request")]
    public void Page_Request_ShouldSetPage()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Page(new PageRequest(4, 25))
            .Build();

        result.Value.Page.Should().Be(new PageRequest(4, 25));
    }

    [Fact(DisplayName = "Page should reject a page below one")]
    public void Page_InvalidPage_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).Page(0, 10).Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.PageInvalid);
    }

    [Fact(DisplayName = "Page should reject a page size above the maximum")]
    public void Page_TooLargeSize_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Page(1, QueryConstant.Limits.Page.MaxSize + 1)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.PageSizeInvalid);
    }

    [Fact(DisplayName = "TryPage should set the page")]
    public void TryPage_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryPage(3, 15);

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Page.Should().Be(new PageRequest(3, 15));
    }

    [Fact(DisplayName = "TryPage should accept a typed request")]
    public void TryPage_Request_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryPage(new PageRequest(2, 30));

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Page.Should().Be(new PageRequest(2, 30));
    }

    [Fact(DisplayName = "TryPage should surface an invalid page")]
    public void TryPage_Invalid_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryPage(1, 0);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.PageSizeInvalid);
    }

    // ---- Search --------------------------------------------------------

    [Fact(DisplayName = "Search should clear the request for blank text")]
    public void Search_Blank_ShouldClear()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Search("hello")
            .Search("   ")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().BeNull();
    }

    [Fact(DisplayName = "Search should reject text beyond the length limit")]
    public void Search_TooLong_ShouldFail()
    {
        string text = new('a', QueryConstant.Limits.Search.MaxLength + 1);

        var result = QueryBuilder<Item>.Create(CreateMap()).Search(text).Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.SearchTooLong);
    }

    [Fact(DisplayName = "Search should reject more terms than allowed")]
    public void Search_TooManyTerms_ShouldFail()
    {
        string text = string.Join(' ', Enumerable.Repeat("term", QueryConstant.Limits.Search.MaxTerms + 1));

        var result = QueryBuilder<Item>.Create(CreateMap()).Search(text).Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.SearchTooManyTerms);
    }

    [Fact(DisplayName = "Search should require a searchable field")]
    public void Search_NoSearchableField_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateUnsearchableMap())
            .Search("hello")
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.SearchNotConfigured);
    }

    [Fact(DisplayName = "Search should build a basic request")]
    public void Search_Text_ShouldSetBasicRequest()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).Search("hello world").Build();

        var request = result.Value.Search.Should().BeOfType<BasicSearchRequest>().Subject;
        request.Query.Should().Be("hello world");
        request.Mode.Should().Be(SearchMode.Contains);
        request.GetTerms().Should().HaveCount(2);
    }

    [Fact(DisplayName = "Search should build a full-text request")]
    public void Search_FullText_ShouldSetFullTextRequest()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Search("hello", SearchMode.FullText)
            .Build();

        result.Value.Search.Should().BeOfType<FullTextSearchRequest>()
            .Which.Mode.Should().Be(SearchMode.FullText);
    }

    [Fact(DisplayName = "Search request should be stored when its fields are valid")]
    public void Search_Request_ShouldSetSearch()
    {
        var request = new BasicSearchRequest("hello", SearchMode.StartsWith)
        {
            Fields = ["name"],
        };

        var result = QueryBuilder<Item>.Create(CreateMap()).Search(request).Build();

        result.Value.Search.Should().BeSameAs(request);
    }

    [Fact(DisplayName = "Search request should fail when no field is searchable")]
    public void Search_RequestNoSearchable_ShouldFail()
    {
        var request = new BasicSearchRequest("hello", SearchMode.Contains);

        var result = QueryBuilder<Item>.Create(CreateUnsearchableMap())
            .Search(request)
            .Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.SearchNotConfigured);
    }

    [Fact(DisplayName = "Search request should reject an empty field list")]
    public void Search_RequestEmptyFields_ShouldFail()
    {
        var request = new BasicSearchRequest("hello", SearchMode.Contains) { Fields = [] };

        var result = QueryBuilder<Item>.Create(CreateMap()).Search(request).Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.SearchNotConfigured);
    }

    [Fact(DisplayName = "Search request should reject too many fields")]
    public void Search_RequestTooManyFields_ShouldFail()
    {
        var request = new BasicSearchRequest("hello", SearchMode.Contains)
        {
            Fields = [.. Enumerable.Repeat("name", QueryConstant.Limits.Search.MaxFields + 1)],
        };

        var result = QueryBuilder<Item>.Create(CreateMap()).Search(request).Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.SearchTooManyFields);
    }

    [Fact(DisplayName = "Search request should reject an unmapped field")]
    public void Search_RequestUnmappedField_ShouldFail()
    {
        var request = new BasicSearchRequest("hello", SearchMode.Contains)
        {
            Fields = ["missing"],
        };

        var result = QueryBuilder<Item>.Create(CreateMap()).Search(request).Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "Search request should reject a non-searchable field")]
    public void Search_RequestNonSearchableField_ShouldFail()
    {
        var request = new BasicSearchRequest("hello", SearchMode.Contains)
        {
            Fields = ["score"],
        };

        var result = QueryBuilder<Item>.Create(CreateMap()).Search(request).Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.SearchNotConfigured);
    }

    [Fact(DisplayName = "TrySearch should build the request")]
    public void TrySearch_Text_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TrySearch("hello");

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Search.Should().NotBeNull();
    }

    [Fact(DisplayName = "TrySearch should accept a request")]
    public void TrySearch_Request_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TrySearch(new BasicSearchRequest("hello", SearchMode.Contains));

        result.IsSuccess.Should().BeTrue();
        result.Value.Build().Value.Search.Should().NotBeNull();
    }

    // ---- Execution options ---------------------------------------------

    [Fact(DisplayName = "Distinct should set the flag")]
    public void Distinct_ShouldSetFlag()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).Distinct().Build();

        result.Value.Options.Distinct.Should().BeTrue();
    }

    [Fact(DisplayName = "Distinct should accept an explicit false")]
    public void Distinct_False_ShouldClearFlag()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Distinct()
            .Distinct(false)
            .Build();

        result.Value.Options.Distinct.Should().BeFalse();
    }

    [Fact(DisplayName = "AsNoTracking should accept an explicit false")]
    public void AsNoTracking_False_ShouldClearFlag()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).AsNoTracking(false).Build();

        result.Value.Options.AsNoTracking.Should().BeFalse();
    }

    [Fact(DisplayName = "MaxRows should set the hard row cap")]
    public void MaxRows_ShouldSetCap()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).MaxRows(42).Build();

        result.Value.Options.MaxRows.Should().Be(42);
    }

    [Fact(DisplayName = "MaxRows should reject a non-positive cap")]
    public void MaxRows_Invalid_ShouldThrow()
    {
        Action act = () => QueryBuilder<Item>.Create(CreateMap()).MaxRows(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Include should add a path once")]
    public void Include_Duplicate_ShouldAddOnce()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Include("Children")
            .Include("Children")
            .Build();

        result.Value.Options.Includes.Should().ContainSingle().Which.Should().Be("Children");
    }

    [Fact(DisplayName = "Include should reject a blank path")]
    public void Include_Blank_ShouldThrow()
    {
        Action act = () => QueryBuilder<Item>.Create(CreateMap()).Include(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "WithOptions should replace the execution options")]
    public void WithOptions_ShouldReplaceOptions()
    {
        var options = QueryExecutionOptions.Default with
        {
            AsSplitQuery = true,
            MaxRows = 99,
        };

        var result = QueryBuilder<Item>.Create(CreateMap())
            .Distinct()
            .WithOptions(options)
            .Build();

        result.Value.Options.Should().BeSameAs(options);
        result.Value.Options.Distinct.Should().BeFalse();
    }

    [Fact(DisplayName = "TryDistinct should set the flag")]
    public void TryDistinct_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryDistinct();

        result.Value.Build().Value.Options.Distinct.Should().BeTrue();
    }

    [Fact(DisplayName = "TryAsNoTracking should set the flag")]
    public void TryAsNoTracking_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryAsNoTracking(false);

        result.Value.Build().Value.Options.AsNoTracking.Should().BeFalse();
    }

    [Fact(DisplayName = "TryMaxRows should set the hard row cap")]
    public void TryMaxRows_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryMaxRows(7);

        result.Value.Build().Value.Options.MaxRows.Should().Be(7);
    }

    [Fact(DisplayName = "TryMaxRows should reject a non-positive cap")]
    public void TryMaxRows_Invalid_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryMaxRows(-1);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.BuilderMaxRowsInvalid);
    }

    [Fact(DisplayName = "TryMaxRows should preserve a failed state")]
    public void TryMaxRows_WhenStateFailed_ShouldPreserveFailure()
    {
        var failed = QueryBuilder<Item>.FromParameters(
            new QueryMapRegistry(),
            new QueryParameters());

        var result = failed.TryMaxRows(5);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.MapNotRegistered);
    }

    [Fact(DisplayName = "TryInclude should add the path")]
    public void TryInclude_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryInclude("Children");

        result.Value.Build().Value.Options.Includes.Should().ContainSingle();
    }

    [Fact(DisplayName = "TryInclude should reject a blank path")]
    public void TryInclude_Blank_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryInclude(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.BuilderIncludeInvalid);
    }

    [Fact(DisplayName = "TryInclude should preserve a failed state")]
    public void TryInclude_WhenStateFailed_ShouldPreserveFailure()
    {
        var failed = QueryBuilder<Item>.FromParameters(
            new QueryMapRegistry(),
            new QueryParameters());

        var result = failed.TryInclude("Children");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.MapNotRegistered);
    }

    [Fact(DisplayName = "TryWithOptions should replace the execution options")]
    public void TryWithOptions_ShouldSucceed()
    {
        var options = QueryExecutionOptions.Default with { MaxRows = 11 };

        var result = QueryBuilder<Item>.Create(CreateMap()).TryWithOptions(options);

        result.Value.Build().Value.Options.Should().BeSameAs(options);
    }

    // ---- Projection ----------------------------------------------------

    [Fact(DisplayName = "Select text should replace the columns")]
    public void Select_Text_ShouldSetColumns()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).Select("name,score").Build();

        result.Value.Select.Should().HaveCount(2);
        result.Value.Select![0].Field.Should().Be("name");
    }

    [Fact(DisplayName = "Select text should clear the columns when empty")]
    public void Select_TextEmpty_ShouldClear()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).Select("name").Select("").Build();

        result.Value.Select.Should().BeNull();
    }

    [Fact(DisplayName = "Select text should fail for an unmapped field")]
    public void Select_TextUnmapped_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).Select("missing").Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "Select typed fields should replace the columns")]
    public void Select_TypedFields_ShouldSetColumns()
    {
        IReadOnlyList<SelectField> fields = [new SelectField("name", "label")];

        var result = QueryBuilder<Item>.Create(CreateMap()).Select(fields).Build();

        result.Value.Select.Should().ContainSingle()
            .Which.Alias.Should().Be("label");
    }

    [Fact(DisplayName = "Select expression should reject a non-selectable field")]
    public void Select_ExpressionNotSelectable_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).Select(item => item.Secret).Build();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotSelectable);
    }

    [Fact(DisplayName = "Select typed fields should reject a null entry")]
    public void Select_TypedFieldNull_ShouldFail()
    {
        IReadOnlyList<SelectField> fields = [null!];

        var result = QueryBuilder<Item>.Create(CreateMap()).Select(fields).Build();

        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "Select typed fields should reject an empty field name")]
    public void Select_TypedFieldEmptyName_ShouldFail()
    {
        IReadOnlyList<SelectField> fields = [new SelectField(" ", "label")];

        var result = QueryBuilder<Item>.Create(CreateMap()).Select(fields).Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "Select typed fields should reject an unmapped field")]
    public void Select_TypedFieldUnmapped_ShouldFail()
    {
        IReadOnlyList<SelectField> fields = [new SelectField("missing", "label")];

        var result = QueryBuilder<Item>.Create(CreateMap()).Select(fields).Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "Select typed fields should reject a non-selectable field")]
    public void Select_TypedFieldNotSelectable_ShouldFail()
    {
        IReadOnlyList<SelectField> fields = [new SelectField("secret", "label")];

        var result = QueryBuilder<Item>.Create(CreateMap()).Select(fields).Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotSelectable);
    }

    [Fact(DisplayName = "AddSelect text should append the columns")]
    public void AddSelect_Text_ShouldAppend()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select("name")
            .AddSelect("score")
            .Build();

        result.Value.Select.Should().HaveCount(2);
    }

    [Fact(DisplayName = "AddSelect blank text should append nothing")]
    public void AddSelect_TextEmpty_ShouldNoop()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select("name")
            .AddSelect("")
            .Build();

        result.Value.Select.Should().ContainSingle();
    }

    [Fact(DisplayName = "AddSelect expression should append the column")]
    public void AddSelect_Expression_ShouldAppend()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select("name")
            .AddSelect(item => item.Score)
            .Build();

        result.Value.Select.Should().HaveCount(2);
    }

    [Fact(DisplayName = "AddSelect typed fields should append the columns")]
    public void AddSelect_TypedFields_ShouldAppend()
    {
        IReadOnlyList<SelectField> fields = [new SelectField("score", "score")];

        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select("name")
            .AddSelect(fields)
            .Build();

        result.Value.Select.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GroupBy text should replace the keys")]
    public void GroupBy_Text_ShouldSetKeys()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).GroupBy("name,score").Build();

        result.Value.GroupBy.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GroupBy text should clear the keys when empty")]
    public void GroupBy_TextEmpty_ShouldClear()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).GroupBy("name").GroupBy("").Build();

        result.Value.GroupBy.Should().BeNull();
    }

    [Fact(DisplayName = "GroupBy text should fail for an unmapped field")]
    public void GroupBy_TextUnmapped_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).GroupBy("missing").Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "GroupBy expression should expand the members")]
    public void GroupBy_Expression_ShouldSetKeys()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .GroupBy(item => new { item.Name, item.Score })
            .Build();

        result.Value.GroupBy!.Select(key => key.Field)
            .Should().BeEquivalentTo(["name", "score"]);
    }

    [Fact(DisplayName = "GroupBy expression should reject a non-groupable field")]
    public void GroupBy_ExpressionNotGroupable_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).GroupBy(item => item.Code).Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotGroupable);
    }

    [Fact(DisplayName = "AddGroupBy text should append the keys")]
    public void AddGroupBy_Text_ShouldAppend()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .GroupBy("name")
            .AddGroupBy("score")
            .Build();

        result.Value.GroupBy.Should().HaveCount(2);
    }

    [Fact(DisplayName = "AddGroupBy blank text should append nothing")]
    public void AddGroupBy_TextEmpty_ShouldNoop()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .GroupBy("name")
            .AddGroupBy("")
            .Build();

        result.Value.GroupBy.Should().ContainSingle();
    }

    [Fact(DisplayName = "Aggregate text should append the specs")]
    public void Aggregate_Text_ShouldAppend()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Aggregate("count(score),sum(score)")
            .Build();

        result.Value.Aggregate.Should().HaveCount(2);
        result.Value.Aggregate![0].Alias.Should().Be("count(score)");
    }

    [Fact(DisplayName = "Aggregate text should reject an unsupported function for the type")]
    public void Aggregate_TextUnsupported_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).Aggregate("sum(name)").Build();

        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.AggregateUnsupported);
    }

    [Fact(DisplayName = "Aggregate expression should use the supplied alias")]
    public void Aggregate_Expression_ShouldUseAlias()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Aggregate(AggregateFunction.Count, item => item.Score, "total")
            .Build();

        result.Value.Aggregate.Should().ContainSingle()
            .Which.Should().Be(new AggregateSpec(AggregateFunction.Count, "score", "total"));
    }

    [Fact(DisplayName = "Aggregate convenience methods should append every function")]
    public void Aggregate_ConvenienceMethods_ShouldAppendAll()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Count(item => item.Score)
            .Sum(item => item.Score)
            .Average(item => item.Score)
            .Min(item => item.Score)
            .Max(item => item.Score)
            .DistinctCount(item => item.Score)
            .Build();

        result.Value.Aggregate!.Select(spec => spec.Function).Should().BeEquivalentTo(
        [
            AggregateFunction.Count,
            AggregateFunction.Sum,
            AggregateFunction.Average,
            AggregateFunction.Minimum,
            AggregateFunction.Maximum,
            AggregateFunction.DistinctCount,
        ]);
    }

    [Fact(DisplayName = "TrySelect text should replace the columns")]
    public void TrySelect_Text_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TrySelect("name");

        result.Value.Build().Value.Select.Should().ContainSingle();
    }

    [Fact(DisplayName = "TrySelect text should surface a parse failure")]
    public void TrySelect_TextUnmapped_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TrySelect("missing");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryErrorCodes.FieldNotMapped);
    }

    [Fact(DisplayName = "TrySelect expression should replace the columns")]
    public void TrySelect_Expression_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TrySelect(item => item.Name);

        result.Value.Build().Value.Select.Should().ContainSingle();
    }

    [Fact(DisplayName = "TrySelect typed fields should replace the columns")]
    public void TrySelect_TypedFields_ShouldSucceed()
    {
        IReadOnlyList<SelectField> fields = [new SelectField("name", "name")];

        var result = QueryBuilder<Item>.Create(CreateMap()).TrySelect(fields);

        result.Value.Build().Value.Select.Should().ContainSingle();
    }

    [Fact(DisplayName = "TryAddSelect text should append the columns")]
    public void TryAddSelect_Text_ShouldSucceed()
    {
        var selected = QueryBuilder<Item>.Create(CreateMap()).TrySelect("name");

        var result = selected.Value.TryAddSelect("score");

        result.Value.Build().Value.Select.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TryAddSelect expression should append the column")]
    public void TryAddSelect_Expression_ShouldSucceed()
    {
        var selected = QueryBuilder<Item>.Create(CreateMap()).TrySelect("name");

        var result = selected.Value.TryAddSelect(item => item.Score);

        result.Value.Build().Value.Select.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TryAddSelect typed fields should append the columns")]
    public void TryAddSelect_TypedFields_ShouldSucceed()
    {
        IReadOnlyList<SelectField> fields = [new SelectField("score", "score")];

        var selected = QueryBuilder<Item>.Create(CreateMap()).TrySelect("name");

        var result = selected.Value.TryAddSelect(fields);

        result.Value.Build().Value.Select.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TryProject should store the projection")]
    public void TryProject_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryProject(item => item.Name);

        result.IsSuccess.Should().BeTrue();
        result.Value.BuildProjected<string>().Value.Projection.Should().NotBeNull();
    }

    [Fact(DisplayName = "TryGroupBy text should replace the keys")]
    public void TryGroupBy_Text_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap()).TryGroupBy("name");

        result.Value.Build().Value.GroupBy.Should().ContainSingle();
    }

    [Fact(DisplayName = "TryGroupBy expression should replace the keys")]
    public void TryGroupBy_Expression_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryGroupBy(item => item.Name);

        result.Value.Build().Value.GroupBy.Should().ContainSingle();
    }

    [Fact(DisplayName = "TryAddGroupBy text should append the keys")]
    public void TryAddGroupBy_Text_ShouldSucceed()
    {
        var grouped = QueryBuilder<Item>.Create(CreateMap()).TryGroupBy("name");

        var result = grouped.Value.TryAddGroupBy("score");

        result.Value.Build().Value.GroupBy.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TryAggregate expression should append the spec")]
    public void TryAggregate_Expression_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryAggregate(AggregateFunction.Count, item => item.Score);

        result.Value.Build().Value.Aggregate.Should().ContainSingle();
    }

    [Fact(DisplayName = "TryAggregate text should append the specs")]
    public void TryAggregate_Text_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .TryAggregate("count(score)");

        result.Value.Build().Value.Aggregate.Should().ContainSingle();
    }

    [Fact(DisplayName = "Try aggregate conveniences should append every function")]
    public void TryAggregate_ConvenienceMethods_ShouldAppendAll()
    {
        QueryBuilder<Item> builder = QueryBuilder<Item>.Create(CreateMap())
            .TryCount(item => item.Score).Value
            .TrySum(item => item.Score).Value
            .TryAverage(item => item.Score).Value
            .TryMin(item => item.Score).Value
            .TryMax(item => item.Score).Value
            .TryDistinctCount(item => item.Score).Value;

        builder.Build().Value.Aggregate.Should().HaveCount(6);
    }

    [Fact(DisplayName = "BuildProjected should reject a tabular query")]
    public void BuildProjected_WhenTabular_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select("name")
            .Project(item => item.Name)
            .BuildProjected<string>();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.BuilderEntityShapeRequired);
    }

    [Fact(DisplayName = "BuildProjected should reject a mismatched response type")]
    public void BuildProjected_WhenMismatched_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Project(item => item.Name)
            .BuildProjected<int>();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.BuilderProjectionMismatch);
    }

    [Fact(DisplayName = "BuildTabular should reject a stored projection")]
    public void BuildTabular_WithProjection_ShouldFail()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .Select("name")
            .Project(item => item.Name)
            .BuildTabular();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.BuilderShapeConflict);
    }

    [Fact(DisplayName = "BuildTabular should report the caller supplied entity error")]
    public void BuildTabular_WhenEntity_ShouldUseCallerError()
    {
        var error = Error.BadRequest("Todos.Query.EntityUnsupported", "entity query");

        var result = QueryBuilder<Item>.Create(CreateMap()).BuildTabular(error);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == "Todos.Query.EntityUnsupported");
    }

    [Fact(DisplayName = "BuildTabular should accept a grouped aggregate")]
    public void BuildTabular_GroupedAggregate_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.Create(CreateMap())
            .GroupBy("active")
            .Count(item => item.Score)
            .BuildTabular();

        result.IsSuccess.Should().BeTrue();
        result.Value.Source.Shape.Should().Be(QueryShape.Tabular);
    }

    // ---- Monadic extensions --------------------------------------------

    [Fact(DisplayName = "Every monadic extension should forward to its Try counterpart")]
    public void MonadicExtensions_ShouldCoverEveryCategory()
    {
        FilterCondition node = ActiveCondition();

        var result = QueryBuilder<Item>.CreateResult(CreateMap())
            .Where("name=test")
            .Where(node)
            .Where(item => item.Active)
            .OrWhere("name=other")
            .OrWhere(node)
            .OrWhere(item => item.Active)
            .BindQuery(query => Result<Query<Item>>.Ok(query))
            .EnsureQuery((Query<Item> _) => true, Error.BadRequest("Test.Invariant", "never"))
            .OrderBy("name")
            .OrderBy(item => item.Name)
            .OrderBy(new SortField("score", SortDirection.Ascending, SortNulls.None))
            .OrderByDescending(item => item.Score)
            .ThenBy("score")
            .ThenBy(item => item.Score)
            .ThenBy(new SortField("active", SortDirection.Ascending, SortNulls.None))
            .ThenByDescending(item => item.Score)
            .Page(1, 10)
            .Page(new PageRequest(2, 10))
            .Search("hello")
            .Search(new BasicSearchRequest("hello", SearchMode.Contains))
            .WithOptions(QueryExecutionOptions.Default)
            .Distinct()
            .AsNoTracking()
            .MaxRows(50)
            .Include("Children")
            .Select("name")
            .Select(item => item.Name)
            .Select([new SelectField("name", "name")])
            .AddSelect("score")
            .AddSelect(item => item.Extra)
            .AddSelect([new SelectField("note", "note")])
            .GroupBy("name")
            .GroupBy(item => item.Name)
            .AddGroupBy("active")
            .Aggregate(AggregateFunction.Count, item => item.Name, "total")
            .Aggregate("count(name)")
            .Count(item => item.Score)
            .Sum(item => item.Score)
            .Average(item => item.Score)
            .Min(item => item.Score)
            .Max(item => item.Score)
            .DistinctCount(item => item.Score)
            .Project(item => new ProjectedItem(item.Name))
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Select.Should().HaveCount(4);
        result.Value.GroupBy.Should().HaveCount(2);
        result.Value.Aggregate.Should().HaveCount(8);
        result.Value.Options.MaxRows.Should().Be(50);
    }

    [Fact(DisplayName = "Monadic BuildEntity should succeed for an entity query")]
    public void MonadicBuildEntity_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.CreateResult(CreateMap()).BuildEntity();

        result.Value.Shape.Should().Be(QueryShape.Entity);
    }

    [Fact(DisplayName = "Monadic BuildEntity should report the caller error")]
    public void MonadicBuildEntity_WhenTabular_ShouldUseCallerError()
    {
        var error = Error.BadRequest("Todos.Query.TabularUnsupported", "tabular query");

        var result = QueryBuilder<Item>.CreateResult(CreateMap())
            .Select("name")
            .BuildEntity(error);

        result.Errors.Should().ContainSingle(e => e.Code == "Todos.Query.TabularUnsupported");
    }

    [Fact(DisplayName = "Monadic BuildProjected should return the projection")]
    public void MonadicBuildProjected_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.CreateResult(CreateMap())
            .Project(item => new ProjectedItem(item.Name))
            .BuildProjected<Item, ProjectedItem>();

        result.IsSuccess.Should().BeTrue();
        result.Value.Projection.Should().NotBeNull();
    }

    [Fact(DisplayName = "Monadic BuildTabular should return the tabular query")]
    public void MonadicBuildTabular_ShouldSucceed()
    {
        var result = QueryBuilder<Item>.CreateResult(CreateMap())
            .GroupBy("active")
            .Count(item => item.Score)
            .BuildTabular();

        result.IsSuccess.Should().BeTrue();
        result.Value.Source.Shape.Should().Be(QueryShape.Tabular);
    }

    [Fact(DisplayName = "Monadic BuildTabular should report the caller error")]
    public void MonadicBuildTabular_WhenEntity_ShouldUseCallerError()
    {
        var error = Error.BadRequest("Todos.Query.EntityUnsupported", "entity query");

        var result = QueryBuilder<Item>.CreateResult(CreateMap()).BuildTabular(error);

        result.Errors.Should().ContainSingle(e => e.Code == "Todos.Query.EntityUnsupported");
    }
}