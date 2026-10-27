using Application.Features.Todos.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Contract.Todos.Models;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Commands;

/// <summary>Unit tests for <see cref="RestoreTodoHandler"/>.</summary>
public sealed class RestoreTodoHandlerTest
{
    private static RestoreTodoHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Restore should clear the deletion state")]
    public async Task Handle_WhenDeleted_ShouldRestore()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.IsDeleted = true;
        todo.DeletedAtUtc = TodoHandlerFixture.Now;
        todo.DeletedBy = TodoHandlerFixture.Actor;
        fixture.RegisterTodo(todo);

        // Act
        Result<TodoModel> result = await CreateHandler(fixture).Handle(
            new RestoreTodoCommand(todo.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeleted.Should().BeFalse();
        result.Value.DeletedAtUtc.Should().BeNull();
        result.Value.DeletedBy.Should().BeNull();
        result.Value.ModifiedAtUtc.Should().Be(TodoHandlerFixture.Now);
    }

    [Fact(DisplayName = "Restore should reject a todo that is not deleted")]
    public async Task Handle_WhenNotDeleted_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);

        // Act
        Result<TodoModel> result = await CreateHandler(fixture).Handle(
            new RestoreTodoCommand(todo.Id),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        fixture.TodoRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
