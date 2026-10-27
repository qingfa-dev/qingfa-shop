using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Contract.Todos.Models;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Commands;

/// <summary>Unit tests for <see cref="UpdateTodoHandler"/>.</summary>
public sealed class UpdateTodoHandlerTest
{
    private static async Task<Result<TodoModel>> UpdateAsync(
        TodoHandlerFixture fixture,
        UpdateTodoCommand command)
    {
        var handler = new UpdateTodoHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        return await handler.Handle(command, CancellationToken.None);
    }

    [Fact(DisplayName = "Update should patch only the supplied fields")]
    public async Task Handle_WithTitle_ShouldChangeTitleAndStampModified()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo("old");
        fixture.RegisterTodo(todo);

        // Act
        Result<TodoModel> result = await UpdateAsync(
            fixture,
            new UpdateTodoCommand(todo.Id, Title: "  new  "));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("new");
        result.Value.ModifiedAtUtc.Should().Be(TodoHandlerFixture.Now);
        result.Value.ModifiedBy.Should().Be(TodoHandlerFixture.Actor);
    }

    [Fact(DisplayName = "Update should clear the description when asked")]
    public async Task Handle_WithClearDescription_ShouldRemoveDescription()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = Todo.Create(
            Guid.NewGuid(),
            TodoHandlerFixture.TenantId,
            "task",
            TodoHandlerFixture.Now,
            TodoHandlerFixture.Actor,
            description: "remove me").Value;
        fixture.RegisterTodo(todo);

        // Act
        Result<TodoModel> result = await UpdateAsync(
            fixture,
            new UpdateTodoCommand(todo.Id, ClearDescription: true));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Fact(DisplayName = "Update should return not found for an unknown todo")]
    public async Task Handle_WhenMissing_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Guid id = Guid.NewGuid();

        // Act
        Result<TodoModel> result = await UpdateAsync(fixture, new UpdateTodoCommand(id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == RepositoryOutcome.Failure.NotFoundFor<Todo>(id).Code);
        fixture.TodoRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Update should hide todos owned by another tenant")]
    public async Task Handle_WithForeignTenant_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo foreign = Todo.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "foreign",
            TodoHandlerFixture.Now,
            TodoHandlerFixture.Actor).Value;
        fixture.RegisterTodo(foreign);

        // Act
        Result<TodoModel> result = await UpdateAsync(
            fixture,
            new UpdateTodoCommand(foreign.Id, Title: "hijack"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == RepositoryOutcome.Failure.NotFoundFor<Todo>(foreign.Id).Code);
    }

    [Fact(DisplayName = "Update should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result<TodoModel> result = await UpdateAsync(
            fixture,
            new UpdateTodoCommand(Guid.NewGuid(), Title: "task"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Tenantable.TenantId.Required");
    }
}
