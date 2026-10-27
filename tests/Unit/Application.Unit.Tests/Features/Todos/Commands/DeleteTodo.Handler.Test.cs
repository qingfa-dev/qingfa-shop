using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Commands;

/// <summary>Unit tests for <see cref="DeleteTodoHandler"/>.</summary>
public sealed class DeleteTodoHandlerTest
{
    private static DeleteTodoHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Soft delete should mark the todo deleted")]
    public async Task Handle_Soft_ShouldMarkDeleted()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodoCommand(todo.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeTrue();
        todo.DeletedAtUtc.Should().Be(TodoHandlerFixture.Now);
        fixture.TodoRepository.Verify(
            repository => repository.UpdateAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.TodoRepository.Verify(
            repository => repository.RemoveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Hard delete should remove the todo permanently")]
    public async Task Handle_Hard_ShouldRemoveTodo()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodoCommand(todo.Id, DeleteMode.Hard),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.TodoRepository.Verify(
            repository => repository.RemoveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
        todo.IsDeleted.Should().BeFalse();
    }

    [Fact(DisplayName = "Delete should return not found for an unknown todo")]
    public async Task Handle_WhenMissing_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodoCommand(Guid.NewGuid()),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Delete should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodoCommand(Guid.NewGuid()),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
