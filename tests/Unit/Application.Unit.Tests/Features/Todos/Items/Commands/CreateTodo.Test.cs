using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Unit tests for the create todo slice.</summary>
public sealed class CreateTodoTest
{
    private static async Task<Result<CreateTodo.Response>> CreateAsync(
        TodoHandlerFixture fixture,
        CreateTodo.Request request)
    {
        var handler = new CreateTodo.UseCaseHandler(
            fixture.TodoRepository.Object,
            fixture.UnitOfWork.Object,
            fixture.Tenant,
            fixture.ActorContext,
            fixture.Clock);

        return await handler.Handle(
            new CreateTodo.UseCase(request),
            CancellationToken.None);
    }

    [Fact(DisplayName = "Create should persist a pending todo with audit metadata")]
    public async Task Handle_WithValidRequest_ShouldPersistTodo()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo? stored = null;
        fixture.TodoRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => stored = todo)
            .ReturnsAsync(Result.Success());

        // Act
        Result<CreateTodo.Response> result = await CreateAsync(
            fixture,
            new CreateTodo.Request
            {
                Title = "Ship the feature",
                Description = "With tests",
                Priority = TodoPriority.High,
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        stored.Should().NotBeNull();
        stored!.TenantId.Should().Be(TodoHandlerFixture.TenantId);
        stored.Status.Should().Be(TodoStatus.Pending);
        stored.Title.Should().Be("Ship the feature");
        result.Value.Title.Should().Be("Ship the feature");
        fixture.UnitOfWork.Verify(
            unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Create should fail when the tenant is missing")]
    public async Task Handle_WithoutTenant_ShouldFail()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        fixture.Tenant.TenantId = Guid.Empty;

        // Act
        Result<CreateTodo.Response> result = await CreateAsync(
            fixture,
            new CreateTodo.Request { Title = "Task" });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Tenantable.TenantId.Required");
        fixture.TodoRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Create should add every requested category")]
    public async Task Handle_WithCategoryIds_ShouldAddCategoryReferences()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Guid categoryId = Guid.NewGuid();
        Todo? stored = null;
        fixture.TodoRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => stored = todo)
            .ReturnsAsync(Result.Success());

        // Act
        Result<CreateTodo.Response> result = await CreateAsync(
            fixture,
            new CreateTodo.Request
            {
                Title = "Task",
                CategoryIds = [categoryId],
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        stored!.CategoryIds.Should().Contain(categoryId);
    }

    [Fact(DisplayName = "Create validator should reject a blank title")]
    public void Validate_WithBlankTitle_ShouldFail()
    {
        // Act
        var result = new CreateTodo.UseCaseValidator().Validate(
            new CreateTodo.UseCase(new CreateTodo.Request
            {
                Title = " ",
            }));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
