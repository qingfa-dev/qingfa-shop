using Application.Common.Persistence;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Searching;
using Application.Common.Querying.Sorting;

using Core.Common.Monad.Results;
using Core.Todos;

using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Integration.Tests.Persistence.Todos;

/// <summary>
/// DTOs for Todo mapping integration tests.
/// </summary>
public static class TodoMappingIntegrationDtos
{
    public record TodoSimpleDto(string Title, TodoStatus Status, TodoPriority Priority);
    public record TodoWithOptionalDto(string Title, string? Description, TodoStatus Status, DateTimeOffset? DueAtUtc);
    public record TodoWithAuditDto(string Title, string? CreatedBy, DateTimeOffset CreatedAtUtc);
    public record TodoListSimpleDto(string Name, string? Description);
    public record TodoCategorySimpleDto(string Name, string? Description);
    public record TodoWithListInfoDto(string Title, TodoStatus Status, Guid? ListId, bool HasList);
}

/// <summary>
/// Integration tests for EfCoreRepository mapping methods with real PostgreSQL QueryTranslation.
/// Uses TodoEfCoreFixture for real database-backed tests.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Mapping.Todo.PostgreSQL")]
[Collection("Todo EfCore")]
public sealed class TodoMappingIntegrationTest(TodoEfCoreFixture fixture) : IAsyncLifetime
{
    private readonly TodoEfCoreFixture _fixture = fixture;

    private IMappableRepository<Todo, Guid> _todoRepo =>
        _fixture.CurrentScope?.ServiceProvider.GetRequiredService<IPersistentRepository<Todo, Guid>>() as IMappableRepository<Todo, Guid>
        ?? throw new InvalidOperationException("Todo repository not found");

    private IMappableRepository<TodoList, Guid> _listRepo =>
        _fixture.CurrentScope?.ServiceProvider.GetRequiredService<IPersistentRepository<TodoList, Guid>>() as IMappableRepository<TodoList, Guid>
        ?? throw new InvalidOperationException("TodoList repository not found");

    private IMappableRepository<TodoCategory, Guid> _categoryRepo =>
        _fixture.CurrentScope?.ServiceProvider.GetRequiredService<IPersistentRepository<TodoCategory, Guid>>() as IMappableRepository<TodoCategory, Guid>
        ?? throw new InvalidOperationException("TodoCategory repository not found");

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _fixture.CleanAsync();
        _fixture.CreateScope();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _fixture.CleanAsync();
    }

    #region MapAsync - Todo entity

    [Fact(DisplayName = "MapAsync should map Todo to simple DTO with PostgreSQL")]
    public async Task MapAsync_Todo_SimpleDto_ShouldMapCorrectly()
    {
        // Arrange
        await SeedTodosAsync(5);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto => dto.Title.Should().StartWith("Todo"));
    }

    [Fact(DisplayName = "MapAsync should map Todo with optional fields via PostgreSQL")]
    public async Task MapAsync_Todo_WithOptionalFields_ShouldHandleNulls()
    {
        // Arrange
        await SeedTodosAsync(5);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoWithOptionalDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoWithOptionalDto(t.Title, t.Description, t.Status, t.DueAtUtc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        var withDescription = result.Value.Items.Where(d => d.Description != null).ToList();
        var withoutDescription = result.Value.Items.Where(d => d.Description == null).ToList();
        withDescription.Should().NotBeEmpty();
        withoutDescription.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "MapAsync should map Todo with filter by status via PostgreSQL")]
    public async Task MapAsync_Todo_FilterByStatus_ShouldReturnMatching()
    {
        // Arrange
        await SeedTodosAsync(10);
        var query = new Query<Todo>
        {
            Filter = new FilterCondition(
                "status", FilterOperator.Equal,
                "0", (int)TodoStatus.Pending, typeof(int), false, null, null)
        };

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.Status == TodoStatus.Pending);
    }

    [Fact(DisplayName = "MapAsync should map Todo with pagination via PostgreSQL")]
    public async Task MapAsync_Todo_WithPagination_ShouldPageCorrectly()
    {
        // Arrange
        await SeedTodosAsync(10);
        var query = new Query<Todo>
        {
            Page = new PageRequest(2, 3)
        };

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "MapAsync should map Todo with sort by title via PostgreSQL")]
    public async Task MapAsync_Todo_WithSort_ShouldReturnOrdered()
    {
        // Arrange
        await SeedTodosAsync(5);
        var query = new Query<Todo>
        {
            OrderBy = new List<SortField>
            {
                new SortField("title", SortDirection.Ascending, SortNulls.None)
            }
        };

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        var titles = result.Value.Items.Select(d => d.Title).ToList();
        titles.Should().BeInAscendingOrder();
    }

    [Fact(DisplayName = "MapAsync should map Todo with search via PostgreSQL")]
    public async Task MapAsync_Todo_WithSearch_ShouldFilterCorrectly()
    {
        // Arrange
        await SeedTodosAsync(10);
        var query = new Query<Todo>
        {
            Search = new BasicSearchRequest("Todo", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.Title.Contains("Todo"));
    }

    [Fact(DisplayName = "MapAsync should return empty when no results via PostgreSQL")]
    public async Task MapAsync_Todo_EmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        await SeedTodosAsync(5);
        var query = new Query<Todo>
        {
            Filter = new FilterCondition(
                "title", FilterOperator.Equal,
                "Nonexistent", "Nonexistent", typeof(string), true, null, null)
        };

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "MapAsync should handle AND filter group via PostgreSQL")]
    public async Task MapAsync_Todo_AndFilter_ShouldReturnMatching()
    {
        // Arrange
        await SeedTodosAsync(10);
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
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(
            dto => dto.Status == TodoStatus.Pending && dto.Priority == TodoPriority.High);
    }

    [Fact(DisplayName = "MapAsync should handle audit fields via PostgreSQL")]
    public async Task MapAsync_Todo_AuditFields_ShouldMapCorrectly()
    {
        // Arrange
        var todo = CreateTodo("Audited task");
        await _fixture.TodoRepository.AddAsync(todo);
        await _fixture.UnitOfWork.SaveChangesAsync();

        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoWithAuditDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoWithAuditDto(t.Title, t.CreatedBy, t.CreatedAtUtc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        var dto = result.Value.Items.First();
        dto.CreatedBy.Should().Be("test");
        dto.CreatedAtUtc.Should().BeAfter(DateTimeOffset.MinValue);
    }

    #endregion

    #region ProjectAsync - Todo entity

    [Fact(DisplayName = "ProjectAsync should project Todo to simple DTO with PostgreSQL")]
    public async Task ProjectAsync_Todo_SimpleDto_ShouldProjectCorrectly()
    {
        // Arrange
        await SeedTodosAsync(5);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto => dto.Title.Should().StartWith("Todo"));
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with optional fields via PostgreSQL")]
    public async Task ProjectAsync_Todo_WithOptionalFields_ShouldHandleNulls()
    {
        // Arrange
        await SeedTodosAsync(5);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoWithOptionalDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoWithOptionalDto(t.Title, t.Description, t.Status, t.DueAtUtc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        var withDescription = result.Value.Items.Where(d => d.Description != null).ToList();
        var withoutDescription = result.Value.Items.Where(d => d.Description == null).ToList();
        withDescription.Should().NotBeEmpty();
        withoutDescription.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with filter via PostgreSQL")]
    public async Task ProjectAsync_Todo_FilterByStatus_ShouldReturnMatching()
    {
        // Arrange
        await SeedTodosAsync(10);
        var query = new Query<Todo>
        {
            Filter = new FilterCondition(
                "status", FilterOperator.Equal,
                "0", (int)TodoStatus.Pending, typeof(int), false, null, null)
        };

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.Status == TodoStatus.Pending);
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with pagination via PostgreSQL")]
    public async Task ProjectAsync_Todo_WithPagination_ShouldPageCorrectly()
    {
        // Arrange
        await SeedTodosAsync(10);
        var query = new Query<Todo>
        {
            Page = new PageRequest(2, 3)
        };

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with sort via PostgreSQL")]
    public async Task ProjectAsync_Todo_WithSort_ShouldReturnOrdered()
    {
        // Arrange
        await SeedTodosAsync(5);
        var query = new Query<Todo>
        {
            OrderBy = new List<SortField>
            {
                new SortField("title", SortDirection.Ascending, SortNulls.None)
            }
        };

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        var titles = result.Value.Items.Select(d => d.Title).ToList();
        titles.Should().BeInAscendingOrder();
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with search via PostgreSQL")]
    public async Task ProjectAsync_Todo_WithSearch_ShouldFilterCorrectly()
    {
        // Arrange
        await SeedTodosAsync(10);
        var query = new Query<Todo>
        {
            Search = new BasicSearchRequest("Todo", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(dto => dto.Title.Contains("Todo"));
    }

    [Fact(DisplayName = "ProjectAsync should return empty when no results via PostgreSQL")]
    public async Task ProjectAsync_Todo_EmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        await SeedTodosAsync(5);
        var query = new Query<Todo>
        {
            Filter = new FilterCondition(
                "title", FilterOperator.Equal,
                "Nonexistent", "Nonexistent", typeof(string), true, null, null)
        };

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "ProjectAsync should handle OR filter group via PostgreSQL")]
    public async Task ProjectAsync_Todo_OrFilter_ShouldReturnMatching()
    {
        // Arrange
        await SeedTodosAsync(10);
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
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(
            dto => dto.Status == TodoStatus.Pending || dto.Status == TodoStatus.Completed);
    }

    [Fact(DisplayName = "ProjectAsync should handle audit fields via PostgreSQL")]
    public async Task ProjectAsync_Todo_AuditFields_ShouldProjectCorrectly()
    {
        // Arrange
        var todo = CreateTodo("Audited task");
        await _fixture.TodoRepository.AddAsync(todo);
        await _fixture.UnitOfWork.SaveChangesAsync();

        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoWithAuditDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoWithAuditDto(t.Title, t.CreatedBy, t.CreatedAtUtc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        var dto = result.Value.Items.First();
        dto.CreatedBy.Should().Be("test");
        dto.CreatedAtUtc.Should().BeAfter(DateTimeOffset.MinValue);
    }

    #endregion

    #region MapAsync - TodoList entity

    [Fact(DisplayName = "MapAsync should map TodoList to simple DTO via PostgreSQL")]
    public async Task MapAsync_TodoList_SimpleDto_ShouldMapCorrectly()
    {
        // Arrange
        await SeedTodoListsAsync(5);
        var query = new Query<TodoList>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoListSimpleDto>> result = await _listRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoListSimpleDto(t.Name, t.Description));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto => dto.Name.Should().StartWith("List"));
    }

    #endregion

    #region ProjectAsync - TodoList entity

    [Fact(DisplayName = "ProjectAsync should project TodoList to simple DTO via PostgreSQL")]
    public async Task ProjectAsync_TodoList_SimpleDto_ShouldProjectCorrectly()
    {
        // Arrange
        await SeedTodoListsAsync(5);
        var query = new Query<TodoList>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoListSimpleDto>> result = await _listRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoListSimpleDto(t.Name, t.Description));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
    }

    #endregion

    #region MapAsync - TodoCategory entity

    [Fact(DisplayName = "MapAsync should map TodoCategory to simple DTO via PostgreSQL")]
    public async Task MapAsync_TodoCategory_SimpleDto_ShouldMapCorrectly()
    {
        // Arrange
        await SeedTodoCategoriesAsync(5);
        var query = new Query<TodoCategory>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoCategorySimpleDto>> result = await _categoryRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoCategorySimpleDto(t.Name, t.Description));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto => dto.Name.Should().StartWith("Category"));
    }

    #endregion

    #region ProjectAsync - TodoCategory entity

    [Fact(DisplayName = "ProjectAsync should project TodoCategory to simple DTO via PostgreSQL")]
    public async Task ProjectAsync_TodoCategory_SimpleDto_ShouldProjectCorrectly()
    {
        // Arrange
        await SeedTodoCategoriesAsync(5);
        var query = new Query<TodoCategory>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoCategorySimpleDto>> result = await _categoryRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoCategorySimpleDto(t.Name, t.Description));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
    }

    #endregion

    #region Cross-entity and edge cases

    [Fact(DisplayName = "MapAsync should map Todo with list info via PostgreSQL")]
    public async Task MapAsync_Todo_WithListInfo_ShouldMapCorrectly()
    {
        // Arrange
        var list = CreateTodoList("Work");
        await _fixture.TodoListRepository.AddAsync(list);
        await _fixture.UnitOfWork.SaveChangesAsync();

        var todoWithList = CreateTodo("Listed task", listId: list.Id);
        var todoWithoutList = CreateTodo("No list task");
        await _fixture.TodoRepository.AddRangeAsync([todoWithList, todoWithoutList]);
        await _fixture.UnitOfWork.SaveChangesAsync();

        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoWithListInfoDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoWithListInfoDto(
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
    }

    [Fact(DisplayName = "ProjectAsync should project Todo with list info via PostgreSQL")]
    public async Task ProjectAsync_Todo_WithListInfo_ShouldProjectCorrectly()
    {
        // Arrange
        var list = CreateTodoList("Work");
        await _fixture.TodoListRepository.AddAsync(list);
        await _fixture.UnitOfWork.SaveChangesAsync();

        var todoWithList = CreateTodo("Listed task", listId: list.Id);
        var todoWithoutList = CreateTodo("No list task");
        await _fixture.TodoRepository.AddRangeAsync([todoWithList, todoWithoutList]);
        await _fixture.UnitOfWork.SaveChangesAsync();

        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoWithListInfoDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoWithListInfoDto(
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

    [Fact(DisplayName = "MapAsync should handle large dataset via PostgreSQL")]
    public async Task MapAsync_Todo_LargeDataset_ShouldMapAll()
    {
        // Arrange
        await SeedTodosAsync(50);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(50);
    }

    [Fact(DisplayName = "ProjectAsync should handle large dataset via PostgreSQL")]
    public async Task ProjectAsync_Todo_LargeDataset_ShouldProjectAll()
    {
        // Arrange
        await SeedTodosAsync(50);
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(50);
    }

    [Fact(DisplayName = "MapAsync should handle empty database via PostgreSQL")]
    public async Task MapAsync_Todo_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.MapAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "ProjectAsync should handle empty database via PostgreSQL")]
    public async Task ProjectAsync_Todo_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new Query<Todo>();

        // Act
        Result<QueryPage<TodoMappingIntegrationDtos.TodoSimpleDto>> result = await _todoRepo.ProjectAsync(
            query,
            t => new TodoMappingIntegrationDtos.TodoSimpleDto(t.Title, t.Status, t.Priority));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private async Task SeedTodosAsync(int count)
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
        await _fixture.TodoRepository.AddRangeAsync(todos);
        await _fixture.UnitOfWork.SaveChangesAsync();
    }

    private async Task SeedTodoListsAsync(int count)
    {
        var lists = new List<TodoList>();
        for (int i = 1; i <= count; i++)
        {
            lists.Add(CreateTodoList(
                name: $"List {i}",
                description: i % 2 == 0 ? $"Description for list {i}" : null));
        }
        await _fixture.TodoListRepository.AddRangeAsync(lists);
        await _fixture.UnitOfWork.SaveChangesAsync();
    }

    private async Task SeedTodoCategoriesAsync(int count)
    {
        var categories = new List<TodoCategory>();
        for (int i = 1; i <= count; i++)
        {
            categories.Add(CreateTodoCategory(
                name: $"Category {i}",
                description: i % 2 == 0 ? $"Description for category {i}" : null));
        }
        await _fixture.TodoCategoryRepository.AddRangeAsync(categories);
        await _fixture.UnitOfWork.SaveChangesAsync();
    }

    private static Todo CreateTodo(
        string title = "Buy milk",
        TodoStatus status = TodoStatus.Pending,
        TodoPriority priority = TodoPriority.Medium,
        string? description = null,
        DateTimeOffset? dueAtUtc = null,
        Guid? listId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var result = Todo.Create(
            id: Guid.NewGuid(),
            tenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            title: title,
            nowUtc: now,
            actor: "test",
            description: description,
            priority: priority,
            dueAtUtc: dueAtUtc,
            listId: listId);
        var todo = result.Value;
        todo.Status = status;
        return todo;
    }

    private static TodoList CreateTodoList(string name = "Work", string? description = null)
    {
        var now = DateTimeOffset.UtcNow;
        var result = TodoList.Create(
            id: Guid.NewGuid(),
            tenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            name: name,
            nowUtc: now,
            actor: "test",
            description: description);
        return result.Value;
    }

    private static TodoCategory CreateTodoCategory(string name = "Bug", string? description = null)
    {
        var now = DateTimeOffset.UtcNow;
        var result = TodoCategory.Create(
            id: Guid.NewGuid(),
            tenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            name: name,
            nowUtc: now,
            actor: "test",
            description: description);
        return result.Value;
    }

    #endregion
}
