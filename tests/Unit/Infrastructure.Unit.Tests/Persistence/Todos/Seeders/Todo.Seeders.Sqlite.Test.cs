using System.Data;

using Application.Common.Persistence.Initialization;

using Infrastructure.Persistence.Schemas.Todos.Seeders;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Unit.Tests.Persistence.Todos.Seeders;

/// <summary>
/// Executes the three Todo seeders against SQLite so the raw-SQL insert,
/// reference lookup, idempotency, and parameter branches are all exercised.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class TodoSeedersSqliteTest : IAsyncLifetime
{
    private string _mainPath = string.Empty;
    private string _todoPath = string.Empty;
    private SqliteConnection _connection = null!;

    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "opencode");
        Directory.CreateDirectory(root);
        _mainPath = Path.Combine(root, $"seed-main-{Guid.NewGuid():N}.db");
        _todoPath = Path.Combine(root, $"seed-todo-{Guid.NewGuid():N}.db");

        _connection = new SqliteConnection($"Data Source={_mainPath}");
        _connection.Open();
        Execute($"ATTACH DATABASE '{_todoPath.Replace("'", "''", StringComparison.Ordinal)}' AS todo;");
        Execute("""
            CREATE TABLE todo.todo_categories (
                id TEXT PRIMARY KEY, name TEXT, description TEXT, created_at_utc TEXT,
                created_by TEXT, tenant_id TEXT, metadata TEXT);
            CREATE TABLE todo.todo_lists (
                id TEXT PRIMARY KEY, name TEXT, description TEXT, created_at_utc TEXT,
                created_by TEXT, tenant_id TEXT, metadata TEXT);
            CREATE TABLE todo.todos (
                id TEXT PRIMARY KEY, title TEXT, description TEXT, status INTEGER,
                priority INTEGER, due_at_utc TEXT, completed_at_utc TEXT, list_id TEXT,
                category_ids TEXT, created_at_utc TEXT, created_by TEXT, tenant_id TEXT,
                metadata TEXT);
            """);
        _connection.Close();
        return default;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _connection.Dispose();
        File.Delete(_mainPath);
        File.Delete(_todoPath);
        return default;
    }

    private void Execute(string sql)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private long Count(string table)
    {
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table};";
        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private IInitializationContext CreateContext()
    {
        var context = new Mock<IInitializationContext>();
        context.SetupGet(c => c.Connection).Returns(_connection);
        context.SetupGet(c => c.Logger).Returns(Mock.Of<ILogger>());
        return context.Object;
    }

    private async Task RunTodoSeederAsync()
    {
        _connection.Close();
        Result result = await new TodoSeeder().ExecuteAsync(CreateContext());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Seeders should insert samples, reopen the connection, and skip on rerun")]
    public async Task Seeders_ShouldInsertAndSkip()
    {
        // Arrange
        IInitializationContext context = CreateContext();

        // Act - todos first, with no category or list rows so references are null.
        _connection.Close();
        Result firstTodoRun = await new TodoSeeder().ExecuteAsync(context);
        firstTodoRun.IsSuccess.Should().BeTrue();
        Count("todo.todos").Should().Be(5);

        // Act - categories and lists then provide references.
        (await new TodoCategorySeeder().ExecuteAsync(context)).IsSuccess.Should().BeTrue();
        (await new TodoListSeeder().ExecuteAsync(context)).IsSuccess.Should().BeTrue();
        Count("todo.todo_categories").Should().Be(4);
        Count("todo.todo_lists").Should().Be(3);

        Execute("DELETE FROM todo.todos;");

        // Act - seed again with references present.
        _connection.Close();
        Result secondTodoRun = await new TodoSeeder().ExecuteAsync(context);

        // Assert
        secondTodoRun.IsSuccess.Should().BeTrue();
        Count("todo.todos").Should().Be(5);
        Count("todo.todos WHERE list_id IS NOT NULL").Should().Be(3);

        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM todo.todos WHERE category_ids IS NOT NULL;";
        Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture)
            .Should().Be(5);

        // Act - rerun every seeder; each should skip.
        (await new TodoCategorySeeder().ExecuteAsync(context)).IsSuccess.Should().BeTrue();
        (await new TodoListSeeder().ExecuteAsync(context)).IsSuccess.Should().BeTrue();
        (await new TodoSeeder().ExecuteAsync(context)).IsSuccess.Should().BeTrue();

        // Assert
        Count("todo.todo_categories").Should().Be(4);
        Count("todo.todo_lists").Should().Be(3);
        Count("todo.todos").Should().Be(5);
    }

    [Fact(DisplayName = "TodoSeeder should write a category value without references")]
    public async Task TodoSeeder_WithoutReferences_ShouldWriteCategoryValue()
    {
        // Act
        _connection.Close();
        Result result = await new TodoSeeder().ExecuteAsync(CreateContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        Count("todo.todos").Should().Be(5);

        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT category_ids FROM todo.todos;";
        using SqliteDataReader reader = command.ExecuteReader();
        var values = new List<string>();
        while (reader.Read())
        {
            values.Add(reader.GetString(0));
        }

        values.Should().ContainSingle().Which.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "Seeders should expose preview operations after seeding")]
    public async Task Seeders_Preview_AfterSeeding_ShouldReportDataCheck()
    {
        // Arrange
        IInitializationContext context = CreateContext();
        _connection.Close();
        await new TodoSeeder().ExecuteAsync(context);
        (await new TodoCategorySeeder().ExecuteAsync(context)).IsSuccess.Should().BeTrue();
        (await new TodoListSeeder().ExecuteAsync(context)).IsSuccess.Should().BeTrue();

        // Act
        var categories = await new TodoCategorySeeder().PreviewAsync(context);
        var lists = await new TodoListSeeder().PreviewAsync(context);
        var todos = await new TodoSeeder().PreviewAsync(context);

        // Assert
        categories.Value.Should().ContainSingle().Which.Type.Should().Be(InitializationOperationType.DataCheck);
        lists.Value.Should().ContainSingle().Which.Type.Should().Be(InitializationOperationType.DataCheck);
        todos.Value.Should().ContainSingle().Which.Type.Should().Be(InitializationOperationType.DataCheck);
    }
}
