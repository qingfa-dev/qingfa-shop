using Application.Features.Todos.Categories.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Categories;

namespace Application.Unit.Tests.Features.Todos.Categories.Commands;

/// <summary>Unit tests for the create category slice.</summary>
public sealed class CreateTodoCategoryTest
{
    [Fact(DisplayName = "Create should persist a category for the ambient tenant")]
    public async Task Handle_WithValidRequest_ShouldPersist()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory? stored = null;
        fixture.CategoryRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TodoCategory>(), It.IsAny<CancellationToken>()))
            .Callback<TodoCategory, CancellationToken>((category, _) => stored = category)
            .ReturnsAsync(Result.Success());
        var handler = new CreateTodoCategory.UseCaseHandler(
            fixture.CategoryRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<CreateTodoCategory.Response> result = await handler.Handle(
            new CreateTodoCategory.UseCase(new CreateTodoCategory.Request
            {
                Name = "Errands",
            }),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        stored!.TenantId.Should().Be(TodoHandlerFixture.TenantId);
        result.Value.Name.Should().Be("Errands");
    }

    [Fact(DisplayName = "Create validator should reject a blank name")]
    public void Validate_WithBlankName_ShouldFail()
    {
        // Act
        var result = new CreateTodoCategory.UseCaseValidator().Validate(
            new CreateTodoCategory.UseCase(new CreateTodoCategory.Request
            {
                Name = " ",
            }));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
