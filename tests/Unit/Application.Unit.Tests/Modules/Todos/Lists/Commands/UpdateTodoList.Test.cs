using Application.Modules.Todos.Lists.Commands;
using Application.Unit.Tests.Modules.Todos.Fixtures;

using Core.Modules.Todos.Lists;

namespace Application.Unit.Tests.Modules.Todos.Lists.Commands;

/// <summary>Unit tests for the update list slice.</summary>
public sealed class UpdateTodoListTest
{
    private static UpdateTodoList.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.ListRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Update should rename a list")]
    public async Task Handle_WithName_ShouldRename()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList("old");
        fixture.RegisterList(list);

        // Act
        Result<UpdateTodoList.Response> result = await CreateHandler(fixture).Handle(
            new UpdateTodoList.UseCase(new UpdateTodoList.Request
            {
                Id = list.Id,
                Name = "new",
            }),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("new");
        result.Value.ModifiedAtUtc.Should().Be(TodoHandlerFixture.Now);
    }

    [Fact(DisplayName = "Update should clear the description when asked")]
    public async Task Handle_WithClearDescription_ShouldRemoveDescription()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        list.Description = "remove me";
        fixture.RegisterList(list);

        // Act
        Result<UpdateTodoList.Response> result = await CreateHandler(fixture).Handle(
            new UpdateTodoList.UseCase(new UpdateTodoList.Request
            {
                Id = list.Id,
                ClearDescription = true,
            }),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Fact(DisplayName = "Update validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new UpdateTodoList.UseCaseValidator().Validate(
            new UpdateTodoList.UseCase(new UpdateTodoList.Request
            {
                Id = Guid.Empty,
                Name = "name",
            }));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
