using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Projection;
using Application.Features.Todos.Items.Models;
using Application.Features.Todos.Items.Mappings;
using Application.Features.Todos.Items.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Queries;

/// <summary>Read-side tests proving todo queries use the projection.</summary>
[Trait("Category", "Unit")]
public sealed class EventSourcedTodoQueriesTest
{
    private static TodoReadModel CreateReadModel(
        Guid? tenantId = null,
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? TodoHandlerFixture.TenantId,
            Title = "Projected",
            Status = TodoStatus.Pending,
            Priority = TodoPriority.Medium,
            CreatedAtUtc = TodoHandlerFixture.Now,
            IsDeleted = isDeleted,
        };

    private static IEnumerable<FilterNode> Flatten(FilterNode node) =>
        node is FilterGroup group
            ? group.Children.SelectMany(Flatten)
            : [node];

    [Fact(DisplayName = "GetTodo should return the projected row")]
    public async Task GetTodo_ShouldReturnProjectedRow()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoReadModel model = CreateReadModel();
        fixture.TodoReadRepository
            .Setup(repository => repository.FindByIdAsync(model.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<TodoReadModel>>.Success(Maybe<TodoReadModel>.From(model)));
        var handler = new GetTodo.UseCaseHandler(fixture.TodoReadRepository.Object, fixture.Tenant);

        // Act
        Result<GetTodo.Response> result = await handler.Handle(
            new GetTodo.UseCase(new GetTodo.Request(model.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(model.Id);
        result.Value.Title.Should().Be("Projected");
    }

    [Fact(DisplayName = "GetTodo should hide another tenant's row")]
    public async Task GetTodo_WithForeignTenant_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoReadModel model = CreateReadModel(tenantId: Guid.NewGuid());
        fixture.TodoReadRepository
            .Setup(repository => repository.FindByIdAsync(model.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<TodoReadModel>>.Success(Maybe<TodoReadModel>.From(model)));
        var handler = new GetTodo.UseCaseHandler(fixture.TodoReadRepository.Object, fixture.Tenant);

        // Act
        Result<GetTodo.Response> result = await handler.Handle(
            new GetTodo.UseCase(new GetTodo.Request(model.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == RepositoryOutcome.Codes.EntityNotFound);
    }

    [Fact(DisplayName = "GetTodo should hide soft-deleted rows by default")]
    public async Task GetTodo_WhenDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoReadModel model = CreateReadModel(isDeleted: true);
        fixture.TodoReadRepository
            .Setup(repository => repository.FindByIdAsync(model.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<TodoReadModel>>.Success(Maybe<TodoReadModel>.From(model)));
        var handler = new GetTodo.UseCaseHandler(fixture.TodoReadRepository.Object, fixture.Tenant);

        // Act
        Result<GetTodo.Response> result = await handler.Handle(
            new GetTodo.UseCase(new GetTodo.Request(model.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "SearchTodos should scope by tenant and deletion and map the read model")]
    public async Task SearchTodos_ShouldScopeAndMapReadModel()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
        TodoReadModel model = CreateReadModel();
        Query<TodoReadModel>? captured = null;
        fixture.TodoMapperRepository
            .Setup(repository => repository.MapAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<Func<TodoReadModel, SearchTodos.Response>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoReadModel>, Func<TodoReadModel, SearchTodos.Response>, CancellationToken>(
                (query, _, _) => captured = query)
            .ReturnsAsync(Result<QueryPage<SearchTodos.Response>>.Success(
                new QueryPage<SearchTodos.Response>([model.ToSummary<SearchTodos.Response>()], 1, 10, 1)));
        var handler = new SearchTodos.UseCaseHandler(
            fixture.TodoMapperRepository.Object, fixture.Registry, fixture.Tenant);

        // Act
        Result<QueryPage<SearchTodos.Response>> result = await handler.Handle(
            new SearchTodos.UseCase(
                new SearchTodos.Request(new QueryParameters { Page = 1, PageSize = 10 })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle().Which.Title.Should().Be("Projected");
        captured.Should().NotBeNull();
        IEnumerable<FilterCondition> conditions = Flatten(captured!.Filter!).OfType<FilterCondition>();
        conditions.Should().Contain(condition =>
            condition.Field == "tenantId" && Equals(condition.Value, TodoHandlerFixture.TenantId));
        conditions.Should().Contain(condition =>
            condition.Field == "isDeleted" && Equals(condition.Value, false));
    }

    [Fact(DisplayName = "SearchTodosTabular should query the read model and return its columns")]
    public async Task SearchTodosTabular_ShouldQueryReadModel()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
        Query<TodoReadModel>? captured = null;
        var expected = new TabularResult(
            [new TabularColumn("title", typeof(string))],
            [new TabularRow(["Projected"])]);
        fixture.TodoProjectionRepository
            .Setup(repository => repository.QueryTabularAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoReadModel>, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(Result<TabularResult>.Success(expected));
        var handler = new SearchTodosTabular.UseCaseHandler(
            fixture.TodoProjectionRepository.Object, fixture.Registry, fixture.Tenant);

        // Act
        Result<SearchTodosTabular.Response> result = await handler.Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters { Select = "title" })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle(column => column.Name == "title");
        result.Value.Rows.Should().ContainSingle();
        result.Value.Rows[0].Values[0].Should().Be("Projected");
        captured.Should().NotBeNull();
    }
}
