using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Items.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Queries;

/// <summary>Unit tests for the search todos slice.</summary>
public sealed class SearchTodosTest
{
    private static SearchTodos.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.TodoRepository.Object, fixture.Registry, fixture.Tenant);

    [Fact(DisplayName = "Search should map matching todos into a page")]
    public async Task Handle_WithResults_ShouldMapPage()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoMap();
        Todo first = fixture.CreateTodo("First");
        Todo second = fixture.CreateTodo("Second");
        fixture.TodoRepository
            .Setup(repository => repository.QueryAsync(
                It.IsAny<Query<Todo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<Todo>>.Success(
                new QueryPage<Todo>([first, second], 1, 10, 2)));

        // Act
        Result<QueryPage<SearchTodos.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodos.UseCase(
                new SearchTodos.Request(new QueryParameters { Page = 1, PageSize = 10 })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Title.Should().Be("First");
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact(DisplayName = "Search should reject a tabular projection")]
    public async Task Handle_WithTabularQuery_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoMap();

        // Act
        Result<QueryPage<SearchTodos.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodos.UseCase(
                new SearchTodos.Request(new QueryParameters { Select = "title" })),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Todos.Query.TabularUnsupported");
        fixture.TodoRepository.Verify(
            repository => repository.QueryAsync(It.IsAny<Query<Todo>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Search should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoMap();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result<QueryPage<SearchTodos.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodos.UseCase(
                new SearchTodos.Request(new QueryParameters())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Tenantable.TenantId.Required");
    }
}
