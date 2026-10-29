using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Lists.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Lists;

namespace Application.Unit.Tests.Features.Todos.Lists.Commands;

/// <summary>Unit tests for the delete list slice.</summary>
public sealed class DeleteTodoListTest
{
    private static DeleteTodoList.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(
            fixture.ListRepository.Object,
            fixture.Cleaner.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

    [Fact(DisplayName = "Soft delete should clear references and mark the list deleted")]
    public async Task Handle_Soft_ShouldCleanAndMarkDeleted()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        fixture.RegisterList(list);

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodoList.UseCase(new DeleteTodoList.Request(list.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        list.IsDeleted.Should().BeTrue();
        fixture.ListRepository.Verify(
            repository => repository.UpdateAsync(list, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Cleaner.Verify(
            cleaner => cleaner.ClearListAsync(
                TodoHandlerFixture.TenantId,
                list.Id,
                TodoHandlerFixture.Now,
                TodoHandlerFixture.Actor,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Hard delete should clear references and remove the list")]
    public async Task Handle_Hard_ShouldCleanAndRemove()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        fixture.RegisterList(list);

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new DeleteTodoList.UseCase(
                new DeleteTodoList.Request(list.Id, DeleteMode.Hard)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.ListRepository.Verify(
            repository => repository.RemoveAsync(list, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Delete validator should reject a missing id")]
    public void Validate_WithEmptyId_ShouldFail()
    {
        // Act
        var result = new DeleteTodoList.UseCaseValidator().Validate(
            new DeleteTodoList.UseCase(new DeleteTodoList.Request(Guid.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
