using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Items.Models;
using Application.Features.Todos.Items.Queries;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Queries;

/// <summary>Unit tests for the get todo slice.</summary>
public sealed class GetTodoTest
{
    private static GetTodo.UseCaseHandler CreateHandler(TodoHandlerFixture fixture) =>
        new(fixture.TodoReadRepository.Object, fixture.Tenant);

    private static TodoReadModel CreateReadModel(
        Guid? tenantId = null,
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? TodoHandlerFixture.TenantId,
            Title = "Read me",
            Status = TodoStatus.Pending,
            Priority = TodoPriority.Medium,
            CreatedAtUtc = TodoHandlerFixture.Now,
            IsDeleted = isDeleted,
        };

    private static void Register(
        TodoHandlerFixture fixture,
        TodoReadModel model) =>
        fixture.TodoReadRepository
            .Setup(repository => repository.FindByIdAsync(
                model.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<TodoReadModel>>.Success(Maybe<TodoReadModel>.From(model)));

    [Fact(DisplayName = "Get should return the todo for the ambient tenant")]
    public async Task Handle_WhenFound_ShouldReturnModel()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoReadModel model = CreateReadModel();
        Register(fixture, model);

        // Act
        Result<GetTodo.Response> result = await CreateHandler(fixture).Handle(
            new GetTodo.UseCase(new GetTodo.Request(model.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(model.Id);
        result.Value.Title.Should().Be("Read me");
    }

    [Fact(DisplayName = "Get should hide a soft-deleted todo by default")]
    public async Task Handle_WhenDeleted_ShouldReturnNotFoundByDefault()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoReadModel model = CreateReadModel(isDeleted: true);
        Register(fixture, model);

        // Act
        Result<GetTodo.Response> result = await CreateHandler(fixture).Handle(
            new GetTodo.UseCase(new GetTodo.Request(model.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == RepositoryOutcome.Codes.EntityNotFound);
    }

    [Fact(DisplayName = "Get should return a soft-deleted todo when IncludeDeleted is set")]
    public async Task Handle_WhenDeletedAndIncluded_ShouldReturnModel()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoReadModel model = CreateReadModel(isDeleted: true);
        Register(fixture, model);

        // Act
        Result<GetTodo.Response> result = await CreateHandler(fixture).Handle(
            new GetTodo.UseCase(new GetTodo.Request(model.Id, IncludeDeleted: true)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "Get should not expose another tenant's todo")]
    public async Task Handle_WithForeignTenant_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        TodoReadModel model = CreateReadModel(tenantId: Guid.NewGuid());
        Register(fixture, model);

        // Act
        Result<GetTodo.Response> result = await CreateHandler(fixture).Handle(
            new GetTodo.UseCase(new GetTodo.Request(model.Id)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == RepositoryOutcome.Codes.EntityNotFound);
    }
}
