using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Unit tests for the delete todo slice.</summary>
public sealed class DeleteTodoTest
{
    private static DeleteTodo.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Soft delete should raise a soft delete event")]
    public async Task Handle_Soft_ShouldRaiseSoftDeleteEvent()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodo.UseCase(new DeleteTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        todo.IsDeleted.Should().BeTrue();
        todo.DeletedAtUtc.Should().Be(TodoHandlerFixture.Now);
        TodoDeleted deleted = todo.UncommittedEvents.OfType<TodoDeleted>().Should().ContainSingle().Subject;
        deleted.IsHard.Should().BeFalse();
        fixture.TodoRepository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Hard delete should raise a hard delete event")]
    public async Task Handle_Hard_ShouldRaiseHardDeleteEvent()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodo.UseCase(
                new DeleteTodo.Request(todo.Id, DeleteMode.Hard)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        TodoDeleted deleted = todo.UncommittedEvents.OfType<TodoDeleted>().Should().ContainSingle().Subject;
        deleted.IsHard.Should().BeTrue();
        fixture.TodoRepository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Delete should return not found for an unknown todo")]
    public async Task Handle_WhenMissing_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodo.UseCase(new DeleteTodo.Request(Guid.NewGuid())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Repository.Entity.NotFound");
        fixture.TodoRepository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Delete should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodo.UseCase(new DeleteTodo.Request(Guid.NewGuid())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Delete validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new DeleteTodo.UseCaseValidator().Validate(
            new DeleteTodo.UseCase(new DeleteTodo.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
