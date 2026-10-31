using Application.Modules.Todos.Items.Commands;
using Application.Unit.Tests.Modules.Todos.Fixtures;

using Core.Modules.Todos.Items;

namespace Application.Unit.Tests.Modules.Todos.Items.Commands;

/// <summary>Unit tests for the start todo slice.</summary>
public sealed class StartTodoTest
{
    private static StartTodo.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Start should move a pending todo into progress")]
    public async Task Handle_FromPending_ShouldSucceed()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);

        // Act
        Result<StartTodo.Response> result = await CreateHandler(fixture).Handle(
            new StartTodo.UseCase(new StartTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TodoStatus.InProgress);
        result.Value.ModifiedAtUtc.Should().Be(TodoHandlerFixture.Now);
        todo.UncommittedEvents.OfType<TodoStatusChanged>().Should().ContainSingle()
            .Which.To.Should().Be(TodoStatus.InProgress);
    }

    [Fact(DisplayName = "Start should reject a completed todo")]
    public async Task Handle_FromCompleted_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Start(TodoHandlerFixture.Now, TodoHandlerFixture.Actor).IsSuccess.Should().BeTrue();
        todo.Complete(TodoHandlerFixture.Now, TodoHandlerFixture.Actor).IsSuccess.Should().BeTrue();
        fixture.RegisterTodo(todo);

        // Act
        Result<StartTodo.Response> result = await CreateHandler(fixture).Handle(
            new StartTodo.UseCase(new StartTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        fixture.TodoRepository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Start should return not found for a missing todo")]
    public async Task Handle_WhenMissing_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Guid id = Guid.NewGuid();

        // Act
        Result<StartTodo.Response> result = await CreateHandler(fixture).Handle(
            new StartTodo.UseCase(new StartTodo.Request(id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Repository.Entity.NotFound");
    }

    [Fact(DisplayName = "Start should treat a soft-deleted stream as not found")]
    public async Task Handle_WhenSoftDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Delete(
            isHard: false,
            TodoHandlerFixture.Now,
            TodoHandlerFixture.Actor).IsSuccess.Should().BeTrue();
        fixture.RegisterTodo(todo);

        // Act
        Result<StartTodo.Response> result = await CreateHandler(fixture).Handle(
            new StartTodo.UseCase(new StartTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error =>
            error.Code == "Repository.Entity.NotFound" && error.Status == 404);
        todo.UncommittedEvents.OfType<TodoStatusChanged>().Should().BeEmpty();
        fixture.TodoRepository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Start validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new StartTodo.UseCaseValidator().Validate(
            new StartTodo.UseCase(new StartTodo.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
