using Application.Modules.Todos.Categories.Commands;
using Application.Unit.Tests.Modules.Todos.Fixtures;

using Core.Modules.Todos.Categories;

namespace Application.Unit.Tests.Modules.Todos.Categories.Commands;

/// <summary>Unit tests for the update category slice.</summary>
public sealed class UpdateTodoCategoryTest
{
    [Fact(DisplayName = "Update should rename a category")]
    public async Task Handle_WithName_ShouldRename()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory("old");
        fixture.RegisterCategory(category);
        var handler = new UpdateTodoCategory.UseCaseHandler(
            fixture.CategoryRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<UpdateTodoCategory.Response> result = await handler.Handle(
            new UpdateTodoCategory.UseCase(new UpdateTodoCategory.Request
            {
                Id = category.Id,
                Name = "new",
            }),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("new");
        result.Value.ModifiedAtUtc.Should().Be(TodoHandlerFixture.Now);
    }

    [Fact(DisplayName = "Update should clear the description when asked")]
    public async Task Handle_WithClearDescription_ShouldRemoveDescription()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        category.Description = "remove me";
        fixture.RegisterCategory(category);
        var handler = new UpdateTodoCategory.UseCaseHandler(
            fixture.CategoryRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<UpdateTodoCategory.Response> result = await handler.Handle(
            new UpdateTodoCategory.UseCase(new UpdateTodoCategory.Request
            {
                Id = category.Id,
                ClearDescription = true,
            }),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Fact(DisplayName = "Update validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new UpdateTodoCategory.UseCaseValidator().Validate(
            new UpdateTodoCategory.UseCase(new UpdateTodoCategory.Request
            {
                Id = Guid.Empty,
                Name = "name",
            }));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
