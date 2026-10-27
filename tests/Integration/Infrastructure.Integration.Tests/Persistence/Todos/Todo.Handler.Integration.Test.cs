using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Commands;
using Application.Features.Todos.Queries;

using Contract.Todos.Models;

using Core.Common.Monad.Results;
using Core.Todos;

using Infrastructure.Integration.Tests.Persistence.Todos;

namespace Infrastructure.Integration.Tests.Persistence.Todos;

/// <summary>
/// PostgreSQL-backed tests that drive the Todos handlers through the real
/// repository, unit of work, and DbContext.
/// </summary>
[Collection("Todo EfCore")]
public sealed class TodoHandlerIntegrationTest : IAsyncLifetime
{
    private static readonly Guid TenantId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string Actor = "integration";

    private readonly TodoEfCoreFixture _fixture;

    /// <summary>Creates the test with the shared PostgreSQL fixture.</summary>
    /// <param name="fixture">The shared fixture.</param>
    public TodoHandlerIntegrationTest(TodoEfCoreFixture fixture) => _fixture = fixture;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _fixture.CreateScope();
        _fixture.Ambient.TenantId = TenantId;
        _fixture.Ambient.Actor = Actor;
        await _fixture.CleanAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _fixture.CleanAsync();

    [Fact(DisplayName = "A todo should flow through create, update, complete, delete, and restore")]
    public async Task Todo_ShouldFlowThroughEveryUseCase()
    {
        // Act: create
        Result<TodoModel> created = await CreateTodoAsync("First task");
        created.IsSuccess.Should().BeTrue();
        Guid id = created.Value.Id;

        // Assert: persisted and readable
        Result<TodoModel> fetched = await GetTodoAsync(id);
        fetched.IsSuccess.Should().BeTrue();
        fetched.Value.Title.Should().Be("First task");
        fetched.Value.Status.Should().Be(TodoStatus.Pending);

        // Act: update
        var update = new UpdateTodoHandler(
            _fixture.TodoRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<TodoModel> updated = await update.Handle(
            new UpdateTodoCommand(id, Title: "Renamed", Priority: TodoPriority.High),
            CancellationToken.None);

        updated.IsSuccess.Should().BeTrue(string.Join("; ", updated.Errors.Select(error => $"{error.Code}: {error.Message}")));
        updated.Value.Title.Should().Be("Renamed");
        updated.Value.Priority.Should().Be(TodoPriority.High);

        // Act: start and complete
        var start = new StartTodoHandler(
            _fixture.TodoRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<TodoModel> started = await start.Handle(new StartTodoCommand(id), CancellationToken.None);
        started.IsSuccess.Should().BeTrue();

        var complete = new CompleteTodoHandler(
            _fixture.TodoRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<TodoModel> completed = await complete.Handle(new CompleteTodoCommand(id), CancellationToken.None);
        completed.IsSuccess.Should().BeTrue();
        completed.Value.Status.Should().Be(TodoStatus.Completed);
        completed.Value.CompletedAtUtc.Should().Be(_fixture.Clock.UtcNow);

        // Act: soft delete
        var delete = new DeleteTodoHandler(
            _fixture.TodoRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result deleted = await delete.Handle(new DeleteTodoCommand(id), CancellationToken.None);
        deleted.IsSuccess.Should().BeTrue();

        // Assert: hidden by default, visible when explicitly included
        (await GetTodoAsync(id)).IsFailure.Should().BeTrue();
        Result<TodoModel> deletedRow = await GetTodoAsync(id, includeDeleted: true);
        deletedRow.IsSuccess.Should().BeTrue();
        deletedRow.Value.IsDeleted.Should().BeTrue();

        // Act: restore
        var restore = new RestoreTodoHandler(
            _fixture.TodoRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<TodoModel> restored = await restore.Handle(new RestoreTodoCommand(id), CancellationToken.None);
        restored.IsSuccess.Should().BeTrue();
        restored.Value.IsDeleted.Should().BeFalse();

        (await GetTodoAsync(id)).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Deleting a list should clear the list reference on todos")]
    public async Task DeleteList_ShouldClearTodoReferences()
    {
        // Arrange
        var createList = new CreateTodoListHandler(
            _fixture.TodoListRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<TodoListModel> list = await createList.Handle(
            new CreateTodoListCommand("Inbox"),
            CancellationToken.None);

        list.IsSuccess.Should().BeTrue();

        Result<TodoModel> todo = await CreateTodoAsync("Linked", list.Value.Id);
        todo.IsSuccess.Should().BeTrue();

        // Act
        var deleteList = new DeleteTodoListHandler(
            _fixture.TodoListRepository,
            _fixture.ReferenceCleaner,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result deleted = await deleteList.Handle(
            new DeleteTodoListCommand(list.Value.Id),
            CancellationToken.None);

        // Assert
        deleted.IsSuccess.Should().BeTrue();
        Result<TodoModel> refreshed = await GetTodoAsync(todo.Value.Id);
        refreshed.IsSuccess.Should().BeTrue();
        refreshed.Value.ListId.Should().BeNull();

        var getList = new GetTodoListHandler(_fixture.TodoListRepository, _fixture.Ambient);
        (await getList.Handle(new GetTodoListQuery(list.Value.Id), CancellationToken.None))
            .IsFailure.Should().BeTrue();

        Result<TodoListModel> deletedList = await getList.Handle(
            new GetTodoListQuery(list.Value.Id, IncludeDeleted: true),
            CancellationToken.None);

        deletedList.IsSuccess.Should().BeTrue();
        deletedList.Value.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "Deleting a category should remove the reference from todos")]
    public async Task DeleteCategory_ShouldRemoveTodoReferences()
    {
        // Arrange
        var createCategory = new CreateTodoCategoryHandler(
            _fixture.TodoCategoryRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<TodoCategoryModel> category = await createCategory.Handle(
            new CreateTodoCategoryCommand("Urgent"),
            CancellationToken.None);

        category.IsSuccess.Should().BeTrue();

        var createTodo = new CreateTodoHandler(
            _fixture.TodoRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<TodoModel> todo = await createTodo.Handle(
            new CreateTodoCommand("Tagged", CategoryIds: [category.Value.Id]),
            CancellationToken.None);

        todo.IsSuccess.Should().BeTrue();
        todo.Value.CategoryIds.Should().Contain(category.Value.Id);

        // Act
        var deleteCategory = new DeleteTodoCategoryHandler(
            _fixture.TodoCategoryRepository,
            _fixture.ReferenceCleaner,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result deleted = await deleteCategory.Handle(
            new DeleteTodoCategoryCommand(category.Value.Id),
            CancellationToken.None);

        // Assert
        deleted.IsSuccess.Should().BeTrue();
        Result<TodoModel> refreshed = await GetTodoAsync(todo.Value.Id);
        refreshed.IsSuccess.Should().BeTrue();
        refreshed.Value.CategoryIds.Should().NotContain(category.Value.Id);

        var getCategory = new GetTodoCategoryHandler(_fixture.TodoCategoryRepository, _fixture.Ambient);
        (await getCategory.Handle(new GetTodoCategoryQuery(category.Value.Id), CancellationToken.None))
            .IsFailure.Should().BeTrue();

        Result<TodoCategoryModel> deletedCategory = await getCategory.Handle(
            new GetTodoCategoryQuery(category.Value.Id, IncludeDeleted: true),
            CancellationToken.None);

        deletedCategory.IsSuccess.Should().BeTrue();
        deletedCategory.Value.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "Hard delete should remove the row permanently")]
    public async Task HardDelete_ShouldRemoveRow()
    {
        // Arrange
        Result<TodoModel> todo = await CreateTodoAsync("Temporary");
        todo.IsSuccess.Should().BeTrue();

        var delete = new DeleteTodoHandler(
            _fixture.TodoRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        // Act
        Result deleted = await delete.Handle(
            new DeleteTodoCommand(todo.Value.Id, DeleteMode.Hard),
            CancellationToken.None);

        // Assert
        deleted.IsSuccess.Should().BeTrue(string.Join("; ", deleted.Errors.Select(error => $"{error.Code}: {error.Message}")));
        (await GetTodoAsync(todo.Value.Id, includeDeleted: true)).IsFailure.Should().BeTrue();
    }

    private async Task<Result<TodoModel>> CreateTodoAsync(string title, Guid? listId = null)
    {
        var handler = new CreateTodoHandler(
            _fixture.TodoRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        return await handler.Handle(
            new CreateTodoCommand(title, ListId: listId),
            CancellationToken.None);
    }

    private async Task<Result<TodoModel>> GetTodoAsync(Guid id, bool includeDeleted = false)
    {
        var handler = new GetTodoHandler(_fixture.TodoRepository, _fixture.Ambient);
        return await handler.Handle(new GetTodoQuery(id, includeDeleted), CancellationToken.None);
    }
}
