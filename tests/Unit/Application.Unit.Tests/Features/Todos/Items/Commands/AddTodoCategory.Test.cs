using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Unit tests for the add todo category slice.</summary>
public sealed class AddTodoCategoryTest
{
    private static AddTodoCategory.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "AddCategory should append the category reference")]
    public async Task Handle_WithCategoryId_ShouldAppendReference()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        fixture.RegisterTodo(todo);
        Guid categoryId = Guid.NewGuid();

        // Act
        Result<AddTodoCategory.Response> result = await CreateHandler(fixture).Handle(
            new AddTodoCategory.UseCase(new AddTodoCategory.Request(todo.Id, categoryId)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryIds.Should().Contain(categoryId);
    }

    [Fact(DisplayName = "AddCategory should reject a duplicate reference")]
    public async Task Handle_WhenDuplicate_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        Guid categoryId = Guid.NewGuid();
        todo.CategoryIds.Add(categoryId);
        fixture.RegisterTodo(todo);

        // Act
        Result<AddTodoCategory.Response> result = await CreateHandler(fixture).Handle(
            new AddTodoCategory.UseCase(new AddTodoCategory.Request(todo.Id, categoryId)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "AddCategory validator should reject a missing category id")]
    public void Validate_WithEmptyCategoryId_ShouldFail()
    {
        // Act
        var result = new AddTodoCategory.UseCaseValidator().Validate(
            new AddTodoCategory.UseCase(
                new AddTodoCategory.Request(Guid.NewGuid(), Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
