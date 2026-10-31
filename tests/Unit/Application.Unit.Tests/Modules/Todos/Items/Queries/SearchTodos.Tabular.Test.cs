using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Projection;
using Application.BuildingBlocks.Querying.Sorting;
using Application.Modules.Todos.Items.Models;
using Application.Modules.Todos.Items.Queries;
using Application.Unit.Tests.Modules.Todos.Fixtures;

using Core.Modules.Todos.Items;

namespace Application.Unit.Tests.Modules.Todos.Items.Queries;

/// <summary>Unit tests for the tabular search todos slice.</summary>
public sealed class SearchTodosTabularTest
{
    private static SearchTodosTabular.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.TodoProjectionRepository.Object, fixture.Registry, fixture.Tenant);

    private static IEnumerable<FilterNode> Flatten(FilterNode node) =>
        node is FilterGroup group
            ? group.Children.SelectMany(Flatten)
            : [node];

    [Fact(DisplayName = "Tabular search should return the projected columns and rows")]
    public async Task Handle_WithSelect_ShouldReturnTabularResult()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
        var expected = new TabularResult(
            [new TabularColumn("title", typeof(string))],
            [new TabularRow(["First"])]);
        fixture.TodoProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TabularResult>.Success(expected));

        // Act
        Result<SearchTodosTabular.Response> result = await CreateHandler(fixture).Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters { Select = "title" })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle(column => column.Name == "title");
        result.Value.Rows.Should().ContainSingle();
        result.Value.Rows[0].Values[0].Should().Be("First");
    }

    [Fact(DisplayName = "Tabular search should accept a grouped aggregate")]
    public async Task Handle_WithGroupAndAggregate_ShouldReturnTabularResult()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
        Query<TodoReadModel>? captured = null;
        fixture.TodoProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoReadModel>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        Result<SearchTodosTabular.Response> result = await CreateHandler(fixture).Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters
                {
                    GroupBy = "title",
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
        fixture.RegisterTodoReadModelMap();
        Query<TodoReadModel>? captured = null;
        fixture.TodoProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoReadModel>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        Result<SearchTodosTabular.Response> result = await CreateHandler(fixture).Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters
                {
                    Select = "title",
                    GroupBy = "title",
                    Aggregate = "count(id)",
                })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        captured!.Select.Should().ContainSingle().Which.Field.Should().Be("title");
        captured.GroupBy.Should().ContainSingle().Which.Field.Should().Be("title");
        captured.Aggregate.Should().ContainSingle().Which.Field.Should().Be("id");
    }

    [Fact(DisplayName = "Tabular search should order by creation instant descending by default")]
    public async Task Handle_WithoutOrder_ShouldDefaultToCreatedAtDescending()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
        Query<TodoReadModel>? captured = null;
        fixture.TodoProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoReadModel>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        await CreateHandler(fixture).Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters { Select = "title" })),
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
        fixture.RegisterTodoReadModelMap();
        Query<TodoReadModel>? captured = null;
        fixture.TodoProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoReadModel>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        await CreateHandler(fixture).Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters
                {
                    Select = "title",
                    OrderBy = "title",
                })),
            CancellationToken.None);

        // Assert
        SortField order = captured!.OrderBy.Should().ContainSingle().Subject;
        order.Field.Should().Be("title");
        order.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact(DisplayName = "Tabular search should scope the query to the tenant and hide deleted rows")]
    public async Task Handle_ShouldApplyTenantScope()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
        Query<TodoReadModel>? captured = null;
        fixture.TodoProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoReadModel>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        await CreateHandler(fixture).Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters { Select = "title" })),
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
        fixture.RegisterTodoReadModelMap();
        Query<TodoReadModel>? captured = null;
        fixture.TodoProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoReadModel>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(new TabularResult([], [])));

        // Act
        await CreateHandler(fixture).Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(
                    new QueryParameters { Select = "title" },
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
        fixture.RegisterTodoReadModelMap();

        // Act
        Result<SearchTodosTabular.Response> result = await CreateHandler(fixture).Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Todos.Query.EntityUnsupported");
        fixture.TodoProjectionRepository.Verify(
            repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Tabular search should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result<SearchTodosTabular.Response> result = await CreateHandler(fixture).Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters { Select = "title" })),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Tenantable.TenantId.Required");
    }
}
