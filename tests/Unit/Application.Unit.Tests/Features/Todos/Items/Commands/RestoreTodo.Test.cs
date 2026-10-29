using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Unit tests for the restore todo slice.</summary>
public sealed class RestoreTodoTest
{
    private static RestoreTodo.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
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
        Result<RestoreTodo.Response> result = await CreateHandler(fixture).Handle(
            new RestoreTodo.UseCase(new RestoreTodo.Request(todo.Id)),
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
        Result<RestoreTodo.Response> result = await CreateHandler(fixture).Handle(
            new RestoreTodo.UseCase(new RestoreTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        fixture.TodoRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Restore validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new RestoreTodo.UseCaseValidator().Validate(
            new RestoreTodo.UseCase(new RestoreTodo.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
