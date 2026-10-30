using Application.Common.EventSourcing;
using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Items.Models;

using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Services.Todos;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.Todos;

/// <summary>
/// Unit tests for <see cref="TodoReferenceCleaner"/> against SQLite so the
/// tracking, tenant scoping, and category JSON conversion paths all run.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "EventSourcing")]
public sealed class TodoReferenceCleanerTest : IAsyncLifetime
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    private async Task<ApplicationDbContext> CreateSeededContextAsync(params Todo[] todos)
    {
        var context = new ApplicationDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        foreach (Todo todo in todos)
        {
            context.Set<TodoReadModel>().Add(new TodoReadModel
            {
                Id = todo.Id,
                TenantId = todo.TenantId,
                Title = todo.Title,
                Description = todo.Description,
                Status = todo.Status,
                Priority = todo.Priority,
                DueAtUtc = todo.DueAtUtc,
                CompletedAtUtc = todo.CompletedAtUtc,
                ListId = todo.ListId,
                CategoryIds = [.. todo.CategoryIds],
                CreatedAtUtc = todo.CreatedAtUtc,
                CreatedBy = todo.CreatedBy,
                ModifiedAtUtc = todo.ModifiedAtUtc,
                ModifiedBy = todo.ModifiedBy,
                IsDeleted = todo.IsDeleted,
                DeletedAtUtc = todo.DeletedAtUtc,
                DeletedBy = todo.DeletedBy,
                Version = todo.Version,
            });
        }

        await context.SaveChangesAsync();
        return context;
    }

    private static Todo CreateTodo(
        Guid tenantId,
        Guid? listId,
        bool isDeleted = false,
        params Guid[] categoryIds)
    {
        Todo todo = Todo.Create(
            Guid.NewGuid(),
            tenantId,
            "A todo",
            Now.AddDays(-1),
            actor: "creator",
            listId: listId).Value!;

        foreach (Guid categoryId in categoryIds)
        {
            todo.AddCategory(categoryId, Now.AddDays(-1), "creator").IsSuccess.Should().BeTrue();
        }

        if (isDeleted)
        {
            todo.Delete(isHard: false, Now.AddDays(-2), "creator").IsSuccess.Should().BeTrue();
        }

        todo.ClearUncommittedEvents();
        return todo;
    }

    private static Mock<IEventSourcedRepository<Todo, Guid>> CreateRepository(params Todo[] todos)
    {
        var repository = new Mock<IEventSourcedRepository<Todo, Guid>>();

        repository
            .Setup(store => store.LoadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) => Task.FromResult(
                todos.FirstOrDefault(todo => todo.Id == id) is { } found
                    ? Result<Todo>.Success(found)
                    : Result<Todo>.Fail(RepositoryOutcome.Failure.NotFoundFor<Todo>(id))));

        repository
            .Setup(store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        return repository;
    }

    [Fact(DisplayName = "ClearListAsync should clear only the matching tenant and list")]
    public async Task ClearListAsync_ShouldClearMatchingTodos()
    {
        // Arrange
        Guid listId = Guid.NewGuid();
        Guid otherListId = Guid.NewGuid();

        Todo matchingOne = CreateTodo(TenantId, listId);
        Todo matchingTwo = CreateTodo(TenantId, listId);
        Todo otherList = CreateTodo(TenantId, otherListId);
        Todo otherTenant = CreateTodo(OtherTenantId, listId);

        await using ApplicationDbContext context = await CreateSeededContextAsync(
            matchingOne, matchingTwo, otherList, otherTenant);
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(
            matchingOne, matchingTwo, otherList, otherTenant);
        var cleaner = new TodoReferenceCleaner(context, repository.Object);

        // Act
        Result<int> result = await cleaner.ClearListAsync(
            TenantId, listId, Now, "  editor  ", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        matchingOne.ListId.Should().BeNull();
        matchingTwo.ListId.Should().BeNull();
        otherList.ListId.Should().Be(otherListId);
        otherTenant.ListId.Should().Be(listId);

        matchingOne.ModifiedAtUtc.Should().Be(Now.ToUniversalTime());
        matchingOne.ModifiedBy.Should().Be("editor");
        otherList.ModifiedAtUtc.Should().BeNull();

        repository.Verify(
            store => store.SaveAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact(DisplayName = "ClearListAsync should clear soft-deleted todos so restore keeps no dangling reference")]
    public async Task ClearListAsync_ShouldClearSoftDeletedTodos()
    {
        // Arrange
        Guid listId = Guid.NewGuid();
        Todo live = CreateTodo(TenantId, listId);
        Todo deleted = CreateTodo(TenantId, listId, isDeleted: true);

        await using ApplicationDbContext context = await CreateSeededContextAsync(live, deleted);
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(live, deleted);
        var cleaner = new TodoReferenceCleaner(context, repository.Object);

        // Act
        Result<int> result = await cleaner.ClearListAsync(
            TenantId, listId, Now, "editor", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        live.ListId.Should().BeNull();
        deleted.ListId.Should().BeNull();
        deleted.IsDeleted.Should().BeTrue();
        deleted.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoListChanged>();
    }

    [Fact(DisplayName = "ClearListAsync should not abort on a hard-deleted stream with no projection row")]
    public async Task ClearListAsync_WhenHardDeletedRowIsGone_ShouldIgnoreTheStream()
    {
        // Arrange
        Guid listId = Guid.NewGuid();
        Todo live = CreateTodo(TenantId, listId);
        Todo hardDeleted = CreateTodo(TenantId, listId);
        hardDeleted.Delete(isHard: true, Now.AddDays(-2), "creator").IsSuccess.Should().BeTrue();
        hardDeleted.ClearUncommittedEvents();

        // Handle: the projection physically removes hard-deleted rows, so only the live row is seeded.
        await using ApplicationDbContext context = await CreateSeededContextAsync(live);
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(live, hardDeleted);
        var cleaner = new TodoReferenceCleaner(context, repository.Object);

        // Act
        Result<int> result = await cleaner.ClearListAsync(
            TenantId, listId, Now, "editor", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        live.ListId.Should().BeNull();
        hardDeleted.ListId.Should().Be(listId);
        hardDeleted.UncommittedEvents.Should().BeEmpty();
        repository.Verify(
            store => store.SaveAsync(live, It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            store => store.SaveAsync(hardDeleted, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "ClearListAsync should succeed when no todo references the list")]
    public async Task ClearListAsync_WhenNoMatch_ShouldReturnZero()
    {
        // Arrange
        await using ApplicationDbContext context = await CreateSeededContextAsync();
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository();
        var cleaner = new TodoReferenceCleaner(context, repository.Object);

        // Act
        Result<int> result = await cleaner.ClearListAsync(
            TenantId, Guid.NewGuid(), Now, actor: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact(DisplayName = "RemoveCategoryAsync should remove the category from matching todos only")]
    public async Task RemoveCategoryAsync_ShouldRemoveCategoryFromMatchingTodos()
    {
        // Arrange
        Guid categoryId = Guid.NewGuid();
        Guid otherCategoryId = Guid.NewGuid();

        Todo withCategory = CreateTodo(TenantId, null, categoryIds: [categoryId, otherCategoryId]);
        Todo withoutCategory = CreateTodo(TenantId, null, categoryIds: [otherCategoryId]);
        Todo otherTenant = CreateTodo(OtherTenantId, null, categoryIds: [categoryId]);

        await using ApplicationDbContext context = await CreateSeededContextAsync(
            withCategory, withoutCategory, otherTenant);
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(
            withCategory, withoutCategory, otherTenant);
        var cleaner = new TodoReferenceCleaner(context, repository.Object);
        DateTimeOffset? untouchedModifiedAt = withoutCategory.ModifiedAtUtc;

        // Act
        Result<int> result = await cleaner.RemoveCategoryAsync(
            TenantId, categoryId, Now, "editor", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        withCategory.CategoryIds.Should().BeEquivalentTo([otherCategoryId]);
        withoutCategory.CategoryIds.Should().BeEquivalentTo([otherCategoryId]);
        otherTenant.CategoryIds.Should().BeEquivalentTo([categoryId]);

        withCategory.ModifiedAtUtc.Should().Be(Now.ToUniversalTime());
        withCategory.ModifiedBy.Should().Be("editor");
        withoutCategory.ModifiedAtUtc.Should().Be(untouchedModifiedAt);
    }

    [Fact(DisplayName = "RemoveCategoryAsync should remove the category from soft-deleted todos")]
    public async Task RemoveCategoryAsync_ShouldRemoveCategoryFromSoftDeletedTodos()
    {
        // Arrange
        Guid categoryId = Guid.NewGuid();
        Todo deleted = CreateTodo(TenantId, null, isDeleted: true, categoryIds: [categoryId]);

        await using ApplicationDbContext context = await CreateSeededContextAsync(deleted);
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(deleted);
        var cleaner = new TodoReferenceCleaner(context, repository.Object);

        // Act
        Result<int> result = await cleaner.RemoveCategoryAsync(
            TenantId, categoryId, Now, "editor", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        deleted.CategoryIds.Should().BeEmpty();
        deleted.IsDeleted.Should().BeTrue();
        deleted.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCategoryRemoved>();
    }

    [Fact(DisplayName = "RemoveCategoryAsync should succeed when no todo uses the category")]
    public async Task RemoveCategoryAsync_WhenNoMatch_ShouldReturnZero()
    {
        // Arrange
        Todo todo = CreateTodo(TenantId, null, categoryIds: [Guid.NewGuid()]);
        await using ApplicationDbContext context = await CreateSeededContextAsync(todo);
        Mock<IEventSourcedRepository<Todo, Guid>> repository = CreateRepository(todo);
        var cleaner = new TodoReferenceCleaner(context, repository.Object);
        DateTimeOffset? untouchedModifiedAt = todo.ModifiedAtUtc;

        // Act
        Result<int> result = await cleaner.RemoveCategoryAsync(
            TenantId, Guid.NewGuid(), Now, actor: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        todo.ModifiedAtUtc.Should().Be(untouchedModifiedAt);
    }

    [Fact(DisplayName = "ClearListAsync should translate a provider failure")]
    public async Task ClearListAsync_WhenContextFails_ShouldReturnFailure()
    {
        // Arrange
        var context = new ApplicationDbContext(_options);
        await context.DisposeAsync();
        var cleaner = new TodoReferenceCleaner(context, CreateRepository().Object);

        // Act
        Result<int> result = await cleaner.ClearListAsync(
            TenantId, Guid.NewGuid(), Now, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("UnitOfWork.Operation.Invalid");
    }

    [Fact(DisplayName = "RemoveCategoryAsync should translate a provider failure")]
    public async Task RemoveCategoryAsync_WhenContextFails_ShouldReturnFailure()
    {
        // Arrange
        var context = new ApplicationDbContext(_options);
        await context.DisposeAsync();
        var cleaner = new TodoReferenceCleaner(context, CreateRepository().Object);

        // Act
        Result<int> result = await cleaner.RemoveCategoryAsync(
            TenantId, Guid.NewGuid(), Now, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("UnitOfWork.Operation.Invalid");
    }

    [Fact(DisplayName = "ClearListAsync should propagate cancellation")]
    public async Task ClearListAsync_WhenCancelled_ShouldPropagate()
    {
        // Arrange
        await using ApplicationDbContext context = await CreateSeededContextAsync();
        var cleaner = new TodoReferenceCleaner(context, CreateRepository().Object);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        Func<Task> act = () => cleaner.ClearListAsync(
            TenantId, Guid.NewGuid(), Now, null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
