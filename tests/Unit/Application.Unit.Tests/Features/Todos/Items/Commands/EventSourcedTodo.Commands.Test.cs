using Application.Common.EventSourcing;
using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Event-sourced item command tests shared across the todo write slices.</summary>
[Trait("Category", "Unit")]
public sealed class EventSourcedTodoCommandsTest
{
    private static Mock<IEventSourcedRepository<Todo, Guid>> CreateRepository(
        Todo? todo = null)
    {
        var repository = new Mock<IEventSourcedRepository<Todo, Guid>>();

        repository
            .Setup(store => store.LoadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) => Task.FromResult(todo is not null && todo.Id == id
                ? Result<Todo>.Success(todo)
                : Result<Todo>.Fail(RepositoryOutcome.Failure.NotFoundFor<Todo>(id))));

        repository
            .Setup(store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        return repository;
    }

    private static CreateTodo.UseCaseHandler CreateCreateHandler(
        TodoHandlerFixture fixture,
        IEventSourcedRepository<Todo, Guid> repository) =>
        new(repository, fixture.Tenant, fixture.ActorContext, fixture.Clock);

    private static UpdateTodo.UseCaseHandler CreateUpdateHandler(
        TodoHandlerFixture fixture,
        IEventSourcedRepository<Todo, Guid> repository) =>
        new(repository, fixture.Tenant, fixture.ActorContext, fixture.Clock);

    private static StartTodo.UseCaseHandler CreateStartHandler(
        TodoHandlerFixture fixture,
        IEventSourcedRepository<Todo, Guid> repository) =>
        new(repository, fixture.Tenant, fixture.ActorContext, fixture.Clock);

    private static CompleteTodo.UseCaseHandler CreateCompleteHandler(
        TodoHandlerFixture fixture,
        IEventSourcedRepository<Todo, Guid> repository) =>
        new(repository, fixture.Tenant, fixture.ActorContext, fixture.Clock);

    private static ReopenTodo.UseCaseHandler CreateReopenHandler(
        TodoHandlerFixture fixture,
        IEventSourcedRepository<Todo, Guid> repository) =>
        new(repository, fixture.Tenant, fixture.ActorContext, fixture.Clock);

    private static DeleteTodo.UseCaseHandler CreateDeleteHandler(
        TodoHandlerFixture fixture,
        IEventSourcedRepository<Todo, Guid> repository) =>
        new(repository, fixture.Tenant, fixture.ActorContext, fixture.Clock);

    private static RestoreTodo.UseCaseHandler CreateRestoreHandler(
        TodoHandlerFixture fixture,
        IEventSourcedRepository<Todo, Guid> repository) =>
        new(repository, fixture.Tenant, fixture.ActorContext, fixture.Clock);

    [Fact(DisplayName = "Create should save one aggregate carrying one TodoCreated")]
    public async Task Create_ShouldSaveOneAggregateWithOneTodoCreated()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository();
        Todo? saved = null;
        repository
            .Setup(store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => saved = todo)
            .ReturnsAsync(Result.Ok());

        // Act
        Result<CreateTodo.Response> result = await CreateCreateHandler(fixture, repository.Object).Handle(
            new CreateTodo.UseCase(new CreateTodo.Request { Title = "New task" }),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("New task");
        saved.Should().NotBeNull();
        saved!.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCreated>();
        repository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Create should reject a blank title before saving")]
    public async Task Create_WithBlankTitle_ShouldNotSave()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository();

        // Act
        Result<CreateTodo.Response> result = await CreateCreateHandler(fixture, repository.Object).Handle(
            new CreateTodo.UseCase(new CreateTodo.Request { Title = " " }),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        repository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Update should raise exactly one TodoUpdated and map the final values")]
    public async Task Update_ShouldRaiseOneTodoUpdated()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo("old");
        todo.ClearUncommittedEvents();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);

        // Act
        Result<UpdateTodo.Response> result = await CreateUpdateHandler(fixture, repository.Object).Handle(
            new UpdateTodo.UseCase(new UpdateTodo.Request
            {
                Id = todo.Id,
                Title = "new",
                Priority = TodoPriority.High,
            }),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("new");
        result.Value.Priority.Should().Be(TodoPriority.High);
        todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoUpdated>();
        repository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Start should save one TodoStatusChanged")]
    public async Task Start_ShouldSaveOneStatusChanged()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.ClearUncommittedEvents();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);

        // Act
        Result<StartTodo.Response> result = await CreateStartHandler(fixture, repository.Object).Handle(
            new StartTodo.UseCase(new StartTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        TodoStatusChanged changed = todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoStatusChanged>().Subject;
        changed.To.Should().Be(TodoStatus.InProgress);
        repository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Complete should save one TodoStatusChanged")]
    public async Task Complete_ShouldSaveOneStatusChanged()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Start(TodoHandlerFixture.Now, TodoHandlerFixture.Actor).IsSuccess.Should().BeTrue();
        todo.ClearUncommittedEvents();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);

        // Act
        Result<CompleteTodo.Response> result = await CreateCompleteHandler(fixture, repository.Object).Handle(
            new CompleteTodo.UseCase(new CompleteTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        TodoStatusChanged changed = todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoStatusChanged>().Subject;
        changed.To.Should().Be(TodoStatus.Completed);
        repository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Reopen should save one TodoStatusChanged")]
    public async Task Reopen_ShouldSaveOneStatusChanged()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Start(TodoHandlerFixture.Now, TodoHandlerFixture.Actor).IsSuccess.Should().BeTrue();
        todo.Complete(TodoHandlerFixture.Now, TodoHandlerFixture.Actor).IsSuccess.Should().BeTrue();
        todo.ClearUncommittedEvents();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);

        // Act
        Result<ReopenTodo.Response> result = await CreateReopenHandler(fixture, repository.Object).Handle(
            new ReopenTodo.UseCase(new ReopenTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        TodoStatusChanged changed = todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoStatusChanged>().Subject;
        changed.To.Should().Be(TodoStatus.Pending);
        repository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Hard delete should pass isHard true to the delete event")]
    public async Task Delete_Hard_ShouldRaiseHardDeleteEvent()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);

        // Act
        Result result = await CreateDeleteHandler(fixture, repository.Object).Handle(
            new DeleteTodo.UseCase(new DeleteTodo.Request(todo.Id, DeleteMode.Hard)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        TodoDeleted deleted = todo.UncommittedEvents.OfType<TodoDeleted>().Should().ContainSingle().Subject;
        deleted.IsHard.Should().BeTrue();
        repository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Restore should raise TodoRestored")]
    public async Task Restore_ShouldRaiseRestored()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.Delete(isHard: false, TodoHandlerFixture.Now, TodoHandlerFixture.Actor)
            .IsSuccess.Should().BeTrue();
        todo.ClearUncommittedEvents();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);

        // Act
        Result<RestoreTodo.Response> result = await CreateRestoreHandler(fixture, repository.Object).Handle(
            new RestoreTodo.UseCase(new RestoreTodo.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoRestored>();
        repository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "A missing stream should return Repository.Entity.NotFound")]
    public async Task Load_WhenStreamMissing_ShouldReturnNotFound()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository();
        Guid id = Guid.NewGuid();

        // Act
        Result<UpdateTodo.Response> result = await CreateUpdateHandler(fixture, repository.Object).Handle(
            new UpdateTodo.UseCase(new UpdateTodo.Request { Id = id, Title = "task" }),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == RepositoryOutcome.Codes.EntityNotFound);
        repository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "A stream owned by another tenant should return NotFound and never save")]
    public async Task Load_WithForeignTenant_ShouldReturnNotFoundAndNotSave()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo foreign = Todo.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "foreign",
            TodoHandlerFixture.Now,
            TodoHandlerFixture.Actor).Value;
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(foreign);

        // Act
        Result<UpdateTodo.Response> result = await CreateUpdateHandler(fixture, repository.Object).Handle(
            new UpdateTodo.UseCase(new UpdateTodo.Request { Id = foreign.Id, Title = "hijack" }),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == RepositoryOutcome.Codes.EntityNotFound);
        repository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
