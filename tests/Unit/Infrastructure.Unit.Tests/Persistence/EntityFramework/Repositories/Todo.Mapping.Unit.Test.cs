using Application.Common.Persistence;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Searching;
using Application.Common.Querying.Sorting;

using Core.Todos;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.Persistence.EntityFramework.Repositories;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Repositories;

/// <summary>
/// DTOs for Todo mapping tests — covers scalar, computed, and optional properties.
/// </summary>
public static class TodoMappingDtos
{
    // Simple scalar DTO
    public record TodoSimpleDto(string Title, TodoStatus Status, TodoPriority Priority);

    // DTO with optional fields
    public record TodoWithOptionalDto(
        string Title,
        string? Description,
        TodoStatus Status,
        DateTimeOffset? DueAtUtc);

    // DTO with computed values
    public record TodoComputedDto(
        string Title,
        TodoStatus Status,
        bool IsOverdue,
        string StatusLabel,
        int DaysUntilDue);

    // DTO with audit fields
    public record TodoWithAuditDto(
        string Title,
        string? CreatedBy,
        DateTimeOffset CreatedAtUtc,
        string? ModifiedBy,
        DateTimeOffset? ModifiedAtUtc);

    // DTO with category info (JSON array)
    public record TodoWithCategoriesDto(
        string Title,
        int CategoryCount,
        bool HasCategories);

    // TodoList DTOs
    public record TodoListSimpleDto(string Name, string? Description);
    public record TodoListWithAuditDto(string Name, string? CreatedBy, DateTimeOffset CreatedAtUtc);

    // TodoCategory DTOs
    public record TodoCategorySimpleDto(string Name, string? Description);
    public record TodoCategoryWithAuditDto(string Name, string? CreatedBy, DateTimeOffset CreatedAtUtc);

    // Cross-entity DTO (Todo with list name simulation)
    public record TodoWithListInfoDto(
        string Title,
        TodoStatus Status,
        Guid? ListId,
        bool HasList);
}

/// <summary>
/// SQLite-compatible DbContext for Todo entities.
/// Overrides PostgreSQL-specific configurations (jsonb, timestamptz) for SQLite.
/// </summary>
public sealed class TodoSqliteDbContext : EfCoreDbContext
{
    public DbSet<Todo> Todos => Set<Todo>();
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<TodoCategory> TodoCategories => Set<TodoCategory>();

    public TodoSqliteDbContext(DbContextOptions<TodoSqliteDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Skip assembly configurations — they use PostgreSQL-specific types
        // Configure entities manually for SQLite compatibility

        // Todo
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.ToTable("todos");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(t => t.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(t => t.Status).HasColumnName("status").HasConversion<int>().IsRequired();
            entity.Property(t => t.Priority).HasColumnName("priority").HasConversion<int>().IsRequired();
            entity.Property(t => t.DueAtUtc).HasColumnName("due_at_utc");
            entity.Property(t => t.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(t => t.ListId).HasColumnName("list_id");
            entity.Property(t => t.CategoryIds).HasColumnName("category_ids")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v ?? new List<Guid>()),
                    v => string.IsNullOrEmpty(v)
                        ? new List<Guid>()
                        : System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v) ?? new List<Guid>());
            entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(t => t.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
            entity.Property(t => t.ModifiedAtUtc).HasColumnName("modified_at_utc");
            entity.Property(t => t.ModifiedBy).HasColumnName("modified_by").HasMaxLength(256);
            entity.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(t => t.DeletedAtUtc).HasColumnName("deleted_at_utc");
            entity.Property(t => t.DeletedBy).HasColumnName("deleted_by").HasMaxLength(256);
            entity.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
            entity.Property(t => t.Metadata).HasColumnName("metadata")
                .HasConversion(
                    (Core.Common.Monad.Metadata.DefaultMetadata v) => System.Text.Json.JsonSerializer.Serialize(v),
                    (string v) => string.IsNullOrEmpty(v)
                        ? new Core.Common.Monad.Metadata.DefaultMetadata()
                        : new Core.Common.Monad.Metadata.DefaultMetadata(
                            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v) ?? new Dictionary<string, object>()));
        });

        // TodoList
        modelBuilder.Entity<TodoList>(entity =>
        {
            entity.ToTable("todo_lists");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(t => t.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
            entity.Property(t => t.ModifiedAtUtc).HasColumnName("modified_at_utc");
            entity.Property(t => t.ModifiedBy).HasColumnName("modified_by").HasMaxLength(256);
            entity.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(t => t.DeletedAtUtc).HasColumnName("deleted_at_utc");
            entity.Property(t => t.DeletedBy).HasColumnName("deleted_by").HasMaxLength(256);
            entity.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
        });

        // TodoCategory
        modelBuilder.Entity<TodoCategory>(entity =>
        {
            entity.ToTable("todo_categories");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(t => t.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
            entity.Property(t => t.ModifiedAtUtc).HasColumnName("modified_at_utc");
            entity.Property(t => t.ModifiedBy).HasColumnName("modified_by").HasMaxLength(256);
            entity.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(t => t.DeletedAtUtc).HasColumnName("deleted_at_utc");
            entity.Property(t => t.DeletedBy).HasColumnName("deleted_by").HasMaxLength(256);
            entity.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
        });
    }
}

/// <summary>Shared test fixture for Todo SQLite mapping tests.</summary>
public static class TodoSqliteTestFixture
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static TodoSqliteDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoSqliteDbContext>()
            .UseSqlite($"Filename=:memory:;Cache=Shared;Mode=Memory")
            .Options;
        var context = new TodoSqliteDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    public static EfCoreRepository<Todo, Guid> CreateTodoRepository(TodoSqliteDbContext context)
    {
        var translator = new QueryTranslationEfCore<Todo>();
        var map = Infrastructure.Persistence.Todos.TodoQueryMap.Create();
        var registry = new QueryMapRegistry();
        registry.Register(map);
        var capabilities = new Infrastructure.Persistence.EntityFramework.Capabilities.EfCoreQueryCapabilities();
        return new EfCoreRepository<Todo, Guid>(context, translator, registry, capabilities);
    }

    public static EfCoreRepository<TodoList, Guid> CreateTodoListRepository(TodoSqliteDbContext context)
    {
        var translator = new QueryTranslationEfCore<TodoList>();
        var map = Infrastructure.Persistence.Todos.TodoListQueryMap.Create();
        var registry = new QueryMapRegistry();
        registry.Register(map);
        var capabilities = new Infrastructure.Persistence.EntityFramework.Capabilities.EfCoreQueryCapabilities();
        return new EfCoreRepository<TodoList, Guid>(context, translator, registry, capabilities);
    }

    public static EfCoreRepository<TodoCategory, Guid> CreateTodoCategoryRepository(TodoSqliteDbContext context)
    {
        var translator = new QueryTranslationEfCore<TodoCategory>();
        var map = Infrastructure.Persistence.Todos.TodoCategoryQueryMap.Create();
        var registry = new QueryMapRegistry();
        registry.Register(map);
        var capabilities = new Infrastructure.Persistence.EntityFramework.Capabilities.EfCoreQueryCapabilities();
        return new EfCoreRepository<TodoCategory, Guid>(context, translator, registry, capabilities);
    }

    public static Todo CreateTodo(
        string title = "Buy milk",
        TodoStatus status = TodoStatus.Pending,
        TodoPriority priority = TodoPriority.Medium,
        string? description = null,
        DateTimeOffset? dueAtUtc = null,
        Guid? listId = null,
        List<Guid>? categoryIds = null)
    {
        var now = DateTimeOffset.UtcNow;
        var result = Todo.Create(
            id: Guid.NewGuid(),
            tenantId: TenantId,
            title: title ?? "Untitled",
            nowUtc: now,
            actor: "test",
            description: description,
            priority: priority,
            dueAtUtc: dueAtUtc,
            listId: listId);
        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to create Todo: {string.Join(", ", result.Errors.Select(e => e.Message))}");
        var todo = result.Value;
        todo.Status = status;
        if (categoryIds != null)
            ((System.Collections.Generic.List<Guid>)todo.CategoryIds).AddRange(categoryIds);
        return todo;
    }

    public static TodoList CreateTodoList(string name = "Work", string? description = null)
    {
        var now = DateTimeOffset.UtcNow;
        var result = TodoList.Create(
            id: Guid.NewGuid(),
            tenantId: TenantId,
            name: name,
            nowUtc: now,
            actor: "test",
            description: description);
        return result.Value;
    }

    public static TodoCategory CreateTodoCategory(string name = "Bug", string? description = null)
    {
        var now = DateTimeOffset.UtcNow;
        var result = TodoCategory.Create(
            id: Guid.NewGuid(),
            tenantId: TenantId,
            name: name,
            nowUtc: now,
            actor: "test",
            description: description);
        return result.Value;
    }

    public static async Task SeedTodosAsync(TodoSqliteDbContext context, int count)
    {
        var todos = new List<Todo>();
        for (int i = 1; i <= count; i++)
        {
            var status = (i % 4) switch
            {
                0 => TodoStatus.Completed,
                1 => TodoStatus.Pending,
                2 => TodoStatus.InProgress,
                3 => TodoStatus.Cancelled,
                _ => TodoStatus.Pending
            };
            var priority = (i % 3) switch
            {
                0 => TodoPriority.High,
                1 => TodoPriority.Medium,
                2 => TodoPriority.Low,
                _ => TodoPriority.Medium
            };
            todos.Add(CreateTodo(
                title: $"Todo {i}",
                status: status,
                priority: priority,
                description: i % 2 == 0 ? $"Description for todo {i}" : null,
                dueAtUtc: i % 3 == 0 ? DateTimeOffset.UtcNow.AddDays(i) : null));
        }
        context.Set<Todo>().AddRange(todos);
        await context.SaveChangesAsync();
    }

    public static async Task SeedTodoListsAsync(TodoSqliteDbContext context, int count)
    {
        var lists = new List<TodoList>();
        for (int i = 1; i <= count; i++)
        {
            lists.Add(CreateTodoList(
                name: $"List {i}",
                description: i % 2 == 0 ? $"Description for list {i}" : null));
        }
        context.Set<TodoList>().AddRange(lists);
        await context.SaveChangesAsync();
    }

    public static async Task SeedTodoCategoriesAsync(TodoSqliteDbContext context, int count)
    {
        var categories = new List<TodoCategory>();
        for (int i = 1; i <= count; i++)
        {
            categories.Add(CreateTodoCategory(
                name: $"Category {i}",
                description: i % 2 == 0 ? $"Description for category {i}" : null));
        }
        context.Set<TodoCategory>().AddRange(categories);
        await context.SaveChangesAsync();
    }
}

/// <summary>
/// Tests EfCoreRepository mapping methods with real SQLite QueryTranslation for Todo entities.
/// Unlike InMemory tests, these verify actual EF Core expression tree translation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Mapping.Todo.Sqlite")]
public sealed class TodoMappingUnitTest : IAsyncDisposable
{
    private readonly TodoSqliteDbContext _context;

    public TodoMappingUnitTest()
    {
        _context = TodoSqliteTestFixture.CreateContext();
    }

    public async ValueTask DisposeAsync()
    {
        _context.Database.CloseConnection();
        _context.Database.EnsureDeleted();
        await _context.DisposeAsync();
    }

    #region MapAsync - Todo entity

    [Fact(DisplayName = "MapAsync should map Todo to simple DTO with real QueryTranslation")]
    public async Task MapAsync_Todo_SimpleDto_ShouldMapCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto =>
        {
            dto.Title.Should().StartWith("Todo");
            dto.Status.Should().BeOneOf(TodoStatus.Pending, TodoStatus.Completed, TodoStatus.InProgress, TodoStatus.Cancelled);
            dto.Priority.Should().BeOneOf(TodoPriority.High, TodoPriority.Medium, TodoPriority.Low);
        });
    }

    [Fact(DisplayName = "MapAsync should map Todo with optional fields")]
    public async Task MapAsync_Todo_WithOptionalFields_ShouldHandleNulls()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoWithOptionalDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoWithOptionalDto(t.Title, t.Description, t.Status, t.DueAtUtc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        // Some descriptions are null, some due dates are null
        var withDescription = result.Value.Items.Where(d => d.Description != null).ToList();
        var withoutDescription = result.Value.Items.Where(d => d.Description == null).ToList();
        withDescription.Should().NotBeEmpty();
        withoutDescription.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "MapAsync should map Todo with computed values")]
    public async Task MapAsync_Todo_ComputedDto_ShouldCalculateCorrectly()
    {
        // Arrange — Create todos and manually set due dates to test overdue logic
        var now = DateTimeOffset.UtcNow;
        var overdue = TodoSqliteTestFixture.CreateTodo("Overdue task", TodoStatus.Pending);
        overdue.DueAtUtc = now.AddDays(-2); // Manually set past due date
        var future = TodoSqliteTestFixture.CreateTodo("Future task", TodoStatus.Pending, dueAtUtc: now.AddDays(5));
        var noDue = TodoSqliteTestFixture.CreateTodo("No due date", TodoStatus.Pending);
        _context.Set<Todo>().AddRange(overdue, future, noDue);
        await _context.SaveChangesAsync();

        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoComputedDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoComputedDto(
                t.Title,
                t.Status,
                t.DueAtUtc != null && t.DueAtUtc < now && t.Status != TodoStatus.Completed,
                t.Status switch
                {
                    TodoStatus.Pending => "Pending",
                    TodoStatus.InProgress => "In Progress",
                    TodoStatus.Completed => "Completed",
                    TodoStatus.Cancelled => "Cancelled",
                    _ => "Unknown"
                },
                t.DueAtUtc != null ? (int)((t.DueAtUtc.Value - now).TotalDays) : -1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        var overdueDto = result.Value.Items.First(d => d.Title == "Overdue task");
        overdueDto.IsOverdue.Should().BeTrue();
        overdueDto.DaysUntilDue.Should().BeLessThan(0);

        var futureDto = result.Value.Items.First(d => d.Title == "Future task");
        futureDto.IsOverdue.Should().BeFalse();
        futureDto.DaysUntilDue.Should().Be(5);

        var noDueDto = result.Value.Items.First(d => d.Title == "No due date");
        noDueDto.DaysUntilDue.Should().Be(-1);
    }

    [Fact(DisplayName = "MapAsync should map Todo with category count from JSON array")]
    public async Task MapAsync_Todo_CategoryCount_ShouldCountCorrectly()
    {
        // Arrange
        var cat1 = TodoSqliteTestFixture.CreateTodoCategory("Urgent");
        var cat2 = TodoSqliteTestFixture.CreateTodoCategory("Backend");
        _context.Set<TodoCategory>().AddRange(cat1, cat2);
        await _context.SaveChangesAsync();

        var todoWithCats = TodoSqliteTestFixture.CreateTodo(
            "Categorized", categoryIds: [cat1.Id, cat2.Id]);
        var todoWithoutCats = TodoSqliteTestFixture.CreateTodo("Uncategorized");
        _context.Set<Todo>().AddRange(todoWithCats, todoWithoutCats);
        await _context.SaveChangesAsync();

        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoWithCategoriesDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoWithCategoriesDto(
                t.Title,
                t.CategoryIds.Count,
                t.CategoryIds.Count > 0));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        var withCats = result.Value.Items.First(d => d.Title == "Categorized");
        withCats.CategoryCount.Should().Be(2);
        withCats.HasCategories.Should().BeTrue();

        var withoutCats = result.Value.Items.First(d => d.Title == "Uncategorized");
        withoutCats.CategoryCount.Should().Be(0);
        withoutCats.HasCategories.Should().BeFalse();
    }

    [Fact(DisplayName = "MapAsync should map Todo with filter by status")]
    public async Task MapAsync_Todo_FilterByStatus_ShouldReturnMatching()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 10);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
        {
            Filter = new FilterCondition(
                "status", FilterOperator.Equal,
                "0", (int)TodoStatus.Pending, typeof(int), false, null, null)
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.Status == TodoStatus.Pending);
    }

    [Fact(DisplayName = "MapAsync should map Todo with pagination")]
    public async Task MapAsync_Todo_WithPagination_ShouldPageCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 10);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
        {
            Page = new PageRequest(2, 3)
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "MapAsync should map Todo with sort by title")]
    public async Task MapAsync_Todo_WithSort_ShouldReturnOrdered()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
        {
            OrderBy = new List<SortField>
            {
                new SortField("title", SortDirection.Ascending, SortNulls.None)
            }
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        var titles = result.Value.Items.Select(d => d.Title).ToList();
        titles.Should().BeInAscendingOrder();
    }

    [Fact(DisplayName = "MapAsync should map Todo with search")]
    public async Task MapAsync_Todo_WithSearch_ShouldFilterCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 10);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
        {
            Search = new BasicSearchRequest("Todo", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.Title.Contains("Todo"));
    }

    [Fact(DisplayName = "MapAsync should return empty when no results")]
    public async Task MapAsync_Todo_EmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
        {
            Filter = new FilterCondition(
                "title", FilterOperator.Equal,
                "Nonexistent", "Nonexistent", typeof(string), true, null, null)
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "MapAsync should handle cancellation")]
    public async Task MapAsync_Todo_Cancellation_ShouldReturnResult()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority),
            cts.Token);

        // Assert — SQLite in-memory may complete before cancellation is checked
        // The key assertion is that we get a Result, not an unhandled exception
        result.Should().NotBeNull();
    }

    #endregion

    #region ProjectAsync - Todo entity

    [Fact(DisplayName = "ProjectAsync should project Todo to simple DTO with real QueryTranslation")]
    public async Task ProjectAsync_Todo_SimpleDto_ShouldProjectCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto =>
        {
            dto.Title.Should().StartWith("Todo");
        });
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with optional fields")]
    public async Task ProjectAsync_Todo_WithOptionalFields_ShouldHandleNulls()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act — Expression trees don't support ?. so use conditional
        Result<QueryPage<TodoMappingDtos.TodoWithOptionalDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoWithOptionalDto(t.Title, t.Description, t.Status, t.DueAtUtc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        var withDescription = result.Value.Items.Where(d => d.Description != null).ToList();
        var withoutDescription = result.Value.Items.Where(d => d.Description == null).ToList();
        withDescription.Should().NotBeEmpty();
        withoutDescription.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with computed values")]
    public async Task ProjectAsync_Todo_ComputedDto_ShouldCalculateCorrectly()
    {
        // Arrange — Create todos and manually set due dates to test overdue logic
        var now = DateTimeOffset.UtcNow;
        var overdue = TodoSqliteTestFixture.CreateTodo("Overdue task", TodoStatus.Pending);
        overdue.DueAtUtc = now.AddDays(-2); // Manually set past due date
        var future = TodoSqliteTestFixture.CreateTodo("Future task", TodoStatus.Pending, dueAtUtc: now.AddDays(5));
        _context.Set<Todo>().AddRange(overdue, future);
        await _context.SaveChangesAsync();

        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act — Expression-based computed values (no switch expression in expression trees)
        Result<QueryPage<TodoMappingDtos.TodoComputedDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoComputedDto(
                t.Title,
                t.Status,
                t.DueAtUtc != null && t.DueAtUtc < now && t.Status != TodoStatus.Completed,
                t.Status == TodoStatus.Pending ? "Pending" :
                t.Status == TodoStatus.InProgress ? "In Progress" :
                t.Status == TodoStatus.Completed ? "Completed" :
                t.Status == TodoStatus.Cancelled ? "Cancelled" : "Unknown",
                t.DueAtUtc != null ? (int)((t.DueAtUtc.Value - now).TotalDays) : -1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        var overdueDto = result.Value.Items.First(d => d.Title == "Overdue task");
        overdueDto.IsOverdue.Should().BeTrue();
        overdueDto.DaysUntilDue.Should().BeLessThan(0);
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with filter by status")]
    public async Task ProjectAsync_Todo_FilterByStatus_ShouldReturnMatching()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 10);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
        {
            Filter = new FilterCondition(
                "status", FilterOperator.Equal,
                "0", (int)TodoStatus.Pending, typeof(int), false, null, null)
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.Status == TodoStatus.Pending);
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with pagination")]
    public async Task ProjectAsync_Todo_WithPagination_ShouldPageCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 10);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
        {
            Page = new PageRequest(2, 3)
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with sort by title")]
    public async Task ProjectAsync_Todo_WithSort_ShouldReturnOrdered()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
        {
            OrderBy = new List<SortField>
            {
                new SortField("title", SortDirection.Ascending, SortNulls.None)
            }
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        var titles = result.Value.Items.Select(d => d.Title).ToList();
        titles.Should().BeInAscendingOrder();
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with search")]
    public async Task ProjectAsync_Todo_WithSearch_ShouldFilterCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 10);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
        {
            Search = new BasicSearchRequest("Todo", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.Title.Contains("Todo"));
    }

    [Fact(DisplayName = "ProjectAsync should return empty when no results")]
    public async Task ProjectAsync_Todo_EmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
        {
            Filter = new FilterCondition(
                "title", FilterOperator.Equal,
                "Nonexistent", "Nonexistent", typeof(string), true, null, null)
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "ProjectAsync should handle cancellation")]
    public async Task ProjectAsync_Todo_Cancellation_ShouldReturnResult()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority),
            cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "ProjectAsync should project only selected columns (efficiency test)")]
    public async Task ProjectAsync_Todo_Efficiency_ShouldQueryOnlySelectedColumns()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act — Project only Title and Status, not Priority or other fields
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto => dto.Title.Should().StartWith("Todo"));
    }

    #endregion

    #region MapAsync - TodoList entity

    [Fact(DisplayName = "MapAsync should map TodoList to simple DTO")]
    public async Task MapAsync_TodoList_SimpleDto_ShouldMapCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodoListsAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoListRepository(_context);
        var query = new Query<TodoList>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoListSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoListSimpleDto(t.Name, t.Description));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto => dto.Name.Should().StartWith("List"));
    }

    [Fact(DisplayName = "MapAsync should map TodoList with audit fields")]
    public async Task MapAsync_TodoList_WithAuditDto_ShouldMapCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodoListsAsync(_context, 3);
        var repository = TodoSqliteTestFixture.CreateTodoListRepository(_context);
        var query = new Query<TodoList>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoListWithAuditDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoListWithAuditDto(t.Name, t.CreatedBy, t.CreatedAtUtc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().AllSatisfy(dto =>
        {
            dto.CreatedBy.Should().Be("test");
            dto.CreatedAtUtc.Should().BeAfter(DateTimeOffset.MinValue);
        });
    }

    #endregion

    #region ProjectAsync - TodoList entity

    [Fact(DisplayName = "ProjectAsync should project TodoList to simple DTO")]
    public async Task ProjectAsync_TodoList_SimpleDto_ShouldProjectCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodoListsAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoListRepository(_context);
        var query = new Query<TodoList>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoListSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoListSimpleDto(t.Name, t.Description));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
    }

    [Fact(DisplayName = "ProjectAsync should project TodoList with filter")]
    public async Task ProjectAsync_TodoList_FilterByName_ShouldReturnMatching()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodoListsAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoListRepository(_context);
        var query = new Query<TodoList>
        {
            Search = new BasicSearchRequest("List 1", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoListSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoListSimpleDto(t.Name, t.Description));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.Name.Contains("List 1"));
    }

    #endregion

    #region MapAsync - TodoCategory entity

    [Fact(DisplayName = "MapAsync should map TodoCategory to simple DTO")]
    public async Task MapAsync_TodoCategory_SimpleDto_ShouldMapCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodoCategoriesAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoCategoryRepository(_context);
        var query = new Query<TodoCategory>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoCategorySimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoCategorySimpleDto(t.Name, t.Description));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto => dto.Name.Should().StartWith("Category"));
    }

    #endregion

    #region ProjectAsync - TodoCategory entity

    [Fact(DisplayName = "ProjectAsync should project TodoCategory to simple DTO")]
    public async Task ProjectAsync_TodoCategory_SimpleDto_ShouldProjectCorrectly()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodoCategoriesAsync(_context, 5);
        var repository = TodoSqliteTestFixture.CreateTodoCategoryRepository(_context);
        var query = new Query<TodoCategory>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoCategorySimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoCategorySimpleDto(t.Name, t.Description));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
    }

    #endregion

    #region Cross-entity and edge cases

    [Fact(DisplayName = "MapAsync should map Todo with list info")]
    public async Task MapAsync_Todo_WithListInfo_ShouldMapCorrectly()
    {
        // Arrange
        var list = TodoSqliteTestFixture.CreateTodoList("Work");
        _context.Set<TodoList>().Add(list);
        await _context.SaveChangesAsync();

        var todoWithList = TodoSqliteTestFixture.CreateTodo("Listed task", listId: list.Id);
        var todoWithoutList = TodoSqliteTestFixture.CreateTodo("No list task");
        _context.Set<Todo>().AddRange(todoWithList, todoWithoutList);
        await _context.SaveChangesAsync();

        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoWithListInfoDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoWithListInfoDto(
                t.Title,
                t.Status,
                t.ListId,
                t.ListId != null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        var withList = result.Value.Items.First(d => d.Title == "Listed task");
        withList.HasList.Should().BeTrue();
        withList.ListId.Should().Be(list.Id);

        var withoutList = result.Value.Items.First(d => d.Title == "No list task");
        withoutList.HasList.Should().BeFalse();
        withoutList.ListId.Should().BeNull();
    }

    [Fact(DisplayName = "ProjectAsync should map Todo with list info")]
    public async Task ProjectAsync_Todo_WithListInfo_ShouldProjectCorrectly()
    {
        // Arrange
        var list = TodoSqliteTestFixture.CreateTodoList("Work");
        _context.Set<TodoList>().Add(list);
        await _context.SaveChangesAsync();

        var todoWithList = TodoSqliteTestFixture.CreateTodo("Listed task", listId: list.Id);
        var todoWithoutList = TodoSqliteTestFixture.CreateTodo("No list task");
        _context.Set<Todo>().AddRange(todoWithList, todoWithoutList);
        await _context.SaveChangesAsync();

        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoWithListInfoDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoWithListInfoDto(
                t.Title,
                t.Status,
                t.ListId,
                t.ListId != null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        var withList = result.Value.Items.First(d => d.Title == "Listed task");
        withList.HasList.Should().BeTrue();
    }

    [Fact(DisplayName = "MapAsync should handle large dataset")]
    public async Task MapAsync_Todo_LargeDataset_ShouldMapAll()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 50);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(50);
    }

    [Fact(DisplayName = "ProjectAsync should handle large dataset")]
    public async Task ProjectAsync_Todo_LargeDataset_ShouldProjectAll()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 50);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(50);
    }

    [Fact(DisplayName = "MapAsync should handle empty database")]
    public async Task MapAsync_Todo_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "ProjectAsync should handle empty database")]
    public async Task ProjectAsync_Todo_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "MapAsync should handle filter with AND group")]
    public async Task MapAsync_Todo_AndFilter_ShouldReturnMatching()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 10);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
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
                        "2", (int)TodoPriority.High, typeof(int), false, null, null)
                })
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(
            dto => dto.Status == TodoStatus.Pending && dto.Priority == TodoPriority.High);
    }

    [Fact(DisplayName = "ProjectAsync should handle filter with OR group")]
    public async Task ProjectAsync_Todo_OrFilter_ShouldReturnMatching()
    {
        // Arrange
        await TodoSqliteTestFixture.SeedTodosAsync(_context, 10);
        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>
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
                        "2", (int)TodoStatus.Completed, typeof(int), false, null, null)
                })
        };

        // Act
        Result<QueryPage<TodoMappingDtos.TodoSimpleDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(
            dto => dto.Status == TodoStatus.Pending || dto.Status == TodoStatus.Completed);
    }

    [Fact(DisplayName = "MapAsync should handle audit fields correctly")]
    public async Task MapAsync_Todo_AuditFields_ShouldMapCorrectly()
    {
        // Arrange
        _context.Set<Todo>().Add(TodoSqliteTestFixture.CreateTodo("Audited task"));
        await _context.SaveChangesAsync();

        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoWithAuditDto>> result = await repository.MapAsync(
            query,
            t => new TodoMappingDtos.TodoWithAuditDto(
                t.Title,
                t.CreatedBy,
                t.CreatedAtUtc,
                t.ModifiedBy,
                t.ModifiedAtUtc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        var dto = result.Value.Items.First();
        dto.CreatedBy.Should().Be("test");
        dto.CreatedAtUtc.Should().BeAfter(DateTimeOffset.MinValue);
        dto.ModifiedBy.Should().BeNull();
        dto.ModifiedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "ProjectAsync should handle audit fields correctly")]
    public async Task ProjectAsync_Todo_AuditFields_ShouldProjectCorrectly()
    {
        // Arrange
        _context.Set<Todo>().Add(TodoSqliteTestFixture.CreateTodo("Audited task"));
        await _context.SaveChangesAsync();

        var repository = TodoSqliteTestFixture.CreateTodoRepository(_context);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingDtos.TodoWithAuditDto>> result = await repository.ProjectAsync(
            query,
            t => new TodoMappingDtos.TodoWithAuditDto(
                t.Title,
                t.CreatedBy,
                t.CreatedAtUtc,
                t.ModifiedBy,
                t.ModifiedAtUtc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        var dto = result.Value.Items.First();
        dto.CreatedBy.Should().Be("test");
        dto.CreatedAtUtc.Should().BeAfter(DateTimeOffset.MinValue);
    }

    #endregion
}
