using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Commands;
using Application.Features.Todos.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Contract.Common;
using Contract.Todos.Models;

using Core.Todos.Categories;

namespace Application.Unit.Tests.Features.Todos;

/// <summary>Unit tests for the todo category handlers.</summary>
public sealed class TodoCategoryHandlerTest
{
    [Fact(DisplayName = "Create should persist a category for the ambient tenant")]
    public async Task Create_ShouldPersist()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory? stored = null;
        fixture.CategoryRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TodoCategory>(), It.IsAny<CancellationToken>()))
            .Callback<TodoCategory, CancellationToken>((category, _) => stored = category)
            .ReturnsAsync(Result.Success());
        var handler = new CreateTodoCategoryHandler(
            fixture.CategoryRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoCategoryModel> result = await handler.Handle(
            new CreateTodoCategoryCommand("Errands"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        stored!.TenantId.Should().Be(TodoHandlerFixture.TenantId);
        result.Value.Name.Should().Be("Errands");
    }

    [Fact(DisplayName = "Update should rename a category")]
    public async Task Update_ShouldRename()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory("old");
        fixture.RegisterCategory(category);
        var handler = new UpdateTodoCategoryHandler(
            fixture.CategoryRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoCategoryModel> result = await handler.Handle(
            new UpdateTodoCategoryCommand(category.Id, Name: "new"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("new");
        result.Value.ModifiedAtUtc.Should().Be(TodoHandlerFixture.Now);
    }

    [Fact(DisplayName = "Soft delete should remove references and mark the category deleted")]
    public async Task Delete_Soft_ShouldCleanAndMarkDeleted()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        fixture.RegisterCategory(category);
        var handler = new DeleteTodoCategoryHandler(
            fixture.CategoryRepository.Object,
            fixture.Cleaner.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result result = await handler.Handle(
            new DeleteTodoCategoryCommand(category.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
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
    public async Task Delete_Hard_ShouldCleanAndRemove()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        fixture.RegisterCategory(category);
        var handler = new DeleteTodoCategoryHandler(
            fixture.CategoryRepository.Object,
            fixture.Cleaner.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result result = await handler.Handle(
            new DeleteTodoCategoryCommand(category.Id, DeleteMode.Hard),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.CategoryRepository.Verify(
            repository => repository.RemoveAsync(category, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Restore should clear the deletion state")]
    public async Task Restore_ShouldClearDeletion()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        category.IsDeleted = true;
        fixture.RegisterCategory(category);
        var handler = new RestoreTodoCategoryHandler(
            fixture.CategoryRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<TodoCategoryModel> result = await handler.Handle(
            new RestoreTodoCategoryCommand(category.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeleted.Should().BeFalse();
    }

    [Fact(DisplayName = "Get should return the category for the ambient tenant")]
    public async Task Get_ShouldReturnCategory()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoCategory category = fixture.CreateCategory();
        fixture.RegisterCategory(category);
        var handler = new GetTodoCategoryHandler(fixture.CategoryRepository.Object, fixture.Tenant);

        // Act
        Result<TodoCategoryModel> result = await handler.Handle(
            new GetTodoCategoryQuery(category.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(category.Id);
    }

    [Fact(DisplayName = "Search should map matching categories into a page")]
    public async Task Search_ShouldMapPage()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.RegisterCategoryMap();
        TodoCategory category = fixture.CreateCategory();
        fixture.CategoryRepository
            .Setup(repository => repository.QueryAsync(
                It.IsAny<Query<TodoCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QueryPage<TodoCategory>>.Success(
                new QueryPage<TodoCategory>([category], 1, 10, 1)));
        var handler = new SearchTodoCategoriesHandler(
            fixture.CategoryRepository.Object,
            fixture.Registry,
            fixture.Tenant);

        // Act
        Result<PageModel<TodoCategoryModel>> result = await handler.Handle(
            new SearchTodoCategoriesQuery(new QueryParameters { Page = 1, PageSize = 10 }),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(model => model.Id == category.Id);
    }
}
