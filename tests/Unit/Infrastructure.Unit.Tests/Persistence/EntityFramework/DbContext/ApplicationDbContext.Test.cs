using Core.Common.Monad.Metadata;
using Core.Todos;

using Infrastructure.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Contexts;

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

    [Fact(DisplayName = "ApplicationDbContext should expose Todo entity sets without DbSet properties")]
    public void EntitySets_ShouldBeAvailable()
    {
        // Arrange
        var context = new ApplicationDbContext(_options!);

        // Assert
        context.Set<Todo>().Should().BeAssignableTo<DbSet<Todo>>();
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
        tables.Should().BeEquivalentTo("todos", "todo_lists", "todo_categories");
    }

    [Fact(DisplayName = "ApplicationDbContext should place every Todo table in the todo schema")]
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
        schemas.Should().BeEquivalentTo(["todo"]);
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
        var property = context.Model.FindEntityType(typeof(Todo))!
            .FindProperty(nameof(Todo.CategoryIds))!;

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
        var indexes = context.Model.FindEntityType(typeof(Todo))!
            .GetIndexes()
            .Select(index => string.Join(",", index.Properties.Select(property => property.Name)))
            .ToList();

        // Assert
        indexes.Should().Contain("TenantId,IsDeleted");
    }

    [Fact(DisplayName = "ApplicationDbContext should discover migrations from its assembly")]
    public void Database_ShouldDiscoverMigrations()
    {
        // Arrange
        var context = new ApplicationDbContext(_options!);

        // Act
        var migrations = context.Database.GetMigrations().ToList();

        // Assert
        migrations.Should().ContainSingle().Which.Should().EndWith("_InitialCreate");
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

    [Fact(DisplayName = "ApplicationDbContext should persist and read a Todo")]
    public async Task CanAddAndSaveTodo()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var todo = Todo.Create(
            id: Guid.NewGuid(),
            tenantId: TenantId,
            title: "Test Todo",
            nowUtc: DateTimeOffset.UtcNow,
            description: "Test Description").Value!;

        // Act
        context.Set<Todo>().Add(todo);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Set<Todo>().FindAsync(todo.Id);
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
        var todo = Todo.Create(
            id: Guid.NewGuid(),
            tenantId: TenantId,
            title: "Categorized",
            nowUtc: DateTimeOffset.UtcNow)
            .AddCategory(categoryId)
            .Value!;

        // Act
        context.Set<Todo>().Add(todo);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var saved = await context.Set<Todo>().FindAsync(todo.Id);

        // Assert
        saved!.CategoryIds.Should().Contain(categoryId);
    }

    [Fact(DisplayName = "ApplicationDbContext should round-trip metadata")]
    public async Task CanRoundTripMetadata()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        var todo = Todo.Create(
            id: Guid.NewGuid(),
            tenantId: TenantId,
            title: "Metadata",
            nowUtc: DateTimeOffset.UtcNow).Value!;
        todo.Metadata["source"] = "unit-test";

        // Act
        context.Set<Todo>().Add(todo);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var saved = await context.Set<Todo>().FindAsync(todo.Id);

        // Assert
        saved!.Metadata.Should().NotBeNull();
    }

    [Fact(DisplayName = "ApplicationDbContext should query all persisted Todos")]
    public async Task CanQueryTodos()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options!);
        await context.Database.EnsureCreatedAsync();
        context.Set<Todo>().AddRange(
            Todo.Create(Guid.NewGuid(), TenantId, "First", DateTimeOffset.UtcNow).Value!,
            Todo.Create(Guid.NewGuid(), TenantId, "Second", DateTimeOffset.UtcNow).Value!);
        await context.SaveChangesAsync();

        // Act
        var all = await context.Set<Todo>().ToListAsync();

        // Assert
        all.Should().HaveCount(2);
    }
}
