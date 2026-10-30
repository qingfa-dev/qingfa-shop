using Application.Common.EventSourcing;
using Application.Features.Todos.Items.Commands;
using Application.Unit.Tests.Features.Todos.Fixtures;

using Core.Todos.Items;

namespace Application.Unit.Tests.Features.Todos.Items.Commands;

/// <summary>Event-sourced category and list command tests for the todo aggregate.</summary>
[Trait("Category", "Unit")]
public sealed class EventSourcedTodoCategoriesTest
{
    private static Mock<IEventSourcedRepository<Todo, Guid>> CreateRepository(Todo todo)
    {
        var repository = new Mock<IEventSourcedRepository<Todo, Guid>>();
        repository
            .Setup(store => store.LoadAsync(todo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Todo>.Success(todo));
        repository
            .Setup(store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        return repository;
    }

    [Fact(DisplayName = "AddCategory should save one TodoCategoryAdded")]
    public async Task AddCategory_ShouldSaveOneCategoryAdded()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.ClearUncommittedEvents();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);
        Guid categoryId = Guid.NewGuid();
        var handler = new AddTodoCategory.UseCaseHandler(
            repository.Object, fixture.Tenant, fixture.ActorContext, fixture.Clock);

        // Act
        Result<AddTodoCategory.Response> result = await handler.Handle(
            new AddTodoCategory.UseCase(new AddTodoCategory.Request(todo.Id, categoryId)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryIds.Should().Contain(categoryId);
        todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCategoryAdded>();
        repository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "RemoveCategory should save one TodoCategoryRemoved")]
    public async Task RemoveCategory_ShouldSaveOneCategoryRemoved()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        Guid categoryId = Guid.NewGuid();
        todo.AddCategory(categoryId, TodoHandlerFixture.Now, TodoHandlerFixture.Actor)
            .IsSuccess.Should().BeTrue();
        todo.ClearUncommittedEvents();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);
        var handler = new RemoveTodoCategory.UseCaseHandler(
            repository.Object, fixture.Tenant, fixture.ActorContext, fixture.Clock);

        // Act
        Result<RemoveTodoCategory.Response> result = await handler.Handle(
            new RemoveTodoCategory.UseCase(new RemoveTodoCategory.Request(todo.Id, categoryId)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryIds.Should().NotContain(categoryId);
        todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCategoryRemoved>();
        repository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "AssignToList should save one TodoListChanged")]
    public async Task AssignToList_ShouldSaveOneListChanged()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.ClearUncommittedEvents();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);
        Guid listId = Guid.NewGuid();
        var handler = new AssignTodoToList.UseCaseHandler(
            repository.Object, fixture.Tenant, fixture.ActorContext, fixture.Clock);

        // Act
        Result<AssignTodoToList.Response> result = await handler.Handle(
            new AssignTodoToList.UseCase(new AssignTodoToList.Request(todo.Id, listId)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ListId.Should().Be(listId);
        TodoListChanged changed = todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoListChanged>().Subject;
        changed.ListId.Should().Be(listId);
        repository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "ClearList should save one TodoListChanged with a null list")]
    public async Task ClearList_ShouldSaveOneListChanged()
    {
        // Arrange
        var fixture = new TodoHandlerFixture();
        Todo todo = fixture.CreateTodo();
        todo.AssignToList(Guid.NewGuid(), TodoHandlerFixture.Now, TodoHandlerFixture.Actor)
            .IsSuccess.Should().BeTrue();
        todo.ClearUncommittedEvents();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);
        var handler = new ClearTodoList.UseCaseHandler(
            repository.Object, fixture.Tenant, fixture.ActorContext, fixture.Clock);

        // Act
        Result<ClearTodoList.Response> result = await handler.Handle(
            new ClearTodoList.UseCase(new ClearTodoList.Request(todo.Id)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ListId.Should().BeNull();
        TodoListChanged changed = todo.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoListChanged>().Subject;
        changed.ListId.Should().BeNull();
        repository.Verify(
            store => store.SaveAsync(todo, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
