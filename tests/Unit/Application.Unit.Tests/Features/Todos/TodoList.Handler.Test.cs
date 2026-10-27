using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Commands;
using Application.Features.Todos.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Contract.Common;
using Contract.Todos.Models;

using Core.Todos;

namespace Application.Unit.Tests.Features.Todos;

/// <summary>Unit tests for the todo list handlers.</summary>
public sealed class TodoListHandlerTest
{
    [Fact(DisplayName = "Create should persist a list for the ambient tenant")]
    public async Task Create_ShouldPersist()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList? stored = null;
        fixture.ListRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TodoList>(), It.IsAny<CancellationToken>()))
            .Callback<TodoList, CancellationToken>((list, _) => stored = list)
            .ReturnsAsync(Result.Success());
        var handler = new CreateTodoListHandler(
            fixture.ListRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoListModel> result = await handler.Handle(
            new CreateTodoListCommand("Groceries"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        stored!.TenantId.Should().Be(TodoHandlerFixture.TenantId);
        result.Value.Name.Should().Be("Groceries");
    }

    [Fact(DisplayName = "Update should rename a list")]
    public async Task Update_ShouldRename()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList("old");
        fixture.RegisterList(list);
        var handler = new UpdateTodoListHandler(
            fixture.ListRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoListModel> result = await handler.Handle(
            new UpdateTodoListCommand(list.Id, Name: "new"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("new");
        result.Value.ModifiedAtUtc.Should().Be(TodoHandlerFixture.Now);
    }

    [Fact(DisplayName = "Soft delete should clear references and mark the list deleted")]
    public async Task Delete_Soft_ShouldCleanAndMarkDeleted()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        fixture.RegisterList(list);
        var handler = new DeleteTodoListHandler(
            fixture.ListRepository.Object,
            fixture.Cleaner.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result result = await handler.Handle(
            new DeleteTodoListCommand(list.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        list.IsDeleted.Should().BeTrue();
        fixture.ListRepository.Verify(
            repository => repository.UpdateAsync(list, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Cleaner.Verify(
            cleaner => cleaner.ClearListAsync(
                TodoHandlerFixture.TenantId,
                list.Id,
                TodoHandlerFixture.Now,
                TodoHandlerFixture.Actor,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Hard delete should clear references and remove the list")]
    public async Task Delete_Hard_ShouldCleanAndRemove()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        fixture.RegisterList(list);
        var handler = new DeleteTodoListHandler(
            fixture.ListRepository.Object,
            fixture.Cleaner.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result result = await handler.Handle(
            new DeleteTodoListCommand(list.Id, DeleteMode.Hard),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.ListRepository.Verify(
            repository => repository.RemoveAsync(list, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Restore should clear the deletion state")]
    public async Task Restore_ShouldClearDeletion()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        list.IsDeleted = true;
        fixture.RegisterList(list);
        var handler = new RestoreTodoListHandler(
            fixture.ListRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoListModel> result = await handler.Handle(
            new RestoreTodoListCommand(list.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeleted.Should().BeFalse();
    }

    [Fact(DisplayName = "Get should return the list for the ambient tenant")]
    public async Task Get_ShouldReturnList()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        fixture.RegisterList(list);
        var handler = new GetTodoListHandler(fixture.ListRepository.Object, fixture.Tenant);

        // Act
        Result<TodoListModel> result = await handler.Handle(
            new GetTodoListQuery(list.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(list.Id);
    }

    [Fact(DisplayName = "Search should map matching lists into a page")]
    public async Task Search_ShouldMapPage()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterListMap();
        TodoList list = fixture.CreateList();
        fixture.ListRepository
            .Setup(repository => repository.QueryAsync(
                It.IsAny<Query<TodoList>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<TodoList>>.Success(
                new QueryPage<TodoList>([list], 1, 10, 1)));
        var handler = new SearchTodoListsHandler(
            fixture.ListRepository.Object,
            fixture.Registry,
            fixture.Tenant);

        // Act
        Result<PageModel<TodoListModel>> result = await handler.Handle(
            new SearchTodoListsQuery(new QueryParameters { Page = 1, PageSize = 10 }),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(model => model.Id == list.Id);
    }
}
