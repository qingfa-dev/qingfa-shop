using Application.Common.EventSourcing;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Querying.Mapping;
using Application.Common.Security.Authentication.Contexts;
using Application.Features.Todos.Items.Models;
using Application.Features.Todos.Reminders.Models;
using Application.Features.Todos.Abstractions;

using Core.Todos.Categories;
using Core.Todos.Items;
using Core.Todos.Lists;

namespace Application.Unit.Tests.Features.Todos.Fixtures;

/// <summary>Shared mocks and clock for the Todos handler unit tests.</summary>
public sealed class TodoHandlerFixture
{
    /// <summary>The tenant every test operates in.</summary>
    public static readonly Guid TenantId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    /// <summary>The actor every test acts as.</summary>
    public const string Actor = "tester";

    /// <summary>The fixed instant used by the test clock.</summary>
    public static readonly DateTimeOffset Now = DateTimeOffset.UtcNow.Date;

    /// <summary>Gets the todo event-stream repository mock.</summary>
    public Mock<IEventSourcedRepository<Todo, Guid>> TodoRepository { get; } = new();

    /// <summary>Gets the todo read-model repository mock.</summary>
    public Mock<IPersistentRepository<TodoReadModel, Guid>> TodoReadRepository { get; } = new();

    /// <summary>Gets the todo read-model mapping repository mock.</summary>
    public Mock<IMappableRepository<TodoReadModel, Guid>> TodoMapperRepository { get; } = new();

    /// <summary>Gets the todo read-model projection repository mock.</summary>
    public Mock<IProjectionRepository<TodoReadModel, Guid>> TodoProjectionRepository { get; } = new();

    /// <summary>Gets the list repository mock.</summary>
    public Mock<IPersistentRepository<TodoList, Guid>> ListRepository { get; } = new();

    /// <summary>Gets the list mapping repository mock.</summary>
    public Mock<IMappableRepository<TodoList, Guid>> ListMapperRepository { get; } = new();

    /// <summary>Gets the list projection repository mock.</summary>
    public Mock<IProjectionRepository<TodoList, Guid>> ListProjectionRepository { get; } = new();

    /// <summary>Gets the category repository mock.</summary>
    public Mock<IPersistentRepository<TodoCategory, Guid>> CategoryRepository { get; } = new();

    /// <summary>Gets the category mapping repository mock.</summary>
    public Mock<IMappableRepository<TodoCategory, Guid>> CategoryMapperRepository { get; } = new();

    /// <summary>Gets the category projection repository mock.</summary>
    public Mock<IProjectionRepository<TodoCategory, Guid>> CategoryProjectionRepository { get; } = new();

    /// <summary>Gets the unit-of-work mock.</summary>
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();

    /// <summary>Gets the reference cleaner mock.</summary>
    public Mock<ITodoReferenceCleaner> Cleaner { get; } = new();

    /// <summary>Gets the query map registry used by search handlers.</summary>
    public QueryMapRegistry Registry { get; } = new();

    /// <summary>Gets the tenant context.</summary>
    public TestTenantContext Tenant { get; } = new(TenantId);

    /// <summary>Gets the actor context.</summary>
    public TestActorContext ActorContext { get; } = new(Actor);

    /// <summary>Gets the controllable clock.</summary>
    public TestClock Clock { get; } = new(Now);

    /// <summary>Initializes the default successful persistence behavior.</summary>
    public TodoHandlerFixture()
    {
        UnitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        TodoRepository
            .Setup(store => store.LoadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) =>
                Task.FromResult(Result<Todo>.Fail(RepositoryOutcome.Failure.NotFoundFor<Todo>(id))));

        TodoRepository
            .Setup(store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        ListRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<TodoList>>.Success(Maybe<TodoList>.Empty()));

        ListRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TodoList>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        ListRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<TodoList>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        ListRepository
            .Setup(repository => repository.RemoveAsync(It.IsAny<TodoList>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        CategoryRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<TodoCategory>>.Success(Maybe<TodoCategory>.Empty()));

        CategoryRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TodoCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        CategoryRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<TodoCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        CategoryRepository
            .Setup(repository => repository.RemoveAsync(It.IsAny<TodoCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        Cleaner
            .Setup(cleaner => cleaner.ClearListAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(0));

        Cleaner
            .Setup(cleaner => cleaner.RemoveCategoryAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(0));
    }

    /// <summary>Registers a todo stream as loadable by identifier.</summary>
    /// <param name="todo">The todo to return.</param>
    public void RegisterTodo(Todo todo) =>
        TodoRepository
            .Setup(store => store.LoadAsync(todo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Todo>.Success(todo));

    /// <summary>Registers a list as found by identifier.</summary>
    /// <param name="list">The list to return.</param>
    public void RegisterList(TodoList list) =>
        ListRepository
            .Setup(repository => repository.FindByIdAsync(list.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<TodoList>>.Success(Maybe<TodoList>.From(list)));

    /// <summary>Registers a category as found by identifier.</summary>
    /// <param name="category">The category to return.</param>
    public void RegisterCategory(TodoCategory category) =>
        CategoryRepository
            .Setup(repository => repository.FindByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<TodoCategory>>.Success(Maybe<TodoCategory>.From(category)));

    /// <summary>Creates a persisted todo owned by the test tenant.</summary>
    /// <param name="title">The optional title.</param>
    /// <returns>The new todo.</returns>
    public Todo CreateTodo(string title = "Write tests") =>
        Todo.Create(Guid.NewGuid(), TenantId, title, Now, Actor).Value;

    /// <summary>Creates a persisted list owned by the test tenant.</summary>
    /// <param name="name">The optional name.</param>
    /// <returns>The new list.</returns>
    public TodoList CreateList(string name = "Groceries") =>
        TodoList.Create(Guid.NewGuid(), TenantId, name, Now, Actor).Value;

    /// <summary>Creates a persisted category owned by the test tenant.</summary>
    /// <param name="name">The optional name.</param>
    /// <returns>The new category.</returns>
    public TodoCategory CreateCategory(string name = "Errands") =>
        TodoCategory.Create(TenantId, name, Now, Actor, id: Guid.NewGuid()).Value;

    /// <summary>Registers the todo read-model query map used by search tests.</summary>
    public void RegisterTodoReadModelMap() =>
        Registry.Register(QueryMapBuilder<TodoReadModel>.Create()
            .Map("id", todo => todo.Id, field => field.Filterable().Sortable().Groupable())
            .Map("tenantId", todo => todo.TenantId, field => field.Filterable().Sortable().Groupable())
            .Map("isDeleted", todo => todo.IsDeleted, field => field.Filterable())
            .Map("createdAtUtc", todo => todo.CreatedAtUtc, field => field.Filterable().Sortable())
            .Map("title", todo => todo.Title, field => field.Filterable().Searchable().Groupable())
            .Map("description", todo => todo.Description, field => field.Filterable().Searchable())
            .Map("status", todo => todo.Status, field => field.Filterable().Sortable().Groupable())
            .Map("priority", todo => todo.Priority, field => field.Filterable().Sortable().Groupable())
            .Map("dueAtUtc", todo => todo.DueAtUtc, field => field.Filterable().Sortable())
            .Map("listId", todo => todo.ListId, field => field.Filterable().Sortable().Groupable())
            .Map("isCompleted", todo => todo.IsCompleted, field => field.Filterable().Sortable().Groupable())
            .Build().Value);

    /// <summary>Registers the minimal list query map used by search tests.</summary>
    public void RegisterListMap() =>
        Registry.Register(QueryMapBuilder<TodoList>.Create()
            .Map("id", list => list.Id, field => field.Filterable().Sortable().Groupable())
            .Map("tenantId", list => list.TenantId, field => field.Filterable().Sortable().Groupable())
            .Map("isDeleted", list => list.IsDeleted, field => field.Filterable())
            .Map("createdAtUtc", list => list.CreatedAtUtc, field => field.Filterable().Sortable())
            .Map("name", list => list.Name, field => field.Filterable().Searchable().Groupable())
            .Build().Value);

    /// <summary>Registers the minimal reminder query map used by search tests.</summary>
    public void RegisterReminderMap() =>
        Registry.Register(QueryMapBuilder<TodoReminderModel>.Create()
            .Map("id", reminder => reminder.Id, field => field.Filterable().Sortable().Groupable())
            .Map("todoId", reminder => reminder.TodoId, field => field.Filterable().Sortable().Groupable())
            .Map("tenantId", reminder => reminder.TenantId, field => field.Filterable().Sortable().Groupable())
            .Map("remindAtUtc", reminder => reminder.RemindAtUtc, field => field.Filterable().Sortable())
            .Map("status", reminder => reminder.Status, field => field.Filterable().Sortable())
            .Map("snoozeCount", reminder => reminder.SnoozeCount, field => field.Filterable().Sortable())
            .Map("dismissedAtUtc", reminder => reminder.DismissedAtUtc, field => field.Filterable().Sortable())
            .Map("version", reminder => reminder.Version, field => field.Filterable().Sortable())
            .Build().Value);

    /// <summary>Registers the minimal category query map used by search tests.</summary>
    public void RegisterCategoryMap() =>
        Registry.Register(QueryMapBuilder<TodoCategory>.Create()
            .Map("id", category => category.Id, field => field.Filterable().Sortable().Groupable())
            .Map("tenantId", category => category.TenantId, field => field.Filterable().Sortable().Groupable())
            .Map("isDeleted", category => category.IsDeleted, field => field.Filterable())
            .Map("createdAtUtc", category => category.CreatedAtUtc, field => field.Filterable().Sortable())
            .Map("name", category => category.Name, field => field.Filterable().Searchable().Groupable())
            .Build().Value);
}

/// <summary>A tenant context that can be changed by a test.</summary>
/// <param name="tenantId">The initial tenant.</param>
public sealed class TestTenantContext(Guid tenantId) : ITenantContext
{
    /// <inheritdoc />
    public Guid TenantId { get; set; } = tenantId;
}

/// <summary>An actor context that can be changed by a test.</summary>
/// <param name="actor">The initial actor identifier used as UserName.</param>
public sealed class TestActorContext(string? actor) : IUserContext
{
    /// <inheritdoc />
    public string? UserId { get; set; }

    /// <inheritdoc />
    public string? UserName { get; set; } = actor;

    /// <inheritdoc />
    public string? Email { get; set; }

    /// <inheritdoc />
    public bool IsAuthenticated { get; set; } = true;

    /// <inheritdoc />
    public string? IpAddress { get; set; }

    /// <inheritdoc />
    public string? Device { get; set; }
}

/// <summary>A clock frozen at a controllable instant.</summary>
/// <param name="utcNow">The initial instant.</param>
public sealed class TestClock(DateTimeOffset utcNow) : TimeProvider
{
    /// <summary>Gets or sets the instant returned by the clock.</summary>
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => UtcNow;
}
