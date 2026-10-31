using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Pagination;
using Application.Modules.Todos.Categories.Mappers;
using Application.Modules.Todos.Categories.Queries;
using Application.Unit.Tests.Modules.Todos.Fixtures;

using Core.Modules.Todos.Categories;

namespace Application.Unit.Tests.Modules.Todos.Categories.Queries;

/// <summary>Unit tests for the search categories slice.</summary>
public sealed class SearchTodoCategoriesTest
{
    private static SearchTodoCategories.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.CategoryMapperRepository.Object, fixture.Registry, fixture.Tenant);

    private static IEnumerable<FilterNode> Flatten(FilterNode node) =>
        node is FilterGroup group
            ? group.Children.SelectMany(Flatten)
            : [node];

    [Fact(DisplayName = "Search should map matching categories into a page")]
    public async Task Handle_WithResults_ShouldMapPage()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        TodoCategory category = fixture.CreateCategory();
        fixture.CategoryMapperRepository
            .Setup(repository => repository.MapAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<Func<TodoCategory, SearchTodoCategories.Response>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<SearchTodoCategories.Response>>.Success(
                new QueryPage<SearchTodoCategories.Response>(
                    [category.ToModel<SearchTodoCategories.Response>()],
                    1,
                    10,
                    1)));

        // Act
        Result<QueryPage<SearchTodoCategories.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoCategories.UseCase(
                new SearchTodoCategories.Request(new QueryParameters { Page = 1, PageSize = 10 })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(model => model.Id == category.Id);
    }

    [Fact(DisplayName = "Search should pass the composed filter, order, and page to the repository")]
    public async Task Handle_WithFilterOrderAndPaging_ShouldPassComposedQuery()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        Query<TodoCategory>? captured = null;
        fixture.CategoryMapperRepository
            .Setup(repository => repository.MapAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<Func<TodoCategory, SearchTodoCategories.Response>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoCategory>, Func<TodoCategory, SearchTodoCategories.Response>, CancellationToken>(
                (query, _, _) => captured = query)
            .ReturnsAsync(Result<QueryPage<SearchTodoCategories.Response>>.Success(
                new QueryPage<SearchTodoCategories.Response>([], 1, 10, 0)));

        // Act
        Result<QueryPage<SearchTodoCategories.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoCategories.UseCase(
                new SearchTodoCategories.Request(new QueryParameters
                {
                    Filter = "name=Errands",
                    OrderBy = "id",
                    Page = 1,
                    PageSize = 10,
                })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Filter.Should().NotBeNull();

        IEnumerable<FilterCondition> conditions = Flatten(captured.Filter!).OfType<FilterCondition>();
        conditions.Should().Contain(condition =>
            condition.Field == "tenantId" && Equals(condition.Value, fixture.Tenant.TenantId));
        conditions.Should().Contain(condition =>
            condition.Field == "isDeleted" && Equals(condition.Value, false));

        captured.OrderBy.Should().ContainSingle().Which.Field.Should().Be("id");
        captured.Page.Should().Be(new PageRequest(1, 10));
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
        fixture.CategoryMapperRepository.Verify(
            repository => repository.MapAsync(
                It.IsAny<Query<TodoCategory>>(),
                It.IsAny<Func<TodoCategory, SearchTodoCategories.Response>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Search should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result<QueryPage<SearchTodoCategories.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoCategories.UseCase(
                new SearchTodoCategories.Request(new QueryParameters())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Tenantable.TenantId.Required");
    }
}
