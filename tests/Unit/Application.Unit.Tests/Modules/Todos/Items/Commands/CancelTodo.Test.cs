using Application.Modules.Todos.Items.Commands;
using Application.Unit.Tests.Modules.Todos.Fixtures;

using Core.Modules.Todos.Items;

namespace Application.Unit.Tests.Modules.Todos.Items.Commands;

/// <summary>Unit tests for the cancel todo slice.</summary>
public sealed class CancelTodoTest
{
    private static CancelTodo.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Cancel should cancel a pending todo")]
    public async Task Handle_FromPending_ShouldSucceed()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);

        // Act
        Result<CancelTodo.Response> result = await CreateHandler(fixture).Handle(
            new CancelTodo.UseCase(new CancelTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TodoStatus.Cancelled);
        todo.UncommittedEvents.OfType<TodoStatusChanged>().Should().ContainSingle()
            .Which.To.Should().Be(TodoStatus.Cancelled);
    }

    [Fact(DisplayName = "Cancel validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new CancelTodo.UseCaseValidator().Validate(
            new CancelTodo.UseCase(new CancelTodo.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
