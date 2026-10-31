using Application.Modules.Todos.Categories.Commands;
using Application.Unit.Tests.Modules.Todos.Fixtures;

using Core.Modules.Todos.Categories;

namespace Application.Unit.Tests.Modules.Todos.Categories.Commands;

/// <summary>Unit tests for the restore category slice.</summary>
public sealed class RestoreTodoCategoryTest
{
    private static RestoreTodoCategory.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.CategoryRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Restore should clear the deletion state")]
    public async Task Handle_WhenDeleted_ShouldRestore()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        category.IsDeleted = true;
        fixture.RegisterCategory(category);

        // Act
        Result<RestoreTodoCategory.Response> result = await CreateHandler(fixture).Handle(
            new RestoreTodoCategory.UseCase(new RestoreTodoCategory.Request(category.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeleted.Should().BeFalse();
    }

    [Fact(DisplayName = "Restore should reject a category that is not deleted")]
    public async Task Handle_WhenNotDeleted_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        fixture.RegisterCategory(category);

        // Act
        Result<RestoreTodoCategory.Response> result = await CreateHandler(fixture).Handle(
            new RestoreTodoCategory.UseCase(new RestoreTodoCategory.Request(category.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Restore validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new RestoreTodoCategory.UseCaseValidator().Validate(
            new RestoreTodoCategory.UseCase(new RestoreTodoCategory.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
