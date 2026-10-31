using Application.Modules.Todos.Categories.Queries;
using Application.Unit.Tests.Modules.Todos.Fixtures;

using Core.Modules.Todos.Categories;

namespace Application.Unit.Tests.Modules.Todos.Categories.Queries;

/// <summary>Unit tests for the get category slice.</summary>
public sealed class GetTodoCategoryTest
{
    private static GetTodoCategory.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.CategoryRepository.Object, fixture.Tenant);

    [Fact(DisplayName = "Get should return the category for the ambient tenant")]
    public async Task Handle_WhenFound_ShouldReturnModel()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        fixture.RegisterCategory(category);

        // Act
        Result<GetTodoCategory.Response> result = await CreateHandler(fixture).Handle(
            new GetTodoCategory.UseCase(new GetTodoCategory.Request(category.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(category.Id);
    }

    [Fact(DisplayName = "Get should hide a soft-deleted category by default")]
    public async Task Handle_WhenDeleted_ShouldReturnNotFoundByDefault()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        category.IsDeleted = true;
        fixture.RegisterCategory(category);

        // Act
        Result<GetTodoCategory.Response> result = await CreateHandler(fixture).Handle(
            new GetTodoCategory.UseCase(new GetTodoCategory.Request(category.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Get should return a soft-deleted category when IncludeDeleted is set")]
    public async Task Handle_WhenDeletedAndIncluded_ShouldReturnModel()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        category.IsDeleted = true;
        fixture.RegisterCategory(category);

        // Act
        Result<GetTodoCategory.Response> result = await CreateHandler(fixture).Handle(
            new GetTodoCategory.UseCase(
                new GetTodoCategory.Request(category.Id, IncludeDeleted: true)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeleted.Should().BeTrue();
    }
}
