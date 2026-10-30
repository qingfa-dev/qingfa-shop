using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Unit tests for the update todo slice.</summary>
public sealed class UpdateTodoTest
{
    private static async Task<Result<UpdateTodo.Response>> UpdateAsync(
        TodoHandlerFixture fixture,
        UpdateTodo.Request request)
    {
        var handler = new UpdateTodo.UseCaseHandler(
            fixture.TodoRepository.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        return await handler.Handle(
            new UpdateTodo.UseCase(request),
            CancellationToken.None);
    }

    [Fact(DisplayName = "Update should patch only the supplied fields")]
    public async Task Handle_WithTitle_ShouldChangeTitleAndStampModified()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo("old");
        todo.ClearUncommittedEvents();
        fixture.RegisterTodo(todo);

        // Act
        Result<UpdateTodo.Response> result = await UpdateAsync(
            fixture,
            new UpdateTodo.Request
            {
                Id = todo.Id,
                Title = "  new  ",
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("new");
        result.Value.ModifiedAtUtc.Should().Be(TodoHandlerFixture.Now);
        result.Value.ModifiedBy.Should().Be(TodoHandlerFixture.Actor);
        todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoUpdated>();
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
        Result<UpdateTodo.Response> result = await UpdateAsync(
            fixture,
            new UpdateTodo.Request
            {
                Id = todo.Id,
                ClearDescription = true,
            });

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
        Result<UpdateTodo.Response> result = await UpdateAsync(
            fixture,
            new UpdateTodo.Request { Id = id });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == RepositoryOutcome.Codes.EntityNotFound);
        fixture.TodoRepository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
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
        Result<UpdateTodo.Response> result = await UpdateAsync(
            fixture,
            new UpdateTodo.Request
            {
                Id = foreign.Id,
                Title = "hijack",
            });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == RepositoryOutcome.Codes.EntityNotFound);
        fixture.TodoRepository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
        foreign.UncommittedEvents.OfType<TodoUpdated>().Should().BeEmpty();
    }

    [Fact(DisplayName = "Update should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result<UpdateTodo.Response> result = await UpdateAsync(
            fixture,
            new UpdateTodo.Request
            {
                Id = Guid.NewGuid(),
                Title = "task",
            });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Tenantable.TenantId.Required");
    }

    [Fact(DisplayName = "Update validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new UpdateTodo.UseCaseValidator().Validate(
            new UpdateTodo.UseCase(new UpdateTodo.Request
            {
                Id = Guid.Empty,
                Title = "task",
            }));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
