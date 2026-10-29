using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Unit tests for the reopen todo slice.</summary>
public sealed class ReopenTodoTest
{
    private static ReopenTodo.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Reopen should clear the completion instant")]
    public async Task Handle_FromCompleted_ShouldClearCompletedAt()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Status = TodoStatus.Completed;
        todo.CompletedAtUtc = TodoHandlerFixture.Now;
        fixture.RegisterTodo(todo);

        // Act
        Result<ReopenTodo.Response> result = await CreateHandler(fixture).Handle(
            new ReopenTodo.UseCase(new ReopenTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TodoStatus.Pending);
        result.Value.CompletedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Reopen validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new ReopenTodo.UseCaseValidator().Validate(
            new ReopenTodo.UseCase(new ReopenTodo.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
