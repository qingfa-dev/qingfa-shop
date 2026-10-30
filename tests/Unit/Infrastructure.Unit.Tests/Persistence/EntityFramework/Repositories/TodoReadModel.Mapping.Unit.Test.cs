using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Searching;
using Application.Common.Querying.Sorting;
using Application.Features.Todos.Common;
using Application.Features.Todos.Items.Models;

using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.Capabilities;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.Persistence.EntityFramework.Repositories;
using Infrastructure.Persistence.Schemas.Todos.EntityConfiguration;
using Infrastructure.Persistence.Schemas.Todos.Queries;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Repositories;

/// <summary>
/// SQLite-backed query-map tests for the todo read model. They prove the query map
/// translates filters, sorts, searches, and projections over real EF Core query trees.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Mapping.TodoReadModel")]
public sealed class TodoReadModelMappingUnitTest : IAsyncDisposable
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly TodoReadModelSqliteContext _context;

    public TodoReadModelMappingUnitTest()
    {
        var options = new DbContextOptionsBuilder<TodoReadModelSqliteContext>()
            .UseSqlite("Filename=:memory:")
            .Options;
        _context = new TodoReadModelSqliteContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        _context.Database.CloseConnection();
        _context.Database.EnsureDeleted();
        await _context.DisposeAsync();
    }

    [Fact(DisplayName = "MapAsync should map the todo read model to a simple DTO")]
    public async Task MapAsync_ReadModel_SimpleDto_ShouldMapCorrectly()
    {
        // Arrange
        await SeedAsync(5);
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>();

        // Act
        Result<QueryPage<TodoReadModelSimpleDto>> result = await repository.MapAsync(
            query,
            todo => new TodoReadModelSimpleDto(todo.Title, todo.Status, todo.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto =>
        {
            dto.Title.Should().StartWith("Todo");
            dto.Status.Should().BeOneOf(
                TodoStatus.Pending,
                TodoStatus.Completed,
                TodoStatus.InProgress,
                TodoStatus.Cancelled);
        });
    }

    [Fact(DisplayName = "MapAsync should filter the read model by status through the query map")]
    public async Task MapAsync_ReadModel_FilterByStatus_ShouldReturnMatching()
    {
        // Arrange
        await SeedAsync(10);
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>
        {
            Filter = new FilterCondition(
                "status", FilterOperator.Equal,
                "0", (int)TodoStatus.Pending, typeof(int), false, null, null)
        };

        // Act
        Result<QueryPage<TodoReadModelSimpleDto>> result = await repository.MapAsync(
            query,
            todo => new TodoReadModelSimpleDto(todo.Title, todo.Status, todo.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.Status == TodoStatus.Pending);
    }

    [Fact(DisplayName = "MapAsync should sort the read model by title through the query map")]
    public async Task MapAsync_ReadModel_SortByTitle_ShouldReturnOrdered()
    {
        // Arrange
        await SeedAsync(5);
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>
        {
            OrderBy = new List<SortField>
            {
                new("title", SortDirection.Ascending, SortNulls.None)
            }
        };

        // Act
        Result<QueryPage<TodoReadModelSimpleDto>> result = await repository.MapAsync(
            query,
            todo => new TodoReadModelSimpleDto(todo.Title, todo.Status, todo.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(dto => dto.Title).Should().BeInAscendingOrder();
    }

    [Fact(DisplayName = "MapAsync should search the read model title through the query map")]
    public async Task MapAsync_ReadModel_SearchTitle_ShouldFilterCorrectly()
    {
        // Arrange
        await SeedAsync(10);
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>
        {
            Search = new BasicSearchRequest("Todo 1", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<TodoReadModelSimpleDto>> result = await repository.MapAsync(
            query,
            todo => new TodoReadModelSimpleDto(todo.Title, todo.Status, todo.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().Contain(dto => dto.Title == "Todo 1");
        result.Value.Items.Should().OnlyContain(dto => dto.Title.Contains("Todo 1"));
    }

    [Fact(DisplayName = "ProjectAsync should page the read model through the query map")]
    public async Task ProjectAsync_ReadModel_WithPagination_ShouldPageCorrectly()
    {
        // Arrange
        await SeedAsync(10);
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>
        {
            Page = new PageRequest(2, 3)
        };

        // Act
        Result<QueryPage<TodoReadModelSimpleDto>> result = await repository.ProjectAsync(
            query,
            todo => new TodoReadModelSimpleDto(todo.Title, todo.Status, todo.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "ProjectAsync should scope the read model by tenant and deletion through the query map")]
    public async Task ProjectAsync_ReadModel_WithConcernScope_ShouldReturnOwnedLiveRows()
    {
        // Arrange
        await SeedAsync(3);
        _context.Set<TodoReadModel>().AddRange(
            CreateReadModel("Foreign", Guid.NewGuid(), TodoStatus.Pending),
            CreateReadModel("Deleted", TenantId, TodoStatus.Pending, isDeleted: true));
        await _context.SaveChangesAsync();

        var repository = CreateRepository();
        Query<TodoReadModel> query = QueryBuilder<TodoReadModel>
            .Create(TodoReadModelQueryMap.Create())
            .ForTenant(TenantId)
            .IncludeDeleted(false)
            .Build().Value;

        // Act
        Result<QueryPage<TodoReadModelSimpleDto>> result = await repository.MapAsync(
            query,
            todo => new TodoReadModelSimpleDto(todo.Title, todo.Status, todo.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().NotContain(dto => dto.Title == "Foreign");
        result.Value.Items.Should().NotContain(dto => dto.Title == "Deleted");
    }

    [Fact(DisplayName = "MapAsync should apply an AND filter over the read model")]
    public async Task MapAsync_ReadModel_AndFilter_ShouldReturnMatching()
    {
        // Arrange
        await SeedRowsAsync(
            CreateReadModel("Pending high", TenantId, TodoStatus.Pending, priority: TodoPriority.High),
            CreateReadModel("Pending low", TenantId, TodoStatus.Pending, priority: TodoPriority.Low),
            CreateReadModel("Completed high", TenantId, TodoStatus.Completed, priority: TodoPriority.High));
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>
        {
            Filter = new FilterGroup(
                FilterLogic.And,
                new FilterNode[]
                {
                    new FilterCondition(
                        "status", FilterOperator.Equal,
                        "0", (int)TodoStatus.Pending, typeof(int), false, null, null),
                    new FilterCondition(
                        "priority", FilterOperator.Equal,
                        "2", (int)TodoPriority.High, typeof(int), false, null, null),
                })
        };

        // Act
        Result<QueryPage<TodoReadModelSimpleDto>> result = await repository.MapAsync(
            query,
            todo => new TodoReadModelSimpleDto(todo.Title, todo.Status, todo.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Pending high");
    }

    [Fact(DisplayName = "MapAsync should apply an OR filter over the read model")]
    public async Task MapAsync_ReadModel_OrFilter_ShouldReturnMatching()
    {
        // Arrange
        await SeedRowsAsync(
            CreateReadModel("Pending", TenantId, TodoStatus.Pending),
            CreateReadModel("Completed", TenantId, TodoStatus.Completed),
            CreateReadModel("Cancelled", TenantId, TodoStatus.Cancelled));
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>
        {
            Filter = new FilterGroup(
                FilterLogic.Or,
                new FilterNode[]
                {
                    new FilterCondition(
                        "status", FilterOperator.Equal,
                        "0", (int)TodoStatus.Pending, typeof(int), false, null, null),
                    new FilterCondition(
                        "status", FilterOperator.Equal,
                        "2", (int)TodoStatus.Completed, typeof(int), false, null, null),
                })
        };

        // Act
        Result<QueryPage<TodoReadModelSimpleDto>> result = await repository.MapAsync(
            query,
            todo => new TodoReadModelSimpleDto(todo.Title, todo.Status, todo.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(dto => dto.Title)
            .Should().BeEquivalentTo(["Pending", "Completed"]);
    }

    [Fact(DisplayName = "ProjectAsync should preserve null optional read-model fields")]
    public async Task ProjectAsync_ReadModel_OptionalFields_ShouldHandleNulls()
    {
        // Arrange
        DateTimeOffset dueAtUtc = DateTimeOffset.UtcNow.AddDays(2);
        await SeedRowsAsync(
            CreateReadModel(
                "With optional",
                TenantId,
                TodoStatus.Pending,
                description: "Description",
                dueAtUtc: dueAtUtc),
            CreateReadModel("Without optional", TenantId, TodoStatus.Pending));
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>();

        // Act
        Result<QueryPage<TodoReadModelOptionalDto>> result = await repository.ProjectAsync(
            query,
            todo => new TodoReadModelOptionalDto(todo.Title, todo.Description, todo.DueAtUtc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Single(dto => dto.Title == "With optional")
            .Should().Be(new TodoReadModelOptionalDto("With optional", "Description", dueAtUtc));
        result.Value.Items.Single(dto => dto.Title == "Without optional")
            .Should().Be(new TodoReadModelOptionalDto("Without optional", null, null));
    }

    [Fact(DisplayName = "MapAsync should project the computed IsCompleted flag")]
    public async Task MapAsync_ReadModel_ComputedIsCompleted_ShouldReturnFlag()
    {
        // Arrange
        await SeedRowsAsync(
            CreateReadModel("Done", TenantId, TodoStatus.Completed),
            CreateReadModel("Open", TenantId, TodoStatus.Pending));
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>();

        // Act
        Result<QueryPage<TodoReadModelComputedDto>> result = await repository.MapAsync(
            query,
            todo => new TodoReadModelComputedDto(todo.Title, todo.IsCompleted));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single(dto => dto.Title == "Done").IsCompleted.Should().BeTrue();
        result.Value.Items.Single(dto => dto.Title == "Open").IsCompleted.Should().BeFalse();
    }

    [Fact(DisplayName = "ProjectAsync should translate a computed completion projection")]
    public async Task ProjectAsync_ReadModel_ComputedIsCompleted_ShouldTranslate()
    {
        // Arrange
        await SeedRowsAsync(
            CreateReadModel("Done", TenantId, TodoStatus.Completed),
            CreateReadModel("Open", TenantId, TodoStatus.Pending));
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>();

        // Act
        Result<QueryPage<TodoReadModelComputedDto>> result = await repository.ProjectAsync(
            query,
            todo => new TodoReadModelComputedDto(
                todo.Title,
                todo.Status == TodoStatus.Completed));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single(dto => dto.Title == "Done").IsCompleted.Should().BeTrue();
        result.Value.Items.Single(dto => dto.Title == "Open").IsCompleted.Should().BeFalse();
    }

    [Fact(DisplayName = "MapAsync should return a result when the token is already cancelled")]
    public async Task MapAsync_ReadModel_WithCancelledToken_ShouldReturnResult()
    {
        // Arrange
        await SeedAsync(3);
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        Result<QueryPage<TodoReadModelSimpleDto>> result = await repository.MapAsync(
            query,
            todo => new TodoReadModelSimpleDto(todo.Title, todo.Status, todo.Priority),
            cancellation.Token);

        // Assert: SQLite may complete before observing cancellation; the contract is a Result.
        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "MapAsync should page a larger read-model dataset")]
    public async Task MapAsync_ReadModel_LargeDataset_ShouldMapAll()
    {
        // Arrange
        await SeedAsync(50);
        var repository = CreateRepository();
        var query = new Query<TodoReadModel>
        {
            Page = new PageRequest(1, 50)
        };

        // Act
        Result<QueryPage<TodoReadModelSimpleDto>> result = await repository.MapAsync(
            query,
            todo => new TodoReadModelSimpleDto(todo.Title, todo.Status, todo.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(50);
        result.Value.TotalCount.Should().Be(50);
    }

    private EfCoreRepository<TodoReadModel, Guid> CreateRepository()
    {
        var registry = new QueryMapRegistry();
        registry.Register(TodoReadModelQueryMap.Create());
        return new EfCoreRepository<TodoReadModel, Guid>(
            _context,
            new QueryTranslationEfCore<TodoReadModel>(),
            registry,
            new EfCoreQueryCapabilities());
    }

    private async Task SeedAsync(int count)
    {
        var rows = new List<TodoReadModel>();
        for (int index = 1; index <= count; index++)
        {
            rows.Add(CreateReadModel(
                $"Todo {index}",
                TenantId,
                (index % 3) switch
                {
                    0 => TodoStatus.Completed,
                    1 => TodoStatus.Pending,
                    _ => TodoStatus.InProgress,
                }));
        }

        _context.Set<TodoReadModel>().AddRange(rows);
        await _context.SaveChangesAsync();
    }

    private async Task SeedRowsAsync(params TodoReadModel[] rows)
    {
        _context.Set<TodoReadModel>().AddRange(rows);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private static TodoReadModel CreateReadModel(
        string title,
        Guid tenantId,
        TodoStatus status,
        bool isDeleted = false,
        TodoPriority priority = TodoPriority.Medium,
        string? description = null,
        DateTimeOffset? dueAtUtc = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Description = description,
            Status = status,
            Priority = priority,
            DueAtUtc = dueAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            IsDeleted = isDeleted,
        };

    /// <summary>SQLite context that maps the production todo read-model configuration.</summary>
    private sealed class TodoReadModelSqliteContext(DbContextOptions<TodoReadModelSqliteContext> options)
        : Microsoft.EntityFrameworkCore.DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TodoReadModelConfiguration());
            modelBuilder.Ignore<Core.Todos.Lists.TodoList>();
            modelBuilder.Ignore<Core.Todos.Categories.TodoCategory>();
        }
    }
}

/// <summary>The response shape used to prove read-model query-map projection.</summary>
/// <param name="Title">The todo title.</param>
/// <param name="Status">The todo status.</param>
/// <param name="Priority">The todo priority.</param>
public sealed record TodoReadModelSimpleDto(
    string Title,
    TodoStatus Status,
    TodoPriority Priority);

/// <summary>The optional-field shape used to prove null preservation.</summary>
/// <param name="Title">The todo title.</param>
/// <param name="Description">The optional description.</param>
/// <param name="DueAtUtc">The optional due instant.</param>
public sealed record TodoReadModelOptionalDto(
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc);

/// <summary>The computed shape used to prove completion projection.</summary>
/// <param name="Title">The todo title.</param>
/// <param name="IsCompleted">Whether the todo is completed.</param>
public sealed record TodoReadModelComputedDto(
    string Title,
    bool IsCompleted);
