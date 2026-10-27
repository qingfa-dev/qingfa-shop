using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;

using Core.Todos;

namespace Infrastructure.Integration.Tests.Persistence.Todos;

/// <summary>Integration tests for Todo, TodoList, and TodoCategory persistence with PostgreSQL.</summary>
[Collection("Todo EfCore")]
public sealed class TodoRepositoryIntegrationTest(TodoEfCoreFixture fixture) : IAsyncLifetime
{
    private readonly TodoEfCoreFixture _fixture = fixture;
    private IPersistentRepository<Todo, Guid> _todoRepo => _fixture.TodoRepository;
    private IPersistentRepository<TodoList, Guid> _listRepo => _fixture.TodoListRepository;
    private IPersistentRepository<TodoCategory, Guid> _categoryRepo => _fixture.TodoCategoryRepository;
    private IUnitOfWork _unitOfWork => _fixture.UnitOfWork;

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

    [Fact]
    public async Task AddAsync_WhenTodoCreated_ShouldPersistAndRetrieve()
    {
        // Arrange
        var todo = CreateTodo();

        // Act
        var addResult = await _todoRepo.AddAsync(todo);
        var saveResult = await _unitOfWork.SaveChangesAsync();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);

        // Assert
        addResult.IsSuccess.Should().BeTrue();
        saveResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Title.Should().Be("Buy milk");
        getResult.Value.Status.Should().Be(TodoStatus.Pending);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldReturnFailure()
    {
        // Act
        var result = await _todoRepo.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task FindByIdAsync_WhenNotFound_ShouldReturnEmpty()
    {
        // Act
        var result = await _todoRepo.FindByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenEntityExists_ShouldReturnTrue()
    {
        // Arrange
        var todo = CreateTodo();
        await _todoRepo.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _todoRepo.ExistsAsync(todo.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenEntityMissing_ShouldReturnFalse()
    {
        // Act
        var result = await _todoRepo.ExistsAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WhenTodoUpdated_ShouldPersistChanges()
    {
        // Arrange
        var todo = CreateTodo();
        await _todoRepo.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Act
        todo.Title = "Updated title";
        var updateResult = await _todoRepo.UpdateAsync(todo);
        await _unitOfWork.SaveChangesAsync();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Title.Should().Be("Updated title");
    }

    [Fact]
    public async Task RemoveAsync_WhenEntityRemoved_ShouldNotBeFound()
    {
        // Arrange
        var todo = CreateTodo();
        await _todoRepo.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var removeResult = await _todoRepo.RemoveAsync(todo);
        await _unitOfWork.SaveChangesAsync();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);

        // Assert
        removeResult.IsSuccess.Should().BeTrue();
        getResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_WhenTodoListCreated_ShouldPersistAndRetrieve()
    {
        // Arrange
        var list = CreateTodoList();

        // Act
        var addResult = await _listRepo.AddAsync(list);
        var saveResult = await _unitOfWork.SaveChangesAsync();
        var getResult = await _listRepo.GetByIdAsync(list.Id);

        // Assert
        addResult.IsSuccess.Should().BeTrue();
        saveResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Name.Should().Be("Work");
    }

    [Fact]
    public async Task AddAsync_WhenTodoCategoryCreated_ShouldPersistAndRetrieve()
    {
        // Arrange
        var category = CreateTodoCategory();

        // Act
        var addResult = await _categoryRepo.AddAsync(category);
        var saveResult = await _unitOfWork.SaveChangesAsync();
        var getResult = await _categoryRepo.GetByIdAsync(category.Id);

        // Assert
        addResult.IsSuccess.Should().BeTrue();
        saveResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Name.Should().Be("Bug");
    }

    [Fact]
    public async Task AddRangeAsync_WhenMultipleTodos_ShouldPersistAll()
    {
        // Arrange
        var todos = new[] { CreateTodo("Todo 1"), CreateTodo("Todo 2"), CreateTodo("Todo 3") };

        // Act
        var addResult = await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        addResult.IsSuccess.Should().BeTrue();
        foreach (var todo in todos)
        {
            var result = await _todoRepo.GetByIdAsync(todo.Id);
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task QueryAsync_WhenFilteringByStatus_ShouldReturnMatchingTodos()
    {
        // Arrange
        var todo1 = CreateTodo("Pending task", status: TodoStatus.Pending);
        var todo2 = CreateTodo("In progress task", status: TodoStatus.InProgress);
        await _todoRepo.AddRangeAsync(new[] { todo1, todo2 });
        await _unitOfWork.SaveChangesAsync();

        // Act
        var query = new Application.Common.Querying.Query<Todo>
        {
            Filter = new Application.Common.Querying.Filtering.FilterCondition(
                "status", Application.Common.Querying.Filtering.FilterOperator.Equal,
                "0", (int)TodoStatus.Pending, typeof(int), false, null, null)
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Title.Should().Be("Pending task");
    }

    [Fact]
    public async Task CountAsync_WhenCountingTodos_ShouldReturnCorrectCount()
    {
        // Arrange
        var todos = new[] { CreateTodo("A"), CreateTodo("B"), CreateTodo("C") };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _todoRepo.CountAsync(new Application.Common.Querying.Query<Todo>());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    [Fact]
    public async Task AnyAsync_WhenTodosExist_ShouldReturnTrue()
    {
        // Arrange
        await _todoRepo.AddAsync(CreateTodo("Any test"));
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _todoRepo.AnyAsync(new Application.Common.Querying.Query<Todo>());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task QueryAsync_WhenSortingByTitle_ShouldReturnOrderedResults()
    {
        // Arrange
        var todos = new[] { CreateTodo("Charlie"), CreateTodo("Alpha"), CreateTodo("Beta") };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var query = new Application.Common.Querying.Query<Todo>
        {
            OrderBy = new[]
            {
                new Application.Common.Querying.Sorting.SortField(
                    "title", Application.Common.Querying.Sorting.SortDirection.Ascending,
                    Application.Common.Querying.Sorting.SortNulls.None)
            }
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var titles = result.Value.Items.Select(t => t.Title).ToList();
        titles.Should().Equal("Alpha", "Beta", "Charlie");
    }

    [Fact]
    public async Task QueryAsync_WhenPaging_ShouldReturnCorrectPage()
    {
        // Arrange
        var todos = new[]
        {
            CreateTodo("Page 1 Item 1"), CreateTodo("Page 1 Item 2"),
            CreateTodo("Page 2 Item 1"), CreateTodo("Page 2 Item 2")
        };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var query = new Application.Common.Querying.Query<Todo>
        {
            Page = new Application.Common.Querying.Pagination.PageRequest(2, 2)
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(4);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenSingleMatch_ShouldReturnEntity()
    {
        // Arrange
        var todo = CreateTodo("Unique title");
        await _todoRepo.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var query = new Application.Common.Querying.Query<Todo>
        {
            Filter = new Application.Common.Querying.Filtering.FilterCondition(
                "title", Application.Common.Querying.Filtering.FilterOperator.Equal,
                "Unique title", "Unique title", typeof(string), true, null, null)
        };
        var result = await _todoRepo.QueryFirstOrDefaultAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Title.Should().Be("Unique title");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenNoMatch_ShouldReturnEmpty()
    {
        // Act
        var query = new Application.Common.Querying.Query<Todo>
        {
            Filter = new Application.Common.Querying.Filtering.FilterCondition(
                "title", Application.Common.Querying.Filtering.FilterOperator.Equal,
                "Nonexistent", "Nonexistent", typeof(string), true, null, null)
        };
        var result = await _todoRepo.QueryFirstOrDefaultAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_WhenTodoHasCategoryIds_ShouldPersistJsonArray()
    {
        // Arrange
        var category = CreateTodoCategory();
        await _categoryRepo.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        var todo = CreateTodo("Categorized task");
        ((System.Collections.Generic.List<Guid>)todo.CategoryIds).Add(category.Id);

        // Act
        await _todoRepo.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.CategoryIds.Should().Contain(category.Id);
    }

    [Fact]
    public async Task AddAsync_WhenTodoHasListId_ShouldPersistForeignKey()
    {
        // Arrange
        var list = CreateTodoList();
        await _listRepo.AddAsync(list);
        await _unitOfWork.SaveChangesAsync();

        var todo = CreateTodo("Listed task", listId: list.Id);

        // Act
        await _todoRepo.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.ListId.Should().Be(list.Id);
    }

    [Fact]
    public async Task RemoveByIdAsync_WhenIdExists_ShouldRemoveEntity()
    {
        // Arrange
        var todo = CreateTodo("To remove by id");
        await _todoRepo.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var removeResult = await _todoRepo.RemoveAsync(todo.Id);
        await _unitOfWork.SaveChangesAsync();
        var getResult = await _todoRepo.GetByIdAsync(todo.Id);

        // Assert
        removeResult.IsSuccess.Should().BeTrue();
        getResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task QueryAsync_WhenSearchingByTitle_ShouldReturnMatchingTodos()
    {
        // Arrange
        var todos = new[] { CreateTodo("Buy groceries"), CreateTodo("Buy tickets"), CreateTodo("Sell items") };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var query = new Application.Common.Querying.Query<Todo>
        {
            Search = new Application.Common.Querying.Searching.BasicSearchRequest("Buy", Application.Common.Querying.Searching.SearchMode.Contains)
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task QueryAsync_WhenNestedAndFilter_ShouldReturnMatchingTodos()
    {
        // Arrange
        var todos = new[]
        {
            CreateTodo("High priority pending", TodoStatus.Pending, priority: TodoPriority.High),
            CreateTodo("Low priority pending", TodoStatus.Pending, priority: TodoPriority.Low),
            CreateTodo("High priority done", TodoStatus.Completed, priority: TodoPriority.High)
        };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act: AND group — status=Pending AND priority=High
        var query = new Application.Common.Querying.Query<Todo>
        {
            Filter = new Application.Common.Querying.Filtering.FilterGroup(
                Application.Common.Querying.Filtering.FilterLogic.And,
                new[]
                {
                    new Application.Common.Querying.Filtering.FilterCondition(
                        "status", Application.Common.Querying.Filtering.FilterOperator.Equal,
                        "0", (int)TodoStatus.Pending, typeof(int), false, null, null),
                    new Application.Common.Querying.Filtering.FilterCondition(
                        "priority", Application.Common.Querying.Filtering.FilterOperator.Equal,
                        "2", (int)TodoPriority.High, typeof(int), false, null, null)
                })
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Title.Should().Be("High priority pending");
    }

    [Fact]
    public async Task QueryAsync_WhenNestedOrFilter_ShouldReturnMatchingTodos()
    {
        // Arrange
        var todos = new[]
        {
            CreateTodo("Pending task", TodoStatus.Pending),
            CreateTodo("Completed task", TodoStatus.Completed),
            CreateTodo("In progress task", TodoStatus.InProgress)
        };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act: OR group — status=Pending OR status=Completed
        var query = new Application.Common.Querying.Query<Todo>
        {
            Filter = new Application.Common.Querying.Filtering.FilterGroup(
                Application.Common.Querying.Filtering.FilterLogic.Or,
                new[]
                {
                    new Application.Common.Querying.Filtering.FilterCondition(
                        "status", Application.Common.Querying.Filtering.FilterOperator.Equal,
                        "0", (int)TodoStatus.Pending, typeof(int), false, null, null),
                    new Application.Common.Querying.Filtering.FilterCondition(
                        "status", Application.Common.Querying.Filtering.FilterOperator.Equal,
                        "3", (int)TodoStatus.Completed, typeof(int), false, null, null)
                })
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task QueryAsync_WhenDescendingSort_ShouldReturnReversedOrder()
    {
        // Arrange
        var todos = new[] { CreateTodo("Alpha"), CreateTodo("Charlie"), CreateTodo("Beta") };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var query = new Application.Common.Querying.Query<Todo>
        {
            OrderBy = new[]
            {
                new Application.Common.Querying.Sorting.SortField(
                    "title", Application.Common.Querying.Sorting.SortDirection.Descending,
                    Application.Common.Querying.Sorting.SortNulls.None)
            }
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var titles = result.Value.Items.Select(t => t.Title).ToList();
        titles.Should().Equal("Charlie", "Beta", "Alpha");
    }

    [Fact]
    public async Task QueryAsync_WhenMultipleSortFields_ShouldOrderByPrimaryThenSecondary()
    {
        // Arrange
        var todos = new[]
        {
            CreateTodo("Task A", TodoStatus.Pending, priority: TodoPriority.High),
            CreateTodo("Task B", TodoStatus.Pending, priority: TodoPriority.Low),
            CreateTodo("Task C", TodoStatus.Completed, priority: TodoPriority.High)
        };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act: Sort by status ASC, then priority DESC
        var query = new Application.Common.Querying.Query<Todo>
        {
            OrderBy = new[]
            {
                new Application.Common.Querying.Sorting.SortField(
                    "status", Application.Common.Querying.Sorting.SortDirection.Ascending,
                    Application.Common.Querying.Sorting.SortNulls.None),
                new Application.Common.Querying.Sorting.SortField(
                    "priority", Application.Common.Querying.Sorting.SortDirection.Descending,
                    Application.Common.Querying.Sorting.SortNulls.None)
            }
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task QueryAsync_WhenPaginationLastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var todos = new[]
        {
            CreateTodo("Item 1"), CreateTodo("Item 2"),
            CreateTodo("Item 3"), CreateTodo("Item 4"),
            CreateTodo("Item 5")
        };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act: Page 3 of 3 with pageSize=2
        var query = new Application.Common.Querying.Query<Todo>
        {
            Page = new Application.Common.Querying.Pagination.PageRequest(3, 2)
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(5);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task QueryAsync_WhenPageBeyondTotal_ShouldReturnEmpty()
    {
        // Arrange
        await _todoRepo.AddAsync(CreateTodo("Single item"));
        await _unitOfWork.SaveChangesAsync();

        // Act: Page 10 with pageSize=1
        var query = new Application.Common.Querying.Query<Todo>
        {
            Page = new Application.Common.Querying.Pagination.PageRequest(10, 1)
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_WhenSearchWithUnicode_ShouldReturnMatchingTodos()
    {
        // Arrange
        var todos = new[] { CreateTodo("Tâche française"), CreateTodo("Aufgabe"), CreateTodo("Normal task") };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var query = new Application.Common.Querying.Query<Todo>
        {
            Search = new Application.Common.Querying.Searching.BasicSearchRequest("Tâche", Application.Common.Querying.Searching.SearchMode.Contains)
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Title.Should().Be("Tâche française");
    }

    [Fact]
    public async Task QueryAsync_WhenSearchWithSpecialCharacters_ShouldReturnMatchingTodos()
    {
        // Arrange
        var todos = new[] { CreateTodo("Task #1 (urgent)"), CreateTodo("Task 2"), CreateTodo("Task [3]") };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var query = new Application.Common.Querying.Query<Todo>
        {
            Search = new Application.Common.Querying.Searching.BasicSearchRequest("urgent", Application.Common.Querying.Searching.SearchMode.Contains)
        };
        var result = await _todoRepo.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddAsync_WhenNullEntity_ShouldReturnFailure()
    {
        // Act
        var result = await _todoRepo.AddAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AddRangeAsync_WhenEmptyCollection_ShouldSucceed()
    {
        // Act
        var result = await _todoRepo.AddRangeAsync(Array.Empty<Todo>());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_WhenNonExistingId_ShouldReturnFailure()
    {
        // Act
        var result = await _todoRepo.RemoveAsync(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task QuerySingleAsync_WhenMultipleResults_ShouldReturnFailure()
    {
        // Arrange
        var todos = new[] { CreateTodo("A"), CreateTodo("B") };
        await _todoRepo.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _todoRepo.QuerySingleAsync(new Application.Common.Querying.Query<Todo>());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #region Helpers

    private static Todo CreateTodo(
        string title = "Buy milk",
        TodoStatus status = TodoStatus.Pending,
        TodoPriority priority = TodoPriority.Medium,
        Guid? listId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var result = Todo.Create(
            id: Guid.NewGuid(),
            tenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            title: title,
            nowUtc: now,
            actor: "test",
            description: "Test description",
            priority: priority,
            dueAtUtc: now.AddDays(1),
            listId: listId);

        var todo = result.Value;
        todo.Status = status;
        return todo;
    }

    private static TodoList CreateTodoList(string name = "Work")
    {
        var now = DateTimeOffset.UtcNow;
        var result = TodoList.Create(
            id: Guid.NewGuid(),
            tenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            name: name,
            nowUtc: now,
            actor: "test",
            description: "Test list");
        return result.Value;
    }

    private static TodoCategory CreateTodoCategory(string name = "Bug")
    {
        var now = DateTimeOffset.UtcNow;
        var result = TodoCategory.Create(
            id: Guid.NewGuid(),
            tenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            name: name,
            nowUtc: now,
            actor: "test",
            description: "Test category");
        return result.Value;
    }

    #endregion
}
