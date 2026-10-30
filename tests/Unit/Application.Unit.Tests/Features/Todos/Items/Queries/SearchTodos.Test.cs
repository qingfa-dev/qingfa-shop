using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Items.Mappings;
using Application.Features.Todos.Items.Models;
using Application.Features.Todos.Items.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Queries;

/// <summary>Unit tests for the search todos slice.</summary>
public sealed class SearchTodosTest
{
    private static SearchTodos.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.TodoMapperRepository.Object, fixture.Registry, fixture.Tenant);

    private static IEnumerable<FilterNode> Flatten(FilterNode node) =>
        node is FilterGroup group
            ? group.Children.SelectMany(Flatten)
            : [node];

    private static TodoReadModel CreateReadModel(string title) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = TodoHandlerFixture.TenantId,
            Title = title,
            Status = TodoStatus.Pending,
            Priority = TodoPriority.Medium,
            CreatedAtUtc = TodoHandlerFixture.Now,
        };

    [Fact(DisplayName = "Search should map matching todos into a page")]
    public async Task Handle_WithResults_ShouldMapPage()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
        TodoReadModel first = CreateReadModel("First");
        TodoReadModel second = CreateReadModel("Second");
        fixture.TodoMapperRepository
            .Setup(repository => repository.MapAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<Func<TodoReadModel, SearchTodos.Response>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<SearchTodos.Response>>.Success(
                new QueryPage<SearchTodos.Response>(
                    [
                        first.ToSummary<SearchTodos.Response>(),
                        second.ToSummary<SearchTodos.Response>(),
                    ],
                    1,
                    10,
                    2)));

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

    [Fact(DisplayName = "Search should pass the composed filter, order, and page to the repository")]
    public async Task Handle_WithFilterOrderAndPaging_ShouldPassComposedQuery()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
        Query<TodoReadModel>? captured = null;
        fixture.TodoMapperRepository
            .Setup(repository => repository.MapAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<Func<TodoReadModel, SearchTodos.Response>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Query<TodoReadModel>, Func<TodoReadModel, SearchTodos.Response>, CancellationToken>(
                (query, _, _) => captured = query)
            .ReturnsAsync(Result<QueryPage<SearchTodos.Response>>.Success(
                new QueryPage<SearchTodos.Response>([], 1, 10, 0)));

        // Act
        Result<QueryPage<SearchTodos.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodos.UseCase(
                new SearchTodos.Request(new QueryParameters
                {
                    Filter = "title=First",
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
        fixture.RegisterTodoReadModelMap();

        // Act
        Result<QueryPage<SearchTodos.Response>> result = await CreateHandler(fixture).Handle(
            new SearchTodos.UseCase(
                new SearchTodos.Request(new QueryParameters { Select = "title" })),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Todos.Query.TabularUnsupported");
        fixture.TodoMapperRepository.Verify(
            repository => repository.MapAsync(
                It.IsAny<Query<TodoReadModel>>(),
                It.IsAny<Func<TodoReadModel, SearchTodos.Response>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Search should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterTodoReadModelMap();
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
