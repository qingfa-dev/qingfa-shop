using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Lists.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Lists;

namespace Application.Unit.Tests.Features.Todos.Lists.Queries;

/// <summary>Unit tests for the search lists slice.</summary>
public sealed class SearchTodoListsTest
{
    private static SearchTodoLists.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.ListRepository.Object, fixture.Registry, fixture.Tenant);

    [Fact(DisplayName = "Search should map matching lists into a page")]
    public async Task Handle_WithResults_ShouldMapPage()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterListMap();
        TodoList list = fixture.CreateList();
        fixture.ListRepository
            .Setup(repository => repository.QueryAsync(
                It.IsAny<Query<TodoList>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<TodoList>>.Success(
                new QueryPage<TodoList>([list], 1, 10, 1)));

        // Act
        Result<QueryPage<SearchTodoLists.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoLists.UseCase(
                new SearchTodoLists.Request(new QueryParameters { Page = 1, PageSize = 10 })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(model => model.Id == list.Id);
    }

    [Fact(DisplayName = "Search should reject a tabular projection")]
    public async Task Handle_WithTabularQuery_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterListMap();

        // Act
        Result<QueryPage<SearchTodoLists.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoLists.UseCase(
                new SearchTodoLists.Request(new QueryParameters { Select = "name" })),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Todos.Query.TabularUnsupported");
    }
}
