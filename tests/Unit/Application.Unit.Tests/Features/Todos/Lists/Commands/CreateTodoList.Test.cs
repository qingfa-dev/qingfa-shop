using Application.Features.Todos.Lists.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Lists;

namespace Application.Unit.Tests.Features.Todos.Lists.Commands;

/// <summary>Unit tests for the create list slice.</summary>
public sealed class CreateTodoListTest
{
    [Fact(DisplayName = "Create should persist a list for the ambient tenant")]
    public async Task Handle_WithValidRequest_ShouldPersist()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList? stored = null;
        fixture.ListRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TodoList>(), It.IsAny<CancellationToken>()))
            .Callback<TodoList, CancellationToken>((list, _) => stored = list)
            .ReturnsAsync(Result.Success());
        var handler = new CreateTodoList.UseCaseHandler(
            fixture.ListRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        // Act
        Result<CreateTodoList.Response> result = await handler.Handle(
            new CreateTodoList.UseCase(new CreateTodoList.Request
            {
                Name = "Groceries",
            }),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        stored!.TenantId.Should().Be(TodoHandlerFixture.TenantId);
        result.Value.Name.Should().Be("Groceries");
    }

    [Fact(DisplayName = "Create validator should reject a blank name")]
    public void Validate_WithBlankName_ShouldFail()
    {
        // Act
        var result = new CreateTodoList.UseCaseValidator().Validate(
            new CreateTodoList.UseCase(new CreateTodoList.Request
            {
                Name = " ",
            }));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
