using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Categories.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Categories;

namespace Application.Unit.Tests.Features.Todos.Categories.Commands;

/// <summary>Unit tests for the delete category slice.</summary>
public sealed class DeleteTodoCategoryTest
{
    private static DeleteTodoCategory.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.CategoryRepository.Object,
            fixture.Cleaner.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Soft delete should remove references and mark the category deleted")]
    public async Task Handle_Soft_ShouldCleanAndMarkDeleted()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        fixture.RegisterCategory(category);

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodoCategory.UseCase(new DeleteTodoCategory.Request(category.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
        category.IsDeleted.Should().BeTrue();
        fixture.CategoryRepository.Verify(
            repository => repository.UpdateAsync(category, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Cleaner.Verify(
            cleaner => cleaner.RemoveCategoryAsync(
                TodoHandlerFixture.TenantId,
                category.Id,
                TodoHandlerFixture.Now,
                TodoHandlerFixture.Actor,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Hard delete should remove references and the category")]
    public async Task Handle_Hard_ShouldCleanAndRemove()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        fixture.RegisterCategory(category);

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodoCategory.UseCase(
                new DeleteTodoCategory.Request(category.Id, DeleteMode.Hard)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
        fixture.CategoryRepository.Verify(
            repository => repository.RemoveAsync(category, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Delete validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new DeleteTodoCategory.UseCaseValidator().Validate(
            new DeleteTodoCategory.UseCase(new DeleteTodoCategory.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
