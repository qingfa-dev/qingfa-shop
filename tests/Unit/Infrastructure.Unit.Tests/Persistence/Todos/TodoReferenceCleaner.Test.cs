using Core.Todos.Items;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Services.Todos;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.Todos;

/// <summary>
/// Unit tests for <see cref="TodoReferenceCleaner"/> against SQLite so the
/// tracking, tenant scoping, and category JSON conversion paths all run.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class TodoReferenceCleanerTest : IAsyncLifetime
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private string _databasePath = string.Empty;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        _databasePath = $"Data Source=TodoRefCleaner_{Guid.NewGuid():N}.db";
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_databasePath)
            .Options;
        return default;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        await context.Database.EnsureDeletedAsync();
        File.Delete(_databasePath.Replace("Data Source=", string.Empty, StringComparison.Ordinal));
    }

    private async Task<ApplicationDbContext> CreateSeededContextAsync()
    {
        var context = new ApplicationDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static Todo CreateTodo(Guid tenantId, Guid? listId, params Guid[] categoryIds)
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
            todo.CategoryIds.Add(categoryId);
        }

        return todo;
    }

    [Fact(DisplayName = "ClearListAsync should clear only the matching tenant and list")]
    public async Task ClearListAsync_ShouldClearMatchingTodos()
    {
        // Arrange
        await using var context = await CreateSeededContextAsync();
        Guid listId = Guid.NewGuid();
        Guid otherListId = Guid.NewGuid();

        Todo matchingOne = CreateTodo(TenantId, listId);
        Todo matchingTwo = CreateTodo(TenantId, listId);
        Todo otherList = CreateTodo(TenantId, otherListId);
        Todo otherTenant = CreateTodo(OtherTenantId, listId);

        context.Set<Todo>().AddRange(matchingOne, matchingTwo, otherList, otherTenant);
        await context.SaveChangesAsync();

        var cleaner = new TodoReferenceCleaner(context);

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
    }

    [Fact(DisplayName = "ClearListAsync should succeed when no todo references the list")]
    public async Task ClearListAsync_WhenNoMatch_ShouldReturnZero()
    {
        // Arrange
        await using var context = await CreateSeededContextAsync();
        var cleaner = new TodoReferenceCleaner(context);

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
        await using var context = await CreateSeededContextAsync();
        Guid categoryId = Guid.NewGuid();
        Guid otherCategoryId = Guid.NewGuid();

        Todo withCategory = CreateTodo(TenantId, null, categoryId, otherCategoryId);
        Todo withoutCategory = CreateTodo(TenantId, null, otherCategoryId);
        Todo otherTenant = CreateTodo(OtherTenantId, null, categoryId);

        context.Set<Todo>().AddRange(withCategory, withoutCategory, otherTenant);
        await context.SaveChangesAsync();

        var cleaner = new TodoReferenceCleaner(context);

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
        withoutCategory.ModifiedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "RemoveCategoryAsync should succeed when no todo uses the category")]
    public async Task RemoveCategoryAsync_WhenNoMatch_ShouldReturnZero()
    {
        // Arrange
        await using var context = await CreateSeededContextAsync();
        Todo todo = CreateTodo(TenantId, null, Guid.NewGuid());
        context.Set<Todo>().Add(todo);
        await context.SaveChangesAsync();

        var cleaner = new TodoReferenceCleaner(context);

        // Act
        Result<int> result = await cleaner.RemoveCategoryAsync(
            TenantId, Guid.NewGuid(), Now, actor: null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        todo.ModifiedAtUtc.Should().BeNull();
    }

    [Fact(DisplayName = "ClearListAsync should translate a provider failure")]
    public async Task ClearListAsync_WhenContextFails_ShouldReturnFailure()
    {
        // Arrange
        var context = new ApplicationDbContext(_options);
        await context.DisposeAsync();
        var cleaner = new TodoReferenceCleaner(context);

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
        var cleaner = new TodoReferenceCleaner(context);

        // Act
        Result<int> result = await cleaner.RemoveCategoryAsync(
            TenantId, Guid.NewGuid(), Now, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("UnitOfWork.Operation.Invalid");
    }

    [Fact(DisplayName = "ClearListAsync should observe cancellation")]
    public async Task ClearListAsync_WhenCancelled_ShouldReturnFailure()
    {
        // Arrange
        await using var context = await CreateSeededContextAsync();
        var cleaner = new TodoReferenceCleaner(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<int> result = await cleaner.ClearListAsync(
            TenantId, Guid.NewGuid(), Now, null, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("UnitOfWork.Operation.Invalid");
    }
}
