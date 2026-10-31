using Application.BuildingBlocks.Persistence.Repositories;
using Application.Modules.Todos.Categories.Commands;
using Application.Modules.Todos.Categories.Queries;
using Application.Modules.Todos.Items.Commands;
using Application.Modules.Todos.Items.Models;
using Application.Modules.Todos.Items.Queries;
using Application.Modules.Todos.Lists.Commands;
using Application.Modules.Todos.Lists.Queries;

using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Todos.Items;

namespace Infrastructure.Integration.Tests.Modules.Todos.Persistence;

/// <summary>
/// PostgreSQL-backed tests that drive the Todos handlers through the real event store,
/// projection, read model, and unit of work.
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
        _fixture.Ambient.UserName = Actor;
        await _fixture.CleanAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _fixture.CleanAsync();

    [Fact(DisplayName = "A todo should flow through create, update, complete, delete, and restore")]
    public async Task Todo_ShouldFlowThroughEveryUseCase()
    {
        // Act: create
        Result<CreateTodo.Response> created = await CreateTodoAsync("First task");
        created.IsSuccess.Should().BeTrue();
        Guid id = created.Value.Id;

        // Assert: projected and readable
        Result<GetTodo.Response> fetched = await GetTodoAsync(id);
        fetched.IsSuccess.Should().BeTrue();
        fetched.Value.Title.Should().Be("First task");
        fetched.Value.Status.Should().Be(TodoStatus.Pending);

        // Act: update
        var update = new UpdateTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<UpdateTodo.Response> updated = await update.Handle(
            new UpdateTodo.UseCase(new UpdateTodo.Request
            {
                Id = id,
                Title = "Renamed",
                Priority = TodoPriority.High,
            }),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        updated.IsSuccess.Should().BeTrue(string.Join("; ", updated.Errors.Select(error => $"{error.Code}: {error.Message}")));
        updated.Value.Title.Should().Be("Renamed");
        updated.Value.Priority.Should().Be(TodoPriority.High);

        // Act: start and complete
        var start = new StartTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<StartTodo.Response> started = await start.Handle(
            new StartTodo.UseCase(new StartTodo.Request(id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();
        started.IsSuccess.Should().BeTrue();

        var complete = new CompleteTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<CompleteTodo.Response> completed = await complete.Handle(
            new CompleteTodo.UseCase(new CompleteTodo.Request(id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();
        completed.IsSuccess.Should().BeTrue();
        completed.Value.Status.Should().Be(TodoStatus.Completed);
        completed.Value.CompletedAtUtc.Should().Be(_fixture.Clock.UtcNow);

        // Act: soft delete
        var delete = new DeleteTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result deleted = await delete.Handle(
            new DeleteTodo.UseCase(new DeleteTodo.Request(id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();
        deleted.IsSuccess.Should().BeTrue();

        // Assert: hidden by default, visible when explicitly included
        (await GetTodoAsync(id)).IsFailure.Should().BeTrue();
        Result<GetTodo.Response> deletedRow = await GetTodoAsync(id, includeDeleted: true);
        deletedRow.IsSuccess.Should().BeTrue();
        deletedRow.Value.IsDeleted.Should().BeTrue();

        // Act: restore
        var restore = new RestoreTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<RestoreTodo.Response> restored = await restore.Handle(
            new RestoreTodo.UseCase(new RestoreTodo.Request(id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();
        restored.IsSuccess.Should().BeTrue();
        restored.Value.IsDeleted.Should().BeFalse();

        (await GetTodoAsync(id)).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "A todo should transition through start, complete, reopen, and cancel")]
    public async Task Todo_ShouldTransitionThroughEveryStatus()
    {
        // Arrange
        Result<CreateTodo.Response> created = await CreateTodoAsync("State machine");
        created.IsSuccess.Should().BeTrue();
        Guid id = created.Value.Id;

        // Act: start
        var start = new StartTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<StartTodo.Response> started = await start.Handle(
            new StartTodo.UseCase(new StartTodo.Request(id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        // Assert
        started.IsSuccess.Should().BeTrue();
        started.Value.Status.Should().Be(TodoStatus.InProgress);
        (await GetTodoAsync(id)).Value.Status.Should().Be(TodoStatus.InProgress);

        // Act: complete
        var complete = new CompleteTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<CompleteTodo.Response> completed = await complete.Handle(
            new CompleteTodo.UseCase(new CompleteTodo.Request(id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();
        completed.IsSuccess.Should().BeTrue();
        completed.Value.Status.Should().Be(TodoStatus.Completed);

        // Act: reopen
        var reopen = new ReopenTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<ReopenTodo.Response> reopened = await reopen.Handle(
            new ReopenTodo.UseCase(new ReopenTodo.Request(id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        // Assert
        reopened.IsSuccess.Should().BeTrue();
        reopened.Value.Status.Should().Be(TodoStatus.Pending);
        reopened.Value.CompletedAtUtc.Should().BeNull();

        // Act: cancel
        var cancel = new CancelTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<CancelTodo.Response> cancelled = await cancel.Handle(
            new CancelTodo.UseCase(new CancelTodo.Request(id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        // Assert
        cancelled.IsSuccess.Should().BeTrue();
        cancelled.Value.Status.Should().Be(TodoStatus.Cancelled);
        (await GetTodoAsync(id)).Value.Status.Should().Be(TodoStatus.Cancelled);
    }

    [Fact(DisplayName = "Category and list references should add, clear, and remove through commands")]
    public async Task References_ShouldAddAndRemoveThroughCommands()
    {
        // Arrange
        var createCategory = new CreateTodoCategory.UseCaseHandler(
            _fixture.TodoCategoryRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<CreateTodoCategory.Response> category = await createCategory.Handle(
            new CreateTodoCategory.UseCase(new CreateTodoCategory.Request { Name = "Urgent" }),
            CancellationToken.None);
        category.IsSuccess.Should().BeTrue();

        var createList = new CreateTodoList.UseCaseHandler(
            _fixture.TodoListRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<CreateTodoList.Response> list = await createList.Handle(
            new CreateTodoList.UseCase(new CreateTodoList.Request { Name = "Inbox" }),
            CancellationToken.None);
        list.IsSuccess.Should().BeTrue();

        Result<CreateTodo.Response> todo = await CreateTodoAsync("Tagged");
        todo.IsSuccess.Should().BeTrue();
        Guid id = todo.Value.Id;

        // Act: add category and assign list
        var addCategory = new AddTodoCategory.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<AddTodoCategory.Response> added = await addCategory.Handle(
            new AddTodoCategory.UseCase(new AddTodoCategory.Request(id, category.Value.Id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        var assignList = new AssignTodoToList.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<AssignTodoToList.Response> assigned = await assignList.Handle(
            new AssignTodoToList.UseCase(new AssignTodoToList.Request(id, list.Value.Id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        // Assert
        added.IsSuccess.Should().BeTrue();
        assigned.IsSuccess.Should().BeTrue();
        Result<GetTodo.Response> referenced = await GetTodoAsync(id);
        referenced.Value.CategoryIds.Should().Contain(category.Value.Id);
        referenced.Value.ListId.Should().Be(list.Value.Id);

        // Act: remove category and clear list
        var removeCategory = new RemoveTodoCategory.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<RemoveTodoCategory.Response> removed = await removeCategory.Handle(
            new RemoveTodoCategory.UseCase(new RemoveTodoCategory.Request(id, category.Value.Id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        var clearList = new ClearTodoList.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<ClearTodoList.Response> cleared = await clearList.Handle(
            new ClearTodoList.UseCase(new ClearTodoList.Request(id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        // Assert
        removed.IsSuccess.Should().BeTrue();
        cleared.IsSuccess.Should().BeTrue();
        Result<GetTodo.Response> detached = await GetTodoAsync(id);
        detached.Value.CategoryIds.Should().NotContain(category.Value.Id);
        detached.Value.ListId.Should().BeNull();
    }

    [Fact(DisplayName = "Deleting a list should clear the list reference on todos")]
    public async Task DeleteList_ShouldClearTodoReferences()
    {
        // Arrange
        var createList = new CreateTodoList.UseCaseHandler(
            _fixture.TodoListRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<CreateTodoList.Response> list = await createList.Handle(
            new CreateTodoList.UseCase(new CreateTodoList.Request
            {
                Name = "Inbox",
            }),
            CancellationToken.None);

        list.IsSuccess.Should().BeTrue();

        Result<CreateTodo.Response> todo = await CreateTodoAsync("Linked", list.Value.Id);
        todo.IsSuccess.Should().BeTrue();

        // Act
        var deleteList = new DeleteTodoList.UseCaseHandler(
            _fixture.TodoListRepository,
            _fixture.ReferenceCleaner,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result deleted = await deleteList.Handle(
            new DeleteTodoList.UseCase(new DeleteTodoList.Request(list.Value.Id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        // Assert
        deleted.IsSuccess.Should().BeTrue();
        Result<GetTodo.Response> refreshed = await GetTodoAsync(todo.Value.Id);
        refreshed.IsSuccess.Should().BeTrue();
        refreshed.Value.ListId.Should().BeNull();

        var getList = new GetTodoList.UseCaseHandler(_fixture.TodoListRepository, _fixture.Ambient);
        (await getList.Handle(
                new GetTodoList.UseCase(new GetTodoList.Request(list.Value.Id)),
                CancellationToken.None))
            .IsFailure.Should().BeTrue();

        Result<GetTodoList.Response> deletedList = await getList.Handle(
            new GetTodoList.UseCase(
                new GetTodoList.Request(list.Value.Id, IncludeDeleted: true)),
            CancellationToken.None);

        deletedList.IsSuccess.Should().BeTrue();
        deletedList.Value.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "Deleting a category should remove the reference from todos")]
    public async Task DeleteCategory_ShouldRemoveTodoReferences()
    {
        // Arrange
        var createCategory = new CreateTodoCategory.UseCaseHandler(
            _fixture.TodoCategoryRepository,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        CreateTodoCategory.Request request = new()
        {
            Name = "Urgent"
        };

        Result<CreateTodoCategory.Response> category = await createCategory.Handle(
            new CreateTodoCategory.UseCase(request),
            CancellationToken.None);

        category.IsSuccess.Should().BeTrue();

        var createTodo = new CreateTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<CreateTodo.Response> todo = await createTodo.Handle(
            new CreateTodo.UseCase(new CreateTodo.Request
            {
                Title = "Tagged",
                CategoryIds = [category.Value.Id],
            }),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        todo.IsSuccess.Should().BeTrue();
        todo.Value.CategoryIds.Should().Contain(category.Value.Id);

        // Act
        var deleteCategory = new DeleteTodoCategory.UseCaseHandler(
            _fixture.TodoCategoryRepository,
            _fixture.ReferenceCleaner,
            _fixture.UnitOfWork,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result deleted = await deleteCategory.Handle(
            new DeleteTodoCategory.UseCase(
                new DeleteTodoCategory.Request(category.Value.Id)),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        // Assert
        deleted.IsSuccess.Should().BeTrue();
        Result<GetTodo.Response> refreshed = await GetTodoAsync(todo.Value.Id);
        refreshed.IsSuccess.Should().BeTrue();
        refreshed.Value.CategoryIds.Should().NotContain(category.Value.Id);

        var getCategory = new GetTodoCategory.UseCaseHandler(
            _fixture.TodoCategoryRepository,
            _fixture.Ambient);
        (await getCategory.Handle(
                new GetTodoCategory.UseCase(new GetTodoCategory.Request(category.Value.Id)),
                CancellationToken.None))
            .IsFailure.Should().BeTrue();

        Result<GetTodoCategory.Response> deletedCategory = await getCategory.Handle(
            new GetTodoCategory.UseCase(
                new GetTodoCategory.Request(category.Value.Id, IncludeDeleted: true)),
            CancellationToken.None);

        deletedCategory.IsSuccess.Should().BeTrue();
        deletedCategory.Value.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "Hard delete should remove the row permanently")]
    public async Task HardDelete_ShouldRemoveRow()
    {
        // Arrange
        Result<CreateTodo.Response> todo = await CreateTodoAsync("Temporary");
        todo.IsSuccess.Should().BeTrue();

        var delete = new DeleteTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        // Act
        Result deleted = await delete.Handle(
            new DeleteTodo.UseCase(
                new DeleteTodo.Request(todo.Value.Id, DeleteMode.Hard)),
            CancellationToken.None);
        await _fixture.ProjectAsync();

        // Assert
        deleted.IsSuccess.Should().BeTrue(string.Join("; ", deleted.Errors.Select(error => $"{error.Code}: {error.Message}")));
        (await GetTodoAsync(todo.Value.Id, includeDeleted: true)).IsFailure.Should().BeTrue();

        Result<Maybe<TodoReadModel>> row = await _fixture.TodoReadRepository
            .FindByIdAsync(todo.Value.Id);
        row.IsSuccess.Should().BeTrue();
        row.Value.HasNoValue.Should().BeTrue();
    }

    [Fact(DisplayName = "Command responses should match the projected TodoResponseModel")]
    public async Task Responses_ShouldMatchTheProjection()
    {
        // Act
        Result<CreateTodo.Response> created = await CreateTodoAsync("Parity");
        created.IsSuccess.Should().BeTrue();
        await _fixture.ProjectAsync();
        Result<GetTodo.Response> fetched = await GetTodoAsync(created.Value.Id);

        // Assert
        fetched.IsSuccess.Should().BeTrue();
        created.Value.Should().BeEquivalentTo(fetched.Value);

        // Act
        var update = new UpdateTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);
        Result<UpdateTodo.Response> updated = await update.Handle(
            new UpdateTodo.UseCase(new UpdateTodo.Request
            {
                Id = created.Value.Id,
                Title = "Parity updated",
                Description = "Projected description",
            }),
            CancellationToken.None);
        await _fixture.ProjectAsync();
        Result<GetTodo.Response> afterUpdate = await GetTodoAsync(created.Value.Id);

        // Assert
        updated.IsSuccess.Should().BeTrue();
        afterUpdate.IsSuccess.Should().BeTrue();
        updated.Value.Should().BeEquivalentTo(afterUpdate.Value);
    }

    private async Task<Result<CreateTodo.Response>> CreateTodoAsync(string title, Guid? listId = null)
    {
        var handler = new CreateTodo.UseCaseHandler(
            _fixture.TodoEvents,
            _fixture.Ambient,
            _fixture.Ambient,
            _fixture.Clock);

        Result<CreateTodo.Response> result = await handler.Handle(
            new CreateTodo.UseCase(new CreateTodo.Request
            {
                Title = title,
                ListId = listId,
            }),
            CancellationToken.None);

        if (result.IsSuccess)
            await _fixture.ProjectAsync();

        return result;
    }

    private async Task<Result<GetTodo.Response>> GetTodoAsync(Guid id, bool includeDeleted = false)
    {
        var handler = new GetTodo.UseCaseHandler(_fixture.TodoReadRepository, _fixture.Ambient);
        return await handler.Handle(
            new GetTodo.UseCase(new GetTodo.Request(id, includeDeleted)),
            CancellationToken.None);
    }
}
