using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Unit tests for the assign todo to list slice.</summary>
public sealed class AssignTodoToListTest
{
    private static AssignTodoToList.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Assign should set the list reference")]
    public async Task Handle_WithListId_ShouldSetListId()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);
        Guid listId = Guid.NewGuid();

        // Act
        Result<AssignTodoToList.Response> result = await CreateHandler(fixture).Handle(
            new AssignTodoToList.UseCase(new AssignTodoToList.Request(todo.Id, listId)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ListId.Should().Be(listId);
    }

    [Fact(DisplayName = "Assign validator should reject a missing list id")]
    public void Validate_WithEmptyListId_ShouldFail()
    {
        // Act
        var result = new AssignTodoToList.UseCaseValidator().Validate(
            new AssignTodoToList.UseCase(
                new AssignTodoToList.Request(Guid.NewGuid(), Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
