using Application.Features.Todos.Lists.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Lists;

namespace Application.Unit.Tests.Features.Todos.Lists.Commands;

/// <summary>Unit tests for the restore list slice.</summary>
public sealed class RestoreTodoListTest
{
    private static RestoreTodoList.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.ListRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Restore should clear the deletion state")]
    public async Task Handle_WhenDeleted_ShouldRestore()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        list.IsDeleted = true;
        fixture.RegisterList(list);

        // Act
        Result<RestoreTodoList.Response> result = await CreateHandler(fixture).Handle(
            new RestoreTodoList.UseCase(new RestoreTodoList.Request(list.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeleted.Should().BeFalse();
    }

    [Fact(DisplayName = "Restore should reject a list that is not deleted")]
    public async Task Handle_WhenNotDeleted_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        fixture.RegisterList(list);

        // Act
        Result<RestoreTodoList.Response> result = await CreateHandler(fixture).Handle(
            new RestoreTodoList.UseCase(new RestoreTodoList.Request(list.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Restore validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new RestoreTodoList.UseCaseValidator().Validate(
            new RestoreTodoList.UseCase(new RestoreTodoList.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
