using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Contract.Todos.Models;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Commands;

/// <summary>Unit tests for the todo lifecycle handlers.</summary>
public sealed class TodoLifecycleHandlerTest
{
    [Fact(DisplayName = "Start should move a pending todo into progress")]
    public async Task Start_FromPending_ShouldSucceed()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);
        var handler = new StartTodoHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(new StartTodoCommand(todo.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TodoStatus.InProgress);
        result.Value.ModifiedAtUtc.Should().Be(TodoHandlerFixture.Now);
    }

    [Fact(DisplayName = "Start should reject a completed todo")]
    public async Task Start_FromCompleted_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Status = TodoStatus.Completed;
        fixture.RegisterTodo(todo);
        var handler = new StartTodoHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(new StartTodoCommand(todo.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        fixture.TodoRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Complete should stamp the completion instant")]
    public async Task Complete_FromInProgress_ShouldStampCompletedAt()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Status = TodoStatus.InProgress;
        fixture.RegisterTodo(todo);
        var handler = new CompleteTodoHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(new CompleteTodoCommand(todo.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TodoStatus.Completed);
        result.Value.CompletedAtUtc.Should().Be(TodoHandlerFixture.Now);
    }

    [Fact(DisplayName = "Complete should reject a pending todo")]
    public async Task Complete_FromPending_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);
        var handler = new CompleteTodoHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(new CompleteTodoCommand(todo.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Cancel should cancel a pending todo")]
    public async Task Cancel_FromPending_ShouldSucceed()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);
        var handler = new CancelTodoHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(new CancelTodoCommand(todo.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TodoStatus.Cancelled);
    }

    [Fact(DisplayName = "Reopen should clear the completion instant")]
    public async Task Reopen_FromCompleted_ShouldClearCompletedAt()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Status = TodoStatus.Completed;
        todo.CompletedAtUtc = TodoHandlerFixture.Now;
        fixture.RegisterTodo(todo);
        var handler = new ReopenTodoHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(new ReopenTodoCommand(todo.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TodoStatus.Pending);
        result.Value.CompletedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Lifecycle commands should return not found for a missing todo")]
    public async Task Start_WhenMissing_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Guid id = Guid.NewGuid();
        var handler = new StartTodoHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoModel> result = await handler.Handle(new StartTodoCommand(id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == RepositoryOutcome.Failure.NotFoundFor<Todo>(id).Code);
    }
}
