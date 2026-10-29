using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Categories.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Categories;

namespace Application.Unit.Tests.Features.Todos.Categories.Queries;

/// <summary>Unit tests for the search categories slice.</summary>
public sealed class SearchTodoCategoriesTest
{
    private static SearchTodoCategories.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.CategoryRepository.Object, fixture.Registry, fixture.Tenant);

    [Fact(DisplayName = "Search should map matching categories into a page")]
    public async Task Handle_WithResults_ShouldMapPage()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        TodoCategory category = fixture.CreateCategory();
        fixture.CategoryRepository
            .Setup(repository => repository.QueryAsync(
                It.IsAny<Query<TodoCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<TodoCategory>>.Success(
                new QueryPage<TodoCategory>([category], 1, 10, 1)));

        // Act
        Result<QueryPage<SearchTodoCategories.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoCategories.UseCase(
                new SearchTodoCategories.Request(new QueryParameters { Page = 1, PageSize = 10 })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(model => model.Id == category.Id);
    }

    [Fact(DisplayName = "Search should reject a tabular projection")]
    public async Task Handle_WithTabularQuery_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();

        // Act
        Result<QueryPage<SearchTodoCategories.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoCategories.UseCase(
                new SearchTodoCategories.Request(new QueryParameters { Select = "name" })),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Todos.Query.TabularUnsupported");
    }
}
