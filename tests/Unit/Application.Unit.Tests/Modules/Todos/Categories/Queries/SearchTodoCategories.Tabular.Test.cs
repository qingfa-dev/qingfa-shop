using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Projection;
using Application.BuildingBlocks.Querying.Sorting;
using Application.Modules.Todos.Categories.Queries;
using Application.Unit.Tests.Modules.Todos.Fixtures;

using Core.Modules.Todos.Categories;

namespace Application.Unit.Tests.Modules.Todos.Categories.Queries;

/// <summary>Unit tests for the tabular search categories slice.</summary>
public sealed class SearchTodoCategoriesTabularTest
{
    private static SearchTodoCategoriesTabular.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.CategoryProjectionRepository.Object, fixture.Registry, fixture.Tenant);

    private static IEnumerable<FilterNode> Flatten(FilterNode node) =>
        node is FilterGroup group
            ? group.Children.SelectMany(Flatten)
            : [node];

    [Fact(DisplayName = "Tabular search should return the projected columns and rows")]
    public async Task Handle_WithSelect_ShouldReturnTabularResult()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        var expected = new TabularResult(
            [new TabularColumn("name", typeof(string))],
            [new TabularRow(["Errands"])]);
        fixture.CategoryProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TabularResult>.Success(expected));

        // Act
        Result<SearchTodoCategoriesTabular.Response> result = await CreateHandler(fixture).Handle(
            new SearchTodoCategoriesTabular.UseCase(
                new SearchTodoCategoriesTabular.Request(new QueryParameters { Select = "name" })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle(column => column.Name == "name");
        result.Value.Rows.Should().ContainSingle();
        result.Value.Rows[0].Values[0].Should().Be("Errands");
    }

    [Fact(DisplayName = "Tabular search should accept a grouped aggregate")]
    public async Task Handle_WithGroupAndAggregate_ShouldReturnTabularResult()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        Query<TodoCategory>? captured = null;
        fixture.CategoryProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoCategory>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        Result<SearchTodoCategoriesTabular.Response> result = await CreateHandler(fixture).Handle(
            new SearchTodoCategoriesTabular.UseCase(
                new SearchTodoCategoriesTabular.Request(new QueryParameters
                {
                    GroupBy = "name",
                    Aggregate = "count(id)",
                })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        captured!.GroupBy.Should().NotBeNull();
        captured.Aggregate.Should().NotBeNull();
    }

    [Fact(DisplayName = "Tabular search should preserve the select, group, and aggregate clauses")]
    public async Task Handle_WithSelectGroupAndAggregate_ShouldPreserveTabularShape()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        Query<TodoCategory>? captured = null;
        fixture.CategoryProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoCategory>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        Result<SearchTodoCategoriesTabular.Response> result = await CreateHandler(fixture).Handle(
            new SearchTodoCategoriesTabular.UseCase(
                new SearchTodoCategoriesTabular.Request(new QueryParameters
                {
                    Select = "name",
                    GroupBy = "name",
                    Aggregate = "count(id)",
                })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        captured!.Select.Should().ContainSingle().Which.Field.Should().Be("name");
        captured.GroupBy.Should().ContainSingle().Which.Field.Should().Be("name");
        captured.Aggregate.Should().ContainSingle().Which.Field.Should().Be("id");
    }

    [Fact(DisplayName = "Tabular search should order by creation instant descending by default")]
    public async Task Handle_WithoutOrder_ShouldDefaultToCreatedAtDescending()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        Query<TodoCategory>? captured = null;
        fixture.CategoryProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoCategory>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        await CreateHandler(fixture).Handle(
            new SearchTodoCategoriesTabular.UseCase(
                new SearchTodoCategoriesTabular.Request(new QueryParameters { Select = "name" })),
            CancellationToken.None);

        // Assert
        SortField order = captured!.OrderBy.Should().ContainSingle().Subject;
        order.Field.Should().Be("createdAtUtc");
        order.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "Tabular search should keep the caller-supplied order over the default")]
    public async Task Handle_WithOrder_ShouldKeepCallerOrder()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        Query<TodoCategory>? captured = null;
        fixture.CategoryProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoCategory>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        await CreateHandler(fixture).Handle(
            new SearchTodoCategoriesTabular.UseCase(
                new SearchTodoCategoriesTabular.Request(new QueryParameters
                {
                    Select = "name",
                    OrderBy = "name",
                })),
            CancellationToken.None);

        // Assert
        SortField order = captured!.OrderBy.Should().ContainSingle().Subject;
        order.Field.Should().Be("name");
        order.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact(DisplayName = "Tabular search should scope the query to the tenant and hide deleted rows")]
    public async Task Handle_ShouldApplyTenantScope()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        Query<TodoCategory>? captured = null;
        fixture.CategoryProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoCategory>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        await CreateHandler(fixture).Handle(
            new SearchTodoCategoriesTabular.UseCase(
                new SearchTodoCategoriesTabular.Request(new QueryParameters { Select = "name" })),
            CancellationToken.None);

        // Assert
        captured!.Filter.Should().NotBeNull();
        IEnumerable<FilterCondition> conditions = Flatten(captured.Filter!).OfType<FilterCondition>();
        conditions.Should().Contain(condition =>
            condition.Field == "tenantId" &&
            Equals(condition.Value, TodoHandlerFixture.TenantId));
        conditions.Should().Contain(condition =>
            condition.Field == "isDeleted" &&
            Equals(condition.Value, false));
    }

    [Fact(DisplayName = "Tabular search should include deleted rows when asked")]
    public async Task Handle_WithIncludeDeleted_ShouldSkipDeletionScope()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        Query<TodoCategory>? captured = null;
        fixture.CategoryProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoCategory>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        await CreateHandler(fixture).Handle(
            new SearchTodoCategoriesTabular.UseCase(
                new SearchTodoCategoriesTabular.Request(
                    new QueryParameters { Select = "name" },
                    IncludeDeleted: true)),
            CancellationToken.None);

        // Assert
        IEnumerable<FilterCondition> conditions = Flatten(captured!.Filter!).OfType<FilterCondition>();
        conditions.Should().Contain(condition => condition.Field == "tenantId");
        conditions.Should().NotContain(condition => condition.Field == "isDeleted");
    }

    [Fact(DisplayName = "Tabular search should reject an entity-shaped query")]
    public async Task Handle_WithoutProjection_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();

        // Act
        Result<SearchTodoCategoriesTabular.Response> result = await CreateHandler(fixture).Handle(
            new SearchTodoCategoriesTabular.UseCase(
                new SearchTodoCategoriesTabular.Request(new QueryParameters())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Todos.Query.EntityUnsupported");
        fixture.CategoryProjectionRepository.Verify(
            repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Tabular search should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result<SearchTodoCategoriesTabular.Response> result = await CreateHandler(fixture).Handle(
            new SearchTodoCategoriesTabular.UseCase(
                new SearchTodoCategoriesTabular.Request(new QueryParameters { Select = "name" })),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Tenantable.TenantId.Required");
    }
}
