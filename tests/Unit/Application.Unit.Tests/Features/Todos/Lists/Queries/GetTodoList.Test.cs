using Application.Features.Todos.Lists.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Lists;

namespace Application.Unit.Tests.Features.Todos.Lists.Queries;

/// <summary>Unit tests for the get list slice.</summary>
public sealed class GetTodoListTest
{
    private static GetTodoList.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.ListRepository.Object, fixture.Tenant);

    [Fact(DisplayName = "Get should return the list for the ambient tenant")]
    public async Task Handle_WhenFound_ShouldReturnModel()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        fixture.RegisterList(list);

        // Act
        Result<GetTodoList.Response> result = await CreateHandler(fixture).Handle(
            new GetTodoList.UseCase(new GetTodoList.Request(list.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(list.Id);
    }

    [Fact(DisplayName = "Get should hide a soft-deleted list by default")]
    public async Task Handle_WhenDeleted_ShouldReturnNotFoundByDefault()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        list.IsDeleted = true;
        fixture.RegisterList(list);

        // Act
        Result<GetTodoList.Response> result = await CreateHandler(fixture).Handle(
            new GetTodoList.UseCase(new GetTodoList.Request(list.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Get should return a soft-deleted list when IncludeDeleted is set")]
    public async Task Handle_WhenDeletedAndIncluded_ShouldReturnModel()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoList list = fixture.CreateList();
        list.IsDeleted = true;
        fixture.RegisterList(list);

        // Act
        Result<GetTodoList.Response> result = await CreateHandler(fixture).Handle(
            new GetTodoList.UseCase(
                new GetTodoList.Request(list.Id, IncludeDeleted: true)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeleted.Should().BeTrue();
    }
}
