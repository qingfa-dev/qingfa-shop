using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Resetters;

using Infrastructure.Persistence.Initialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Unit.Tests.Persistence.Initialization;

/// <summary>Unit tests for EfCoreResetter using SQLite-backed DbContext.</summary>
public sealed class EfCoreResetterTest : IAsyncLifetime
{
    private string? _dbName;

    public ValueTask InitializeAsync()
    {
        _dbName = $"Data Source=ModeResetter_{Guid.NewGuid()}.db";
        return default;
    }

    public ValueTask DisposeAsync() => default;

    private TestResetterDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestResetterDbContext>()
            .UseSqlite(_dbName ?? $"Data Source=ModeResetter_{Guid.NewGuid()}.db")
            .Options;
        return new TestResetterDbContext(options);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultProperties()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;

        // Act
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger);

        // Assert
        resetter.Name.Should().Be("resetter:efcore");
        resetter.DisplayName.Should().Be("EF Core Database Reset");
        resetter.Order.Should().Be(ResetterConstant.Limits.DefaultOrder);
        resetter.DependsOn.Should().BeEmpty();
        resetter.IsRequired.Should().BeFalse();
        resetter.Scope.Should().Be(ResetScope.Full);
        resetter.Targets.Should().BeEmpty();
        resetter.DisableForeignKeys.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithDataOnlyScope_ShouldSetScope()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;

        // Act
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger, scope: ResetScope.DataOnly);

        // Assert
        resetter.Scope.Should().Be(ResetScope.DataOnly);
    }

    [Fact]
    public void Constructor_WithSchemaOnlyScope_ShouldSetScope()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;

        // Act
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger, scope: ResetScope.SchemaOnly);

        // Assert
        resetter.Scope.Should().Be(ResetScope.SchemaOnly);
    }

    [Fact]
    public void Constructor_WithCustomName_ShouldSetName()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;

        // Act
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger, name: "custom:resetter", displayName: "Custom Resetter");

        // Assert
        resetter.Name.Should().Be("custom:resetter");
        resetter.DisplayName.Should().Be("Custom Resetter");
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestResetterDbContext? context = null;
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;

        // Act & Assert
        var act = () => new EfCoreResetter<TestResetterDbContext>(context!, logger);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("context");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = CreateContext();
        ILogger? logger = null;

        // Act & Assert
        var act = () => new EfCoreResetter<TestResetterDbContext>(context, logger!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;
        string? name = null;

        // Act & Assert
        var act = () => new EfCoreResetter<TestResetterDbContext>(context, logger, name: name!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Constructor_WithDisableForeignKeysFalse_ShouldSetProperty()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;

        // Act
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger, disableForeignKeys: false);

        // Assert
        resetter.DisableForeignKeys.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithSchemaOnlyScope_ShouldRecreateSchema()
    {
        // Arrange
        var freshOptions = new DbContextOptionsBuilder()
            .UseSqlite($"Data Source=ModeResetterSchema_{Guid.NewGuid()}.db")
            .Options;
        var freshContext = new TestResetterDbContext(freshOptions);
        await freshContext.Database.EnsureCreatedAsync();

        // Seed data
        freshContext.Entities.Add(new TestResetterEntity { Name = "Test" });
        await freshContext.SaveChangesAsync();

        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;
        var resetter = new EfCoreResetter<TestResetterDbContext>(freshContext, logger, scope: ResetScope.SchemaOnly);
        var initContext = CreateInitContextWithConnection(freshContext);

        // Act
        var result = await resetter.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify schema was recreated (data should be gone)
        var count = await freshContext.Entities.CountAsync();
        count.Should().Be(0);

        // Cleanup
        await freshContext.Database.EnsureDeletedAsync();
        freshContext.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_WithFullScope_ShouldRecreateSchema()
    {
        // Arrange
        var freshOptions = new DbContextOptionsBuilder()
            .UseSqlite($"Data Source=ModeResetterFull_{Guid.NewGuid()}.db")
            .Options;
        var freshContext = new TestResetterDbContext(freshOptions);
        await freshContext.Database.EnsureCreatedAsync();

        freshContext.Entities.Add(new TestResetterEntity { Name = "Test" });
        await freshContext.SaveChangesAsync();

        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;
        var resetter = new EfCoreResetter<TestResetterDbContext>(freshContext, logger, scope: ResetScope.Full);
        var initContext = CreateInitContextWithConnection(freshContext);

        // Act
        var result = await resetter.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Cleanup
        await freshContext.Database.EnsureDeletedAsync();
        freshContext.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldReturnFailure()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger, scope: ResetScope.SchemaOnly);
        var initContext = CreateInitContextWithConnection(context);

        // Act
        var result = await resetter.ExecuteAsync(initContext, cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Contain("Cancelled");
    }

    [Fact]
    public async Task PreviewAsync_WithDataOnlyScope_ShouldReturnDataChange()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger, scope: ResetScope.DataOnly);
        var initContext = CreateInitContext();

        // Act
        var result = await resetter.PreviewAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Type.Should().Be(InitializationOperationType.DataChange);
        result.Value[0].Description.ToLowerInvariant().Should().Contain("truncate");
    }

    [Fact]
    public async Task PreviewAsync_WithSchemaOnlyScope_ShouldReturnSchemaChange()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger, scope: ResetScope.SchemaOnly);
        var initContext = CreateInitContext();

        // Act
        var result = await resetter.PreviewAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Type.Should().Be(InitializationOperationType.SchemaChange);
        result.Value[0].Description.Should().Contain("schema");
    }

    [Fact]
    public async Task PreviewAsync_WithFullScope_ShouldReturnSchemaChange()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger, scope: ResetScope.Full);
        var initContext = CreateInitContext();

        // Act
        var result = await resetter.PreviewAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Type.Should().Be(InitializationOperationType.SchemaChange);
    }

    [Fact]
    public void Resetter_ShouldImplementIPersistenceResetter()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;

        // Act
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger);

        // Assert
        resetter.Should().BeAssignableTo<IPersistenceResetter>();
        resetter.Should().BeAssignableTo<IInitializationStep>();
    }

    [Fact]
    public void Constructor_WithCustomOrder_ShouldSetOrder()
    {
        // Arrange
        var context = CreateContext();
        var logger = new Mock<ILogger<EfCoreResetter<TestResetterDbContext>>>().Object;

        // Act
        var resetter = new EfCoreResetter<TestResetterDbContext>(context, logger, order: 10);

        // Assert
        resetter.Order.Should().Be(10);
    }

    private EfCoreInitializationContext CreateInitContext()
    {
        var connection = new Mock<System.Data.IDbConnection>().Object;
        var unitOfWork = new Mock<Application.Common.Persistence.UnitOfWorks.IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;
        return new EfCoreInitializationContext(connection, unitOfWork, logger);
    }

    private EfCoreInitializationContext CreateInitContextWithConnection(DbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var unitOfWork = new Mock<Application.Common.Persistence.UnitOfWorks.IUnitOfWork>().Object;
        var logger = new Mock<ILogger>().Object;
        return new EfCoreInitializationContext(connection, unitOfWork, logger);
    }
}

/// <summary>Test DbContext for resetter tests.</summary>
public class TestResetterDbContext : DbContext
{
    public TestResetterDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<TestResetterEntity> Entities => Set<TestResetterEntity>();
}

public class TestResetterEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
