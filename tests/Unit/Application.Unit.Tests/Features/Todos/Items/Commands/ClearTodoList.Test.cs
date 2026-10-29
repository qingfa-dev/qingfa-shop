using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Unit tests for the clear todo list slice.</summary>
public sealed class ClearTodoListTest
{
    private static ClearTodoList.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Clear should remove the list reference")]
    public async Task Handle_WithList_ShouldNullListId()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.ListId = Guid.NewGuid();
        fixture.RegisterTodo(todo);

        // Act
        Result<ClearTodoList.Response> result = await CreateHandler(fixture).Handle(
            new ClearTodoList.UseCase(new ClearTodoList.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ListId.Should().BeNull();
    }

    [Fact(DisplayName = "Clear validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new ClearTodoList.UseCaseValidator().Validate(
            new ClearTodoList.UseCase(new ClearTodoList.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
