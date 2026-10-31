using Application.Modules.Todos.Items.Models;

using Core.Modules.Todos.Categories;
using Core.Modules.Todos.Items;
using Core.Modules.Todos.Lists;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.DbContext;

/// <summary>
/// Unit tests for the single <see cref="ApplicationDbContext"/>, using SQLite so
/// the model can be created without a PostgreSQL container.
/// </summary>
public sealed class ApplicationDbContextTest : IAsyncLifetime
{
    private static readonly Guid TenantId = new("11111111-1111-1111-1111-111111111111");

    private DbContextOptions<ApplicationDbContext>? _options;

    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source=AppDb_{Guid.NewGuid():N}.db")
            .Options;
        return default;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await using var context = new ApplicationDbContext(_options!);
        await context.Database.EnsureDeletedAsync();
    }

    [Fact(DisplayName = "ApplicationDbContext should be constructible and sealed")]
    public void Constructor_ShouldCreateSealedContext()
    {
        // Act
        var context = new ApplicationDbContext(_options!);

        // Assert
        context.Should().NotBeNull();
        context.Should().BeAssignableTo<Microsoft.EntityFrameworkCore.DbContext>();
        typeof(ApplicationDbContext).IsSealed.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplicationDbContext should expose read-model entity sets without DbSet properties")]
    public void EntitySets_ShouldBeAvailable()
    {
        // Arrange
        var context = new ApplicationDbContext(_options!);

        // Assert
        context.Set<TodoReadModel>().Should().BeAssignableTo<DbSet<TodoReadModel>>();
        context.Set<TodoList>().Should().BeAssignableTo<DbSet<TodoList>>();
        context.Set<TodoCategory>().Should().BeAssignableTo<DbSet<TodoCategory>>();
        typeof(ApplicationDbContext)
            .GetProperties(System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly)
            .Should().NotContain(property =>
                property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));
    }

    [Fact(DisplayName = "ApplicationDbContext should not map Todo as an EF entity")]
    public void Model_ShouldNotMapTodoAggregate()
    {
        // Arrange
        var context = new ApplicationDbContext(_options!);

        // Assert
        context.Model.FindEntityType(typeof(Todo)).Should().BeNull();
        context.Model.FindEntityType(typeof(TodoReadModel)).Should().NotBeNull();
    }

    [Fact(DisplayName = "ApplicationDbContext should discover all entity configurations from the assembly")]
    public void Model_ShouldDiscoverConfigurationsFromAssembly()
    {
        // Arrange
        var context = new ApplicationDbContext(_options!);

        // Act
        var tables = context.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .ToList();

        // Assert
        tables.Should().BeEquivalentTo(
            "todos",
            "todo_lists",
            "todo_categories",
            "users",
            "roles",
            "user_roles",
            "user_claims",
            "user_logins",
            "user_tokens",
            "role_claims",
            "passkeys",
            "passkey_data",
            "refresh_tokens",
            "outbox_messages",
            "inbox_messages",
            "event_store_events",
            "event_store_snapshots",
            "projection_checkpoints",
            "todo_reminders",
            "payments",
            "payment_refunds",
            "payment_webhook_events",
            "payment_idempotency_keys",
            "payment_references",
            "payment_disputes");
    }

    [Fact(DisplayName = "ApplicationDbContext should place Todo, identity, messaging, and payment tables in their schemas")]
    public void Model_ShouldUseTheTodoSchema()
    {
        // Arrange
        var context = new ApplicationDbContext(_options!);

        // Act
        var schemas = context.Model.GetEntityTypes()
            .Select(entity => entity.GetSchema())
            .Distinct()
            .ToList();

        // Assert
        schemas.Should().BeEquivalentTo(["identity", "todo", "messaging", "payment"]);
    }

    [Fact(DisplayName = "ApplicationDbContext should apply every entity configuration in the assembly")]
    public void Model_ShouldApplyEveryEntityConfigurationInAssembly()
    {
        // Arrange
        var assembly = typeof(ApplicationDbContext).Assembly;
        var configuredEntities = assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType &&
                    contract.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                .Select(contract => contract.GetGenericArguments()[0]))
            .Distinct()
            .ToList();
        var context = new ApplicationDbContext(_options!);

        // Assert
        configuredEntities.Should().NotBeEmpty();
        foreach (var entityType in configuredEntities)
        {
            context.Model.FindEntityType(entityType).Should().NotBeNull(
                "configuration for {0} should be applied from the assembly", entityType.Name);
        }
    }

    [Fact(DisplayName = "ApplicationDbContext should map CategoryIds as jsonb with a value comparer")]
    public void Model_ShouldMapCategoryIdsAsJsonbWithComparer()
    {
        // Arrange
        var context = new ApplicationDbContext(_options!);

        // Act
        var property = context.Model.FindEntityType(typeof(TodoReadModel))!
            .FindProperty(nameof(TodoReadModel.CategoryIds))!;

        // Assert
        property.GetColumnType().Should().Be("jsonb");
        property.GetValueComparer().Should().NotBeNull();
    }

    [Fact(DisplayName = "ApplicationDbContext should map tenant and audit indexes")]
    public void Model_ShouldMapIndexes()
    {
        // Arrange
        var context = new ApplicationDbContext(_options!);

        // Act
        var indexes = context.Model.FindEntityType(typeof(TodoReadModel))!
            .GetIndexes()
            .Select(index => string.Join(",", index.Properties.Select(property => property.Name)))
            .ToList();

        // Assert
        indexes.Should().Contain("TenantId,IsDeleted");
    }

    [Fact(DisplayName = "ApplicationDbContext should not have pending model changes")]
    public void Model_ShouldNotHavePendingChanges()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=qingfa_shop;Username=postgres;Password=postgres")
            .Options;
        using var context = new ApplicationDbContext(options);

        context.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact(DisplayName = "ApplicationDbContext should discover the InitialCreate and AddPayments migrations")]
    public void Database_ShouldDiscoverMigrations()
    {
        // Arrange
        var context = new ApplicationDbContext(_options!);

        // Act
        var migrations = context.Database.GetMigrations().ToList();

        // Assert
        migrations.Should().HaveCount(2);
        migrations[0].EndsWith("_InitialCreate", StringComparison.Ordinal).Should().BeTrue();
        migrations[1].EndsWith("_AddPayments", StringComparison.Ordinal).Should().BeTrue();
    }

    [Fact(DisplayName = "The Persistence assembly should contain one DbContext and one design-time factory")]
    public void Assembly_ShouldContainExactlyOneContextAndFactory()
    {
        // Arrange
        var assembly = typeof(ApplicationDbContext).Assembly;

        // Act
        var contexts = assembly.GetTypes()
            .Where(type => !type.IsAbstract &&
                typeof(Microsoft.EntityFrameworkCore.DbContext).IsAssignableFrom(type))
            .ToList();
        var factories = assembly.GetTypes()
            .Where(type => !type.IsAbstract &&
                type.GetInterfaces().Any(contract =>
                    contract.IsGenericType &&
                    contract.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<>)))
            .ToList();

        // Assert
        contexts.Should().ContainSingle().Which.Should().Be(typeof(ApplicationDbContext));
        factories.Should().ContainSingle().Which.Should().Be(typeof(ApplicationDbContextDesignTimeFactory));
    }

    [Fact(DisplayName = "ApplicationDbContext database should be accessible")]
    public async Task Database_ShouldBeAccessible()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options!);

        // Act
        await context.Database.EnsureCreatedAsync();

        // Assert
        context.Database.GetDbConnection().Should().NotBeNull();
        (await context.Database.CanConnectAsync()).Should().BeTrue();
    }

    [Fact(DisplayName = "ApplicationDbContext should persist and read a Todo read model")]
    public async Task CanAddAndSaveTodo()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var todo = CreateReadModel("Test Todo", "Test Description");

        // Act
        context.Set<TodoReadModel>().Add(todo);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Set<TodoReadModel>().FindAsync(todo.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Test Todo");
    }

    [Fact(DisplayName = "ApplicationDbContext should persist and read a TodoList")]
    public async Task CanAddAndSaveTodoList()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var list = TodoList.Create(
            id: Guid.NewGuid(),
            tenantId: TenantId,
            name: "Test List",
            nowUtc: DateTimeOffset.UtcNow).Value!;

        // Act
        context.Set<TodoList>().Add(list);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Set<TodoList>().FindAsync(list.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test List");
    }

    [Fact(DisplayName = "ApplicationDbContext should persist and read a TodoCategory")]
    public async Task CanAddAndSaveTodoCategory()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var category = TodoCategory.Create(
            id: Guid.NewGuid(),
            tenantId: TenantId,
            name: "Test Category",
            nowUtc: DateTimeOffset.UtcNow).Value!;

        // Act
        context.Set<TodoCategory>().Add(category);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Set<TodoCategory>().FindAsync(category.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Category");
    }

    [Fact(DisplayName = "ApplicationDbContext should round-trip CategoryIds")]
    public async Task CanRoundTripCategoryIds()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var categoryId = Guid.NewGuid();
        var todo = CreateReadModel("Categorized");
        todo.CategoryIds = [categoryId];

        // Act
        context.Set<TodoReadModel>().Add(todo);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var saved = await context.Set<TodoReadModel>().FindAsync(todo.Id);

        // Assert
        saved!.CategoryIds.Should().Contain(categoryId);
    }

    [Fact(DisplayName = "ApplicationDbContext should round-trip metadata")]
    public async Task CanRoundTripMetadata()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var todo = CreateReadModel("Metadata");
        todo.Metadata["source"] = "unit-test";

        // Act
        context.Set<TodoReadModel>().Add(todo);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var saved = await context.Set<TodoReadModel>().FindAsync(todo.Id);

        // Assert
        saved!.Metadata.Should().NotBeNull();
    }

    [Fact(DisplayName = "ApplicationDbContext should query all persisted Todos")]
    public async Task CanQueryTodos()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        context.Set<TodoReadModel>().AddRange(
            CreateReadModel("First"),
            CreateReadModel("Second"));
        await context.SaveChangesAsync();

        // Act
        var all = await context.Set<TodoReadModel>().ToListAsync();

        // Assert
        all.Should().HaveCount(2);
    }

    private static TodoReadModel CreateReadModel(string title, string? description = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Title = title,
            Description = description,
            Status = TodoStatus.Pending,
            Priority = TodoPriority.Medium,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = "test",
        };
}
