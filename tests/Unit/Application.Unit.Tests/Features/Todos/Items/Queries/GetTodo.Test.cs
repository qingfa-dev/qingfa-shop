using Application.Features.Todos.Items.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Queries;

/// <summary>Unit tests for the get todo slice.</summary>
public sealed class GetTodoTest
{
    private static GetTodo.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.TodoRepository.Object, fixture.Tenant);

    [Fact(DisplayName = "Get should return the todo for the ambient tenant")]
    public async Task Handle_WhenFound_ShouldReturnModel()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo("Read me");
        fixture.RegisterTodo(todo);

        // Act
        Result<GetTodo.Response> result = await CreateHandler(fixture).Handle(
            new GetTodo.UseCase(new GetTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(todo.Id);
        result.Value.Title.Should().Be("Read me");
    }

    [Fact(DisplayName = "Get should hide a soft-deleted todo by default")]
    public async Task Handle_WhenDeleted_ShouldReturnNotFoundByDefault()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.IsDeleted = true;
        fixture.RegisterTodo(todo);

        // Act
        Result<GetTodo.Response> result = await CreateHandler(fixture).Handle(
            new GetTodo.UseCase(new GetTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Get should return a soft-deleted todo when IncludeDeleted is set")]
    public async Task Handle_WhenDeletedAndIncluded_ShouldReturnModel()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.IsDeleted = true;
        fixture.RegisterTodo(todo);

        // Act
        Result<GetTodo.Response> result = await CreateHandler(fixture).Handle(
            new GetTodo.UseCase(new GetTodo.Request(todo.Id, IncludeDeleted: true)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "Get should not expose another tenant's todo")]
    public async Task Handle_WithForeignTenant_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = Todo.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "foreign",
            TodoHandlerFixture.Now,
            TodoHandlerFixture.Actor).Value;
        fixture.RegisterTodo(todo);

        // Act
        Result<GetTodo.Response> result = await CreateHandler(fixture).Handle(
            new GetTodo.UseCase(new GetTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
