using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Unit tests for the remove todo category slice.</summary>
public sealed class RemoveTodoCategoryTest
{
    private static RemoveTodoCategory.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "RemoveCategory should drop the category reference")]
    public async Task Handle_WithCategoryId_ShouldRemoveReference()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        Guid categoryId = Guid.NewGuid();
        todo.CategoryIds.Add(categoryId);
        fixture.RegisterTodo(todo);

        // Act
        Result<RemoveTodoCategory.Response> result = await CreateHandler(fixture).Handle(
            new RemoveTodoCategory.UseCase(
                new RemoveTodoCategory.Request(todo.Id, categoryId)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryIds.Should().NotContain(categoryId);
    }

    [Fact(DisplayName = "RemoveCategory validator should reject a missing category id")]
    public void Validate_WithEmptyCategoryId_ShouldFail()
    {
        // Act
        var result = new RemoveTodoCategory.UseCaseValidator().Validate(
            new RemoveTodoCategory.UseCase(
                new RemoveTodoCategory.Request(Guid.NewGuid(), Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
