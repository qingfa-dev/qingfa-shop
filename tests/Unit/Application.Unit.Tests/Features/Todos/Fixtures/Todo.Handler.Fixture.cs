using Application.Common.Contexts;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Querying.Mapping;
using Application.Features.Todos.Abstractions;

using Core.Todos;
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

    /// <summary>Gets the todo repository mock.</summary>
    public Mock<IPersistentRepository<Todo, Guid>> TodoRepository { get; } = new();

    /// <summary>Gets the list repository mock.</summary>
    public Mock<IPersistentRepository<TodoList, Guid>> ListRepository { get; } = new();

    /// <summary>Gets the category repository mock.</summary>
    public Mock<IPersistentRepository<TodoCategory, Guid>> CategoryRepository { get; } = new();

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
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<Todo>>.Success(Maybe<Todo>.Empty()));

        TodoRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        TodoRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        TodoRepository
            .Setup(repository => repository.RemoveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

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

    /// <summary>Registers a todo as found by identifier.</summary>
    /// <param name="todo">The todo to return.</param>
    public void RegisterTodo(Todo todo) =>
        TodoRepository
            .Setup(repository => repository.FindByIdAsync(todo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Maybe<Todo>>.Success(Maybe<Todo>.From(todo)));

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
        TodoCategory.Create(Guid.NewGuid(), TenantId, name, Now, Actor).Value;

    /// <summary>Registers the minimal todo query map used by search tests.</summary>
    public void RegisterTodoMap() =>
        Registry.Register(QueryMapBuilder<Todo>.Create()
            .Map("tenantId", todo => todo.TenantId, field => field.Filterable())
            .Map("isDeleted", todo => todo.IsDeleted, field => field.Filterable())
            .Map("createdAtUtc", todo => todo.CreatedAtUtc, field => field.Filterable().Sortable())
            .Map("title", todo => todo.Title, field => field.Filterable().Searchable())
            .Build().Value);

    /// <summary>Registers the minimal list query map used by search tests.</summary>
    public void RegisterListMap() =>
        Registry.Register(QueryMapBuilder<TodoList>.Create()
            .Map("tenantId", list => list.TenantId, field => field.Filterable())
            .Map("isDeleted", list => list.IsDeleted, field => field.Filterable())
            .Map("createdAtUtc", list => list.CreatedAtUtc, field => field.Filterable().Sortable())
            .Map("name", list => list.Name, field => field.Filterable().Searchable())
            .Build().Value);

    /// <summary>Registers the minimal category query map used by search tests.</summary>
    public void RegisterCategoryMap() =>
        Registry.Register(QueryMapBuilder<TodoCategory>.Create()
            .Map("tenantId", category => category.TenantId, field => field.Filterable())
            .Map("isDeleted", category => category.IsDeleted, field => field.Filterable())
            .Map("createdAtUtc", category => category.CreatedAtUtc, field => field.Filterable().Sortable())
            .Map("name", category => category.Name, field => field.Filterable().Searchable())
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
/// <param name="actor">The initial actor.</param>
public sealed class TestActorContext(string? actor) : IActorContext
{
    /// <inheritdoc />
    public string? Actor { get; set; } = actor;
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
