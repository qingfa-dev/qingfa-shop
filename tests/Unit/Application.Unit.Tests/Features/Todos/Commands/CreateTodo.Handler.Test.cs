using Application.Features.Todos.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Contract.Todos.Models;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Commands;

/// <summary>Unit tests for <see cref="CreateTodoHandler"/>.</summary>
public sealed class CreateTodoHandlerTest
{
    private static async Task<Result<TodoModel>> CreateAsync(
        TodoHandlerFixture fixture,
        CreateTodoCommand command)
    {
        var handler = new CreateTodoHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        return await handler.Handle(command, CancellationToken.None);
    }

    [Fact(DisplayName = "Create should persist a pending todo with audit metadata")]
    public async Task Handle_WithValidCommand_ShouldPersistTodo()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo? stored = null;
        fixture.TodoRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => stored = todo)
            .ReturnsAsync(Result.Success());

        // Act
        Result<TodoModel> result = await CreateAsync(
            fixture,
            new CreateTodoCommand("Ship the feature", Description: "With tests", Priority: TodoPriority.High));

        // Assert
        result.IsSuccess.Should().BeTrue();
        stored.Should().NotBeNull();
        stored!.TenantId.Should().Be(TodoHandlerFixture.TenantId);
        stored.Status.Should().Be(TodoStatus.Pending);
        stored.Title.Should().Be("Ship the feature");
        result.Value.Title.Should().Be("Ship the feature");
        fixture.UnitOfWork.Verify(
            unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Create should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result<TodoModel> result = await CreateAsync(fixture, new CreateTodoCommand("Task"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Tenantable.TenantId.Required");
        fixture.TodoRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Create should add every requested category")]
    public async Task Handle_WithCategoryIds_ShouldAddCategoryReferences()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Guid categoryId = Guid.NewGuid();
        Todo? stored = null;
        fixture.TodoRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => stored = todo)
            .ReturnsAsync(Result.Success());

        // Act
        Result<TodoModel> result = await CreateAsync(
            fixture,
            new CreateTodoCommand("Task", CategoryIds: [categoryId]));

        // Assert
        result.IsSuccess.Should().BeTrue();
        stored!.CategoryIds.Should().Contain(categoryId);
    }
}
