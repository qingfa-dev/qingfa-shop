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
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Restore should clear the deletion state")]
    public async Task Handle_WhenDeleted_ShouldRestore()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Delete(isHard: false, TodoHandlerFixture.Now, TodoHandlerFixture.Actor)
            .IsSuccess.Should().BeTrue();
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
        todo.UncommittedEvents.OfType<TodoRestored>().Should().ContainSingle();
    }

    [Fact(DisplayName = "Restore should be an idempotent no-op for a live todo")]
    public async Task Handle_WhenNotDeleted_ShouldBeIdempotent()
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
        result.IsSuccess.Should().BeTrue();
        todo.UncommittedEvents.OfType<TodoRestored>().Should().BeEmpty();
    }

    [Fact(DisplayName = "Restore should reject a hard-deleted stream")]
    public async Task Handle_WhenHardDeleted_ShouldFailWithoutSaving()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Delete(isHard: true, TodoHandlerFixture.Now, TodoHandlerFixture.Actor)
            .IsSuccess.Should().BeTrue();
        todo.ClearUncommittedEvents();
        fixture.RegisterTodo(todo);

        // Act
        Result<RestoreTodo.Response> result = await CreateHandler(fixture).Handle(
            new RestoreTodo.UseCase(new RestoreTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error =>
            error.Code == TodoOutcome.Failure.HardDeletedNotFound.Code);
        todo.UncommittedEvents.Should().BeEmpty();
        fixture.TodoRepository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
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
