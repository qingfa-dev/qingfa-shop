using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Unit tests for the complete todo slice.</summary>
public sealed class CompleteTodoTest
{
    private static CompleteTodo.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Complete should stamp the completion instant")]
    public async Task Handle_FromInProgress_ShouldStampCompletedAt()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Status = TodoStatus.InProgress;
        fixture.RegisterTodo(todo);

        // Act
        Result<CompleteTodo.Response> result = await CreateHandler(fixture).Handle(
            new CompleteTodo.UseCase(new CompleteTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TodoStatus.Completed);
        result.Value.CompletedAtUtc.Should().Be(TodoHandlerFixture.Now);
    }

    [Fact(DisplayName = "Complete should reject a pending todo")]
    public async Task Handle_FromPending_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);

        // Act
        Result<CompleteTodo.Response> result = await CreateHandler(fixture).Handle(
            new CompleteTodo.UseCase(new CompleteTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Complete validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new CompleteTodo.UseCaseValidator().Validate(
            new CompleteTodo.UseCase(new CompleteTodo.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
