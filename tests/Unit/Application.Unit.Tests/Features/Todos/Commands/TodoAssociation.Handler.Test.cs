using Application.Features.Todos.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Contract.Todos.Models;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Commands;

/// <summary>Unit tests for the todo association handlers.</summary>
public sealed class TodoAssociationHandlerTest
{
    [Fact(DisplayName = "Assign should set the list reference")]
    public async Task Assign_ShouldSetListId()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);
        Guid listId = Guid.NewGuid();
        var handler = new AssignTodoToListHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(
            new AssignTodoToListCommand(todo.Id, listId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ListId.Should().Be(listId);
    }

    [Fact(DisplayName = "Clear should remove the list reference")]
    public async Task Clear_ShouldNullListId()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.ListId = Guid.NewGuid();
        fixture.RegisterTodo(todo);
        var handler = new ClearTodoListHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(
            new ClearTodoListCommand(todo.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ListId.Should().BeNull();
    }

    [Fact(DisplayName = "AddCategory should append the category reference")]
    public async Task AddCategory_ShouldAppendReference()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);
        Guid categoryId = Guid.NewGuid();
        var handler = new AddTodoCategoryHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(
            new AddTodoCategoryCommand(todo.Id, categoryId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryIds.Should().Contain(categoryId);
    }

    [Fact(DisplayName = "AddCategory should reject a duplicate reference")]
    public async Task AddCategory_WhenDuplicate_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        Guid categoryId = Guid.NewGuid();
        todo.CategoryIds.Add(categoryId);
        fixture.RegisterTodo(todo);
        var handler = new AddTodoCategoryHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(
            new AddTodoCategoryCommand(todo.Id, categoryId),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "RemoveCategory should drop the category reference")]
    public async Task RemoveCategory_ShouldRemoveReference()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        Guid categoryId = Guid.NewGuid();
        todo.CategoryIds.Add(categoryId);
        fixture.RegisterTodo(todo);
        var handler = new RemoveTodoCategoryHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(
            new RemoveTodoCategoryCommand(todo.Id, categoryId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryIds.Should().NotContain(categoryId);
    }
}
