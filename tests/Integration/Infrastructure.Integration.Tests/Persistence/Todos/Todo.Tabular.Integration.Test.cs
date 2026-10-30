using Application.Common.Querying;
using Application.Features.Todos.Categories.Commands;
using Application.Features.Todos.Categories.Queries;
using Application.Features.Todos.Items.Models;
using Application.Features.Todos.Items.Queries;
using Application.Features.Todos.Lists.Commands;
using Application.Features.Todos.Lists.Queries;

using Core.Common.Monad.Results;
using Core.Todos.Items;

namespace Infrastructure.Integration.Tests.Persistence.Todos;

/// <summary>
/// PostgreSQL-backed tests for the tabular Todos search slices.
/// </summary>
[Collection("Todo EfCore")]
public sealed class TodoTabularIntegrationTest : IAsyncLifetime
{
    private static readonly Guid TenantId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private const string Actor = "integration-tabular";

    private readonly TodoEfCoreFixture _fixture;

    /// <summary>Creates the test with the shared PostgreSQL fixture.</summary>
    /// <param name="fixture">The shared fixture.</param>
    public TodoTabularIntegrationTest(TodoEfCoreFixture fixture) => _fixture = fixture;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _fixture.CreateScope();
        _fixture.Ambient.TenantId = TenantId;
        _fixture.Ambient.UserName = Actor;
        await _fixture.CleanAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _fixture.CleanAsync();

    [Fact(DisplayName = "Tabular todo search should project the requested columns")]
    public async Task SearchTodosTabular_WithSelect_ShouldReturnRows()
    {
        // Arrange
        await SeedAsync(
            CreateTodo("First", TodoStatus.Pending),
            CreateTodo("Second", TodoStatus.Pending));
        var handler = new SearchTodosTabular.UseCaseHandler(
            _fixture.TodoProjectionRepository,
            _fixture.QueryMapProvider,
            _fixture.Ambient);

        // Act
        Result<SearchTodosTabular.Response> result = await handler.Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters { Select = "title" })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle(column => column.Name == "title");
        result.Value.Rows.Should().HaveCount(2);
        result.Value.Rows.Select(row => row.Values[0]).Should().BeEquivalentTo(["First", "Second"]);
    }

    [Fact(DisplayName = "Tabular todo search should group and aggregate")]
    public async Task SearchTodosTabular_WithGroupAndAggregate_ShouldReturnRows()
    {
        // Arrange
        await SeedAsync(
            CreateTodo("Pending", TodoStatus.Pending),
            CreateTodo("Another pending", TodoStatus.Pending),
            CreateTodo("Started", TodoStatus.InProgress));

        var handler = new SearchTodosTabular.UseCaseHandler(
            _fixture.TodoProjectionRepository,
            _fixture.QueryMapProvider,
            _fixture.Ambient);

        // Act
        Result<SearchTodosTabular.Response> result = await handler.Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters
                {
                    GroupBy = "status",
                    Aggregate = "count(id)",
                })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().Contain(column => column.Name == "status");
        result.Value.Columns.Should().Contain(column => column.Name == "count(id)");
        result.Value.Rows.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Tabular todo search should scope rows to the tenant and honor IncludeDeleted")]
    public async Task SearchTodosTabular_ShouldScopeTenantAndIncludeDeleted()
    {
        // Arrange
        await SeedAsync(
            CreateTodo("Own", TodoStatus.Pending),
            CreateTodo("Deleted", TodoStatus.Pending, isDeleted: true),
            CreateTodo("Foreign", TodoStatus.Pending, tenantId: Guid.NewGuid()));

        var handler = new SearchTodosTabular.UseCaseHandler(
            _fixture.TodoProjectionRepository,
            _fixture.QueryMapProvider,
            _fixture.Ambient);

        // Act
        Result<SearchTodosTabular.Response> hidden = await handler.Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(new QueryParameters { Select = "title" })),
            CancellationToken.None);

        Result<SearchTodosTabular.Response> included = await handler.Handle(
            new SearchTodosTabular.UseCase(
                new SearchTodosTabular.Request(
                    new QueryParameters { Select = "title" },
                    IncludeDeleted: true)),
            CancellationToken.None);

        // Assert
        hidden.IsSuccess.Should().BeTrue();
        hidden.Value.Rows.Select(row => row.Values[0]).Should().BeEquivalentTo(["Own"]);

        included.IsSuccess.Should().BeTrue();
        included.Value.Rows.Select(row => row.Values[0])
            .Should().BeEquivalentTo(["Deleted", "Own"]);
    }

    [Fact(DisplayName = "Tabular list search should project the requested columns")]
    public async Task SearchTodoListsTabular_WithSelect_ShouldReturnRows()
    {
        // Arrange
        var createList = new CreateTodoList.UseCaseHandler(
            _fixture.TodoListRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        (await createList.Handle(
                new CreateTodoList.UseCase(new CreateTodoList.Request { Name = "Groceries" }),
                CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        var handler = new SearchTodoListsTabular.UseCaseHandler(
            _fixture.TodoListProjectionRepository,
            _fixture.QueryMapProvider,
            _fixture.Ambient);

        // Act
        Result<SearchTodoListsTabular.Response> result = await handler.Handle(
            new SearchTodoListsTabular.UseCase(
                new SearchTodoListsTabular.Request(new QueryParameters { Select = "name" })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle(column => column.Name == "name");
        result.Value.Rows.Select(row => row.Values[0]).Should().BeEquivalentTo(["Groceries"]);
    }

    [Fact(DisplayName = "Tabular category search should project the requested columns")]
    public async Task SearchTodoCategoriesTabular_WithSelect_ShouldReturnRows()
    {
        // Arrange
        var createCategory = new CreateTodoCategory.UseCaseHandler(
            _fixture.TodoCategoryRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        (await createCategory.Handle(
                new CreateTodoCategory.UseCase(new CreateTodoCategory.Request { Name = "Errands" }),
                CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        var handler = new SearchTodoCategoriesTabular.UseCaseHandler(
            _fixture.TodoCategoryProjectionRepository,
            _fixture.QueryMapProvider,
            _fixture.Ambient);

        // Act
        Result<SearchTodoCategoriesTabular.Response> result = await handler.Handle(
            new SearchTodoCategoriesTabular.UseCase(
                new SearchTodoCategoriesTabular.Request(new QueryParameters { Select = "name" })),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle(column => column.Name == "name");
        result.Value.Rows.Select(row => row.Values[0]).Should().BeEquivalentTo(["Errands"]);
    }

    private async Task SeedAsync(params TodoReadModel[] todos)
    {
        _fixture.DbContext.Set<TodoReadModel>().AddRange(todos);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();
    }

    private static TodoReadModel CreateTodo(
        string title,
        TodoStatus status,
        Guid? tenantId = null,
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? TenantId,
            Title = title,
            Status = status,
            Priority = TodoPriority.Medium,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Actor,
            IsDeleted = isDeleted,
        };
}
