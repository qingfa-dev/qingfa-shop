using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Lists.Mappings;
using Application.Features.Todos.Lists.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Lists;

namespace Application.Unit.Tests.Features.Todos.Lists.Queries;

/// <summary>Unit tests for the search lists slice.</summary>
public sealed class SearchTodoListsTest
{
    private static SearchTodoLists.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.ListMapperRepository.Object, fixture.Registry, fixture.Tenant);

    private static IEnumerable<FilterNode> Flatten(FilterNode node) =>
        node is FilterGroup group
            ? group.Children.SelectMany(Flatten)
            : [node];

    [Fact(DisplayName = "Search should map matching lists into a page")]
    public async Task Handle_WithResults_ShouldMapPage()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterListMap();
        TodoList list = fixture.CreateList();
        fixture.ListMapperRepository
            .Setup(repository => repository.MapAsync(
                It.IsAny<Query<TodoList>>(),
                It.IsAny<Func<TodoList, SearchTodoLists.Response>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<SearchTodoLists.Response>>.Success(
                new QueryPage<SearchTodoLists.Response>(
                    [list.ToModel<SearchTodoLists.Response>()],
                    1,
                    10,
                    1)));

        // Act
        Result<QueryPage<SearchTodoLists.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoLists.UseCase(
                new SearchTodoLists.Request(new QueryParameters { Page = 1, PageSize = 10 })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(model => model.Id == list.Id);
    }

    [Fact(DisplayName = "Search should pass the composed filter, order, and page to the repository")]
    public async Task Handle_WithFilterOrderAndPaging_ShouldPassComposedQuery()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterListMap();
        Query<TodoList>? captured = null;
        fixture.ListMapperRepository
            .Setup(repository => repository.MapAsync(
                It.IsAny<Query<TodoList>>(),
                It.IsAny<Func<TodoList, SearchTodoLists.Response>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoList>, Func<TodoList, SearchTodoLists.Response>, CancellationToken>(
                (query, _, _) => captured = query)
            .ReturnsAsync(Result<QueryPage<SearchTodoLists.Response>>.Success(
                new QueryPage<SearchTodoLists.Response>([], 1, 10, 0)));

        // Act
        Result<QueryPage<SearchTodoLists.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoLists.UseCase(
                new SearchTodoLists.Request(new QueryParameters
                {
                    Filter = "name=Groceries",
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
        fixture.RegisterListMap();

        // Act
        Result<QueryPage<SearchTodoLists.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoLists.UseCase(
                new SearchTodoLists.Request(new QueryParameters { Select = "name" })),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Todos.Query.TabularUnsupported");
        fixture.ListMapperRepository.Verify(
            repository => repository.MapAsync(
                It.IsAny<Query<TodoList>>(),
                It.IsAny<Func<TodoList, SearchTodoLists.Response>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Search should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterListMap();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result<QueryPage<SearchTodoLists.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodoLists.UseCase(
                new SearchTodoLists.Request(new QueryParameters())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Tenantable.TenantId.Required");
    }
}
