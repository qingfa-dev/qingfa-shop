using Infrastructure.Persistence;
using System.Text.Json;

using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Seeders;
using Application.Common.Persistence.UnitOfWorks;

using Core.Common.Monad.Results;
using Core.Todos;

using Infrastructure.Persistence.Initialization;
using Infrastructure.Persistence.Todos;
using Infrastructure.Persistence.Todos.Seeders;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integration.Tests.Persistence.Initialization;

/// <summary>
/// Integration tests for the Todo seeders against the real migrated PostgreSQL schema.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Initialization.Seeders.PostgreSQL")]
[Collection(PersistenceInitializationCollection.Name)]
public sealed class TodoSeederIntegrationTest(PersistenceInitializationFixture fixture) : IAsyncLifetime
{
    private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly PersistenceInitializationFixture _fixture = fixture;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _fixture.DropDatabaseAsync();
        await _fixture.MigrateAsync();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact(DisplayName = "Todos seeders should insert categories, lists, and todos")]
    public async Task Seeders_OnEmptyDatabase_ShouldInsertExpectedRows()
    {
        // Act
        var results = await RunSeedersAsync();

        // Assert
        results.Should().OnlyContain(r => r.IsSuccess);
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    [Fact(DisplayName = "Todos seeders should be idempotent")]
    public async Task Seeders_RunTwice_ShouldNotDuplicateRows()
    {
        // Arrange
        await RunSeedersAsync();

        // Act
        await RunSeedersAsync();

        // Assert
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    [Fact(DisplayName = "TodoSeeder should persist category_ids as a JSON array readable by EF")]
    public async Task TodoSeeder_ShouldPersistCategoryIdsAsJsonArray()
    {
        // Arrange
        await RunSeedersAsync();

        // Act
        var categoryIds = await _fixture.WithDbContextAsync(async context =>
        {
            var todos = await context.Set<Todo>().AsNoTracking()
                .Where(t => t.ListId != null)
                .ToListAsync();

            return todos.SelectMany(t => t.CategoryIds ?? []).ToList();
        });

        // Assert
        categoryIds.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "TodoSeeder should write category_ids as a JSON array literal")]
    public async Task TodoSeeder_ShouldWriteJsonArrayLiteral()
    {
        // Arrange
        await RunSeedersAsync();

        // Act
        var json = await _fixture.ScalarAsync(
            "SELECT category_ids::text FROM todos WHERE list_id IS NOT NULL LIMIT 1;");

        // Assert
        json.Should().NotBeNull();
        using var document = JsonDocument.Parse(json!);
        document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        document.RootElement.GetArrayLength().Should().Be(1);
        document.RootElement[0].GetGuid().Should().NotBeEmpty();
    }

    [Fact(DisplayName = "TodoSeeder should reference a seeded list and category")]
    public async Task TodoSeeder_ShouldReferenceSeededListAndCategory()
    {
        // Arrange
        await RunSeedersAsync();

        // Act
        var (listId, categoryId, todoCategoryId) = await _fixture.WithDbContextAsync(async context =>
        {
            var list = await context.Set<TodoList>().AsNoTracking().FirstAsync();
            var category = await context.Set<TodoCategory>().AsNoTracking().FirstAsync();
            var todo = await context.Set<Todo>().AsNoTracking().FirstAsync(t => t.ListId != null);

            return (listId: list.Id, categoryId: category.Id, todoCategoryId: todo.CategoryIds!.First());
        });

        // Assert
        todoCategoryId.Should().Be(categoryId);
        listId.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "Todos seeders should write valid jsonb metadata")]
    public async Task Seeders_ShouldWriteValidJsonMetadata()
    {
        // Arrange
        await RunSeedersAsync();

        // Act
        var metadata = await _fixture.ScalarAsync("SELECT metadata::text FROM todos LIMIT 1;");

        // Assert
        metadata.Should().NotBeNull();
        using var document = JsonDocument.Parse(metadata!);
        document.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact(DisplayName = "Todos seeders should stamp the default tenant")]
    public async Task Seeders_ShouldStampDefaultTenant()
    {
        // Arrange
        await RunSeedersAsync();

        // Act
        var tenants = await _fixture.WithDbContextAsync(async context =>
        {
            var categories = await context.Set<TodoCategory>().AsNoTracking().Select(c => c.TenantId).ToListAsync();
            var lists = await context.Set<TodoList>().AsNoTracking().Select(l => l.TenantId).ToListAsync();
            var todos = await context.Set<Todo>().AsNoTracking().Select(t => t.TenantId).ToListAsync();
            return categories.Concat(lists).Concat(todos).ToList();
        });

        // Assert
        tenants.Should().OnlyContain(tenant => tenant == DefaultTenantId);
    }

    [Fact(DisplayName = "TodoSeeder should include pending, in-progress, and completed samples")]
    public async Task TodoSeeder_ShouldIncludeAllSampleStatuses()
    {
        // Arrange
        await RunSeedersAsync();

        // Act
        var statuses = await _fixture.WithDbContextAsync(async context =>
            await context.Set<Todo>().AsNoTracking().Select(t => t.Status).ToListAsync());

        // Assert
        statuses.Should().Contain(TodoStatus.Pending);
        statuses.Should().Contain(TodoStatus.InProgress);
        statuses.Should().Contain(TodoStatus.Completed);
    }

    [Fact(DisplayName = "Category seeder preview should list four inserts on an empty database")]
    public async Task CategorySeeder_Preview_OnEmptyDatabase_ShouldListFourInserts()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var seeder = new TodoCategorySeeder();
        var initContext = CreateInitializationContext(scope);

        // Act
        var result = await seeder.PreviewAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);
        result.Value.Should().OnlyContain(op => op.Type == InitializationOperationType.DataChange);
        result.Value.Select(op => op.Description).Should().Contain(d => d.Contains("Bug"));
    }

    [Fact(DisplayName = "List seeder preview should list three inserts on an empty database")]
    public async Task ListSeeder_Preview_OnEmptyDatabase_ShouldListThreeInserts()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var seeder = new TodoListSeeder();
        var initContext = CreateInitializationContext(scope);

        // Act
        var result = await seeder.PreviewAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(op => op.Description).Should().Contain(d => d.Contains("Work"));
    }

    [Fact(DisplayName = "Todo seeder preview should list five inserts on an empty database")]
    public async Task TodoSeeder_Preview_OnEmptyDatabase_ShouldListFiveInserts()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var seeder = new TodoSeeder();
        var initContext = CreateInitializationContext(scope);

        // Act
        var result = await seeder.PreviewAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
        result.Value.Select(op => op.Description).Should().Contain(d => d.Contains("Fix login bug"));
    }

    [Fact(DisplayName = "Seeder previews should report a data check after seeding")]
    public async Task SeederPreview_AfterSeeding_ShouldReportDataCheck()
    {
        // Arrange
        await RunSeedersAsync();
        using var scope = _fixture.CreateScope();

        // Act
        var categories = await new TodoCategorySeeder().PreviewAsync(CreateInitializationContext(scope));
        var lists = await new TodoListSeeder().PreviewAsync(CreateInitializationContext(scope));
        var todos = await new TodoSeeder().PreviewAsync(CreateInitializationContext(scope));

        // Assert
        categories.Value.Should().ContainSingle().Which.Type.Should().Be(InitializationOperationType.DataCheck);
        lists.Value.Should().ContainSingle().Which.Type.Should().Be(InitializationOperationType.DataCheck);
        todos.Value.Should().ContainSingle().Which.Type.Should().Be(InitializationOperationType.DataCheck);
    }

    [Fact(DisplayName = "Default pipeline should run seeders in dependency order")]
    public async Task Pipeline_DefaultOrder_ShouldSeedInDependencyOrder()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var initContext = CreateInitializationContext(scope);
        var pipeline = new InitializationPipelineBuilder()
            .AddSeeder(new TodoSeeder())
            .AddSeeder(new TodoListSeeder())
            .AddSeeder(new TodoCategorySeeder())
            .UseDefaultOrder()
            .Build();

        // Act
        var result = await pipeline.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(4);
        (await _fixture.CountRowsAsync("todo_lists")).Should().Be(3);
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    [Fact(DisplayName = "Category seeder should skip existing rows")]
    public async Task CategorySeeder_WhenRowsExist_ShouldSkip()
    {
        // Arrange
        await _fixture.ExecuteAsync(
            "INSERT INTO todo_categories (id, name, description, created_at_utc, created_by, tenant_id, metadata) " +
            "VALUES (gen_random_uuid(), 'Existing', NULL, now(), 'test', gen_random_uuid(), '{}');");
        using var scope = _fixture.CreateScope();
        var seeder = new TodoCategorySeeder();
        var initContext = CreateInitializationContext(scope);

        // Act
        var result = await seeder.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.CountRowsAsync("todo_categories")).Should().Be(1);
    }

    [Fact(DisplayName = "Todo seeder should skip existing rows")]
    public async Task TodoSeeder_WhenRowsExist_ShouldSkip()
    {
        // Arrange
        await RunSeedersAsync();
        using var scope = _fixture.CreateScope();
        var seeder = new TodoSeeder();
        var initContext = CreateInitializationContext(scope);

        // Act
        var result = await seeder.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.CountRowsAsync("todos")).Should().Be(5);
    }

    private async Task<IReadOnlyList<Result>> RunSeedersAsync()
    {
        using var scope = _fixture.CreateScope();
        var initContext = CreateInitializationContext(scope);

        return
        [
            await new TodoCategorySeeder().ExecuteAsync(initContext),
            await new TodoListSeeder().ExecuteAsync(initContext),
            await new TodoSeeder().ExecuteAsync(initContext)
        ];
    }

    private EfCoreInitializationContext CreateInitializationContext(IServiceScope scope)
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TodoSeederTest");
        return new EfCoreInitializationContext(
            context.Database.GetDbConnection(),
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            logger);
    }
}
