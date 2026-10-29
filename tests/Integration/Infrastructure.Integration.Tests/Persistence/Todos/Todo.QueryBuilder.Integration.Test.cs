using System.Globalization;

using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Projection;
using Application.Features.Todos.Common;
using Application.Features.Todos.Items.Commands;

using Core.Common.Monad.Results;
using Core.Todos.Items;

namespace Infrastructure.Integration.Tests.Persistence.Todos;

/// <summary>
/// Proves that builder-composed Todo queries execute through EF Core with the same
/// semantics the handlers rely on: scoping, filters, ordering, paging, projection,
/// grouping, and aggregation.
/// </summary>
[Collection("Todo EfCore")]
public sealed class TodoQueryBuilderIntegrationTest : IAsyncLifetime
{
    private static readonly Guid TenantId = new("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private const string Actor = "integration-query-builder";

    private readonly TodoEfCoreFixture _fixture;

    /// <summary>Creates the test with the shared PostgreSQL fixture.</summary>
    /// <param name="fixture">The shared fixture.</param>
    public TodoQueryBuilderIntegrationTest(TodoEfCoreFixture fixture) => _fixture = fixture;

    private IPersistentRepository<Todo, Guid> Repository => _fixture.TodoRepository;
    private IMappableRepository<Todo, Guid> MapperRepository => _fixture.TodoMapperRepository;
    private IProjectionRepository<Todo, Guid> ProjectionRepository => _fixture.TodoProjectionRepository;
    private IUnitOfWork UnitOfWork => _fixture.UnitOfWork;

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

    [Fact(DisplayName = "Builder filter, ordering, and paging should execute through QueryAsync")]
    public async Task QueryAsync_WithFluentClauses_ShouldReturnScopedPage()
    {
        // Arrange
        await SeedAsync();

        Query<Todo> query = QueryBuilder<Todo>.FromParameters(
                _fixture.QueryMapProvider,
                new QueryParameters())
            .ApplyTodoScope(TenantId, includeDeleted: false)
            .Where(todo => todo.Status == TodoStatus.Pending)
            .OrderBy(todo => todo.Title)
            .Page(1, 10)
            .Build().Value;

        // Act
        Result<QueryPage<Todo>> result = await Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(todo => todo.Title).Should().Equal("Alpha", "Bravo");
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact(DisplayName = "Builder query should project through ProjectAsync")]
    public async Task ProjectAsync_WithBuilderQuery_ShouldReturnProjectedTodos()
    {
        // Arrange
        await SeedAsync();

        Query<Todo> query = QueryBuilder<Todo>.FromParameters(
                _fixture.QueryMapProvider,
                new QueryParameters { OrderBy = "title" })
            .ApplyTodoScope(TenantId, includeDeleted: false)
            .Build().Value;

        // Act
        Result<QueryPage<ProjectedTodo>> result = await MapperRepository.ProjectAsync(
            query,
            todo => new ProjectedTodo(todo.Title, todo.Status));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items[0].Should().Be(new ProjectedTodo("Alpha", TodoStatus.Pending));
        result.Value.Items[1].Should().Be(new ProjectedTodo("Bravo", TodoStatus.Pending));
        result.Value.Items[2].Should().Be(new ProjectedTodo("Charlie", TodoStatus.InProgress));
    }

    [Fact(DisplayName = "Builder tabular query should group and aggregate through QueryTabularAsync")]
    public async Task QueryTabularAsync_WithBuilderQuery_ShouldReturnGroupedRows()
    {
        // Arrange
        await SeedAsync();

        Result<Query<Todo, TabularResult>> built = QueryBuilder<Todo>.FromParameters(
                _fixture.QueryMapProvider,
                new QueryParameters { GroupBy = "status", Aggregate = "count(id)" })
            .ApplyTodoScope(TenantId, includeDeleted: false)
            .BuildTabular();

        built.IsSuccess.Should().BeTrue();

        // Act
        Result<TabularResult> result = await ProjectionRepository.QueryTabularAsync(built.Value.Source);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Select(column => column.Name)
            .Should().BeEquivalentTo(["status", "count(id)"]);
        result.Value.Rows.Should().HaveCount(2);
        result.Value.Rows.Select(row => Convert.ToInt64(row.Values[1], CultureInfo.InvariantCulture))
            .Should().BeEquivalentTo([2L, 1L]);
    }

    [Fact(DisplayName = "Builder tabular select should project the title column through QueryTabularAsync")]
    public async Task QueryTabularAsync_WithSelect_ShouldReturnTitleColumn()
    {
        // Arrange
        await SeedAsync();

        Result<Query<Todo, TabularResult>> built = QueryBuilder<Todo>.FromParameters(
                _fixture.QueryMapProvider,
                new QueryParameters { Select = "title" })
            .ApplyTodoScope(TenantId, includeDeleted: false)
            .BuildTabular();

        built.IsSuccess.Should().BeTrue();

        // Act
        Result<TabularResult> result = await ProjectionRepository.QueryTabularAsync(built.Value.Source);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle().Which.Name.Should().Be("title");
        result.Value.Rows.Select(row => row.Values[0])
            .Should().BeEquivalentTo(["Alpha", "Bravo", "Charlie"]);
    }

    [Fact(DisplayName = "Builder scope should hide soft-deleted rows unless they are included")]
    public async Task QueryAsync_WithIncludeDeleted_ShouldHideThenIncludeSoftDeletedRow()
    {
        // Arrange
        await SeedAsync();
        Todo deleted = CreateTodo("Deleted", TodoStatus.Pending);
        await Repository.AddAsync(deleted);
        await UnitOfWork.SaveChangesAsync();

        var delete = new DeleteTodo.UseCaseHandler(
            Repository,
            UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result deleteResult = await delete.Handle(
            new DeleteTodo.UseCase(new DeleteTodo.Request(deleted.Id)),
            CancellationToken.None);
        deleteResult.IsSuccess.Should().BeTrue();

        Query<Todo> hidden = QueryBuilder<Todo>.FromParameters(
                _fixture.QueryMapProvider,
                new QueryParameters { OrderBy = "title" })
            .ApplyTodoScope(TenantId, includeDeleted: false)
            .Build().Value;

        Query<Todo> included = QueryBuilder<Todo>.FromParameters(
                _fixture.QueryMapProvider,
                new QueryParameters { OrderBy = "title" })
            .ApplyTodoScope(TenantId, includeDeleted: true)
            .Build().Value;

        // Act
        Result<QueryPage<Todo>> hiddenResult = await Repository.QueryAsync(hidden);
        Result<QueryPage<Todo>> includedResult = await Repository.QueryAsync(included);

        // Assert
        hiddenResult.IsSuccess.Should().BeTrue();
        hiddenResult.Value.Items.Select(todo => todo.Title)
            .Should().Equal("Alpha", "Bravo", "Charlie");

        includedResult.IsSuccess.Should().BeTrue();
        includedResult.Value.Items.Select(todo => todo.Title)
            .Should().Equal("Alpha", "Bravo", "Charlie", "Deleted");
        includedResult.Value.Items.Should().Contain(todo => todo.Id == deleted.Id);
    }

    [Fact(DisplayName = "Parameter-composed and fluent queries should return the same rows")]
    public async Task QueryAsync_ParameterAndFluentQueries_ShouldBeEquivalent()
    {
        // Arrange
        await SeedAsync();

        Query<Todo> fromParameters = QueryBuilder<Todo>.FromParameters(
                _fixture.QueryMapProvider,
                new QueryParameters { OrderBy = "title", Page = 1, PageSize = 10 })
            .ApplyTodoScope(TenantId, includeDeleted: false)
            .Build().Value;

        Query<Todo> fluent = QueryBuilder<Todo>.Create(_fixture.TodoQueryMap)
            .ApplyTodoScope(TenantId, includeDeleted: false)
            .OrderBy(todo => todo.Title)
            .Page(1, 10)
            .Build().Value;

        // Act
        Result<QueryPage<Todo>> parameterResult = await Repository.QueryAsync(fromParameters);
        Result<QueryPage<Todo>> fluentResult = await Repository.QueryAsync(fluent);

        // Assert
        parameterResult.IsSuccess.Should().BeTrue();
        fluentResult.IsSuccess.Should().BeTrue();
        fluentResult.Value.Items.Select(todo => todo.Id)
            .Should().Equal(parameterResult.Value.Items.Select(todo => todo.Id));
    }

    private async Task SeedAsync()
    {
        await Repository.AddRangeAsync(
        [
            CreateTodo("Alpha", TodoStatus.Pending),
            CreateTodo("Bravo", TodoStatus.Pending),
            CreateTodo("Charlie", TodoStatus.InProgress),
        ]);
        await Repository.AddAsync(Todo.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Foreign",
            _fixture.Clock.UtcNow,
            Actor).Value);
        await UnitOfWork.SaveChangesAsync();
    }

    private Todo CreateTodo(string title, TodoStatus status)
    {
        Todo todo = Todo.Create(
            Guid.NewGuid(),
            TenantId,
            title,
            _fixture.Clock.UtcNow,
            Actor).Value;
        todo.Status = status;
        return todo;
    }
}

/// <summary>The response shape used to prove builder projection through EF Core.</summary>
/// <param name="Title">The todo title.</param>
/// <param name="Status">The todo status.</param>
public sealed record ProjectedTodo(string Title, TodoStatus Status);
