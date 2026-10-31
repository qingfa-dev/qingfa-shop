using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.UnitOfWorks;

using Infrastructure.BuildingBlocks.Adapters.Persistence.Initialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Initialization;

namespace Infrastructure.Integration.Tests.BuildingBlocks.Persistence.Initialization;

/// <summary>
/// Integration tests for <see cref="EfCoreMigrator{TContext}"/> against a real
/// PostgreSQL database with the production Todo migrations.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Initialization.Migrations.PostgreSQL")]
[Collection(PersistenceInitializationCollection.Name)]
public sealed class EfCoreMigratorIntegrationTest(PersistenceInitializationFixture fixture) : IAsyncLifetime
{
    private readonly PersistenceInitializationFixture _fixture = fixture;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _fixture.DropDatabaseAsync();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact(DisplayName = "MigrateAsync should create the Todo schema and migration history")]
    public async Task MigrateAsync_OnFreshDatabase_ShouldCreateSchemaAndHistory()
    {
        // Act
        await _fixture.MigrateAsync();

        // Assert
        (await _fixture.TableExistsAsync("todo_categories")).Should().BeTrue();
        (await _fixture.TableExistsAsync("todo_lists")).Should().BeTrue();
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
        (await _fixture.TableExistsAsync("public.__EFMigrationsHistory")).Should().BeTrue();
        (await _fixture.TableExistsAsync("messaging.outbox_messages")).Should().BeTrue();
        (await _fixture.TableExistsAsync("messaging.inbox_messages")).Should().BeTrue();
        (await _fixture.CountRowsAsync("public.__EFMigrationsHistory")).Should().Be(1);
    }

    [Fact(DisplayName = "MigrateAsync should record the InitialCreate migration")]
    public async Task MigrateAsync_OnFreshDatabase_ShouldRecordInitialCreateMigration()
    {
        // Act
        await _fixture.MigrateAsync();

        // Assert
        var migrationId = await _fixture.ScalarAsync(
            "SELECT \"MigrationId\" FROM public.\"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%_InitialCreate';");

        migrationId.Should().NotBeNull();
        migrationId.Should().EndWith("_InitialCreate");
    }

    [Fact(DisplayName = "MigrateAsync should create jsonb category_ids and metadata columns")]
    public async Task MigrateAsync_ShouldCreateJsonbColumns()
    {
        // Act
        await _fixture.MigrateAsync();

        // Assert
        var dataTypes = await _fixture.WithDbContextAsync(async context =>
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            if (command.Connection!.State != System.Data.ConnectionState.Open)
            {
                await command.Connection.OpenAsync();
            }

            command.CommandText = """
                SELECT column_name || ':' || data_type
                FROM information_schema.columns
                WHERE table_schema = 'todo'
                  AND table_name = 'todos'
                  AND column_name IN ('category_ids', 'metadata')
                ORDER BY column_name;
                """;

            var results = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        });

        dataTypes.Should().BeEquivalentTo(["category_ids:jsonb", "metadata:jsonb"]);
    }

    [Fact(DisplayName = "MigrateAsync should add the version column to every aggregate table")]
    public async Task MigrateAsync_ShouldAddVersionColumnsToAggregateTables()
    {
        // Act
        await _fixture.MigrateAsync();

        // Assert
        string? columnCount = await _fixture.ScalarAsync("""
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = 'todo'
              AND column_name = 'version'
              AND table_name IN ('todos', 'todo_lists', 'todo_categories');
            """);

        columnCount.Should().Be("3");
    }

    [Fact(DisplayName = "MigrateAsync should be idempotent")]
    public async Task MigrateAsync_WhenCalledTwice_ShouldBeIdempotent()
    {
        // Arrange
        await _fixture.MigrateAsync();

        // Act
        await _fixture.MigrateAsync();

        // Assert
        (await _fixture.CountRowsAsync("public.__EFMigrationsHistory")).Should().Be(1);
    }

    [Fact(DisplayName = "ExecuteAsync should move a legacy migration history into the default schema")]
    public async Task ExecuteAsync_WithLegacyHistory_ShouldMoveItToDefaultSchema()
    {
        // Arrange: a deployed database whose history still lives in the old schema.
        await _fixture.MigrateAsync();
        await _fixture.ExecuteAsync(MoveHistoryToLegacySchemaSql());

        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        var migrator = new EfCoreMigrator<ApplicationDbContext>(context, logger);
        var initContext = CreateInitializationContext(scope, context, logger);

        // Act
        var result = await migrator.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.TableExistsAsync("legacy_history.__EFMigrationsHistory")).Should().BeFalse();
        (await _fixture.TableExistsAsync("public.__EFMigrationsHistory")).Should().BeTrue();
        (await _fixture.CountRowsAsync("public.__EFMigrationsHistory")).Should().Be(1);
    }

    [Fact(DisplayName = "ExecuteAsync should keep a legacy history when the target table exists")]
    public async Task ExecuteAsync_WithLegacyHistoryAndExistingTarget_ShouldSkipTheMove()
    {
        // Arrange: public history already exists, so moving onto it would fail.
        await _fixture.MigrateAsync();
        await _fixture.ExecuteAsync(CreateLegacyHistorySql());

        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        var migrator = new EfCoreMigrator<ApplicationDbContext>(context, logger);
        var initContext = CreateInitializationContext(scope, context, logger);

        // Act
        var result = await migrator.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.TableExistsAsync("legacy_history.__EFMigrationsHistory")).Should().BeTrue();
        (await _fixture.CountRowsAsync("public.__EFMigrationsHistory")).Should().Be(1);
    }

    private static string MoveHistoryToLegacySchemaSql() => """
        CREATE SCHEMA IF NOT EXISTS legacy_history;
        ALTER TABLE public."__EFMigrationsHistory" SET SCHEMA legacy_history;
        """;

    private static string CreateLegacyHistorySql() => """
        CREATE SCHEMA IF NOT EXISTS legacy_history;
        CREATE TABLE legacy_history."__EFMigrationsHistory" (
            "MigrationId" character varying(150) NOT NULL,
            "ProductVersion" character varying(32) NOT NULL,
            CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
        );
        INSERT INTO legacy_history."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20200101000000_Legacy', '9.0.0');
        """;

    [Fact(DisplayName = "GetPendingMigrationsAsync should report InitialCreate before migrating")]
    public async Task GetPendingMigrationsAsync_BeforeMigrating_ShouldContainInitialCreate()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act
        var pending = await context.Database.GetPendingMigrationsAsync();

        // Assert
        pending.Should().ContainSingle();
        pending.Single().EndsWith("_InitialCreate", StringComparison.Ordinal).Should().BeTrue();
    }

    [Fact(DisplayName = "GetPendingMigrationsAsync should be empty after migrating")]
    public async Task GetPendingMigrationsAsync_AfterMigrating_ShouldBeEmpty()
    {
        // Arrange
        await _fixture.MigrateAsync();
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act
        var pending = await context.Database.GetPendingMigrationsAsync();

        // Assert
        pending.Should().BeEmpty();
    }

    [Fact(DisplayName = "EfCoreMigrator ExecuteAsync should apply pending migrations")]
    public async Task ExecuteAsync_WithPendingMigrations_ShouldApplyThem()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        var migrator = new EfCoreMigrator<ApplicationDbContext>(context, logger);
        var initContext = CreateInitializationContext(scope, context, logger);

        // Act
        var result = await migrator.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.TableExistsAsync("todos")).Should().BeTrue();
    }

    [Fact(DisplayName = "EfCoreMigrator ExecuteAsync should succeed when no migrations are pending")]
    public async Task ExecuteAsync_WhenUpToDate_ShouldReturnSuccess()
    {
        // Arrange
        await _fixture.MigrateAsync();
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        var migrator = new EfCoreMigrator<ApplicationDbContext>(context, logger);
        var initContext = CreateInitializationContext(scope, context, logger);

        // Act
        var result = await migrator.ExecuteAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "EfCoreMigrator PreviewAsync should describe a pending migration")]
    public async Task PreviewAsync_WithPendingMigrations_ShouldReturnSchemaChange()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        var migrator = new EfCoreMigrator<ApplicationDbContext>(context, logger);
        var initContext = CreateInitializationContext(scope, context, logger);

        // Act
        var result = await migrator.PreviewAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value.Should().OnlyContain(operation =>
            operation.Type == InitializationOperationType.SchemaChange);
        result.Value[0].Description.Should().Contain("InitialCreate");
        result.Value[0].CommandText.Should().EndWith("_InitialCreate");
        result.Value[^1].CommandText.Should().EndWith("_InitialCreate");
    }

    [Fact(DisplayName = "EfCoreMigrator PreviewAsync should report an up-to-date schema")]
    public async Task PreviewAsync_WhenUpToDate_ShouldReturnSchemaCheck()
    {
        // Arrange
        await _fixture.MigrateAsync();
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        var migrator = new EfCoreMigrator<ApplicationDbContext>(context, logger);
        var initContext = CreateInitializationContext(scope, context, logger);

        // Act
        var result = await migrator.PreviewAsync(initContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Type.Should().Be(InitializationOperationType.SchemaCheck);
        result.Value[0].EstimatedRowsAffected.Should().Be(0);
    }

    [Fact(DisplayName = "EfCoreMigrator should expose the latest migration as its version")]
    public async Task Version_ShouldExposeLatestMigrationId()
    {
        // Arrange
        await _fixture.MigrateAsync();
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        var migrator = new EfCoreMigrator<ApplicationDbContext>(context, logger);

        // Assert
        migrator.Version.Should().EndWith("_InitialCreate");
    }

    private static EfCoreInitializationContext CreateInitializationContext(
        IServiceScope scope,
        ApplicationDbContext context,
        ILogger logger)
    {
        return new EfCoreInitializationContext(
            context.Database.GetDbConnection(),
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            logger);
    }
}