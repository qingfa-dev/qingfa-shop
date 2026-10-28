using System.Data;

using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Seeders;

using Core.Todos.Items;

namespace Infrastructure.Persistence.Schemas.Todos.Seeders;
/// <summary>
/// Seeds sample todo items into the database.
/// Depends on TodoCategorySeeder and TodoListSeeder to provide category and list references.
/// </summary>
public sealed class TodoSeeder : IPersistenceSeeder<Todo, Guid>
{
    /// <inheritdoc />
    public string Name => "seeder:todos";

    /// <inheritdoc />
    public string DisplayName => "Seed Todo Items";

    /// <inheritdoc />
    public int Order => 30;

    /// <inheritdoc />
    public string[] DependsOn => ["seeder:todo-categories", "seeder:todo-lists"];

    /// <inheritdoc />
    public bool IsRequired => false;

    /// <inheritdoc />
    Type? IPersistenceSeeder.EntityType => typeof(Todo);

    /// <inheritdoc />
    Type? IPersistenceSeeder.KeyType => typeof(Guid);

    /// <summary>Default tenant ID for seeded data.</summary>
    private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <inheritdoc />
    public Task<Result> ExecuteAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        // Guard: Check if todos already exist to ensure idempotency.
        var existingCount = GetExistingCount(context);
        if (existingCount > 0)
        {
            PersistenceSeederLoggers.AlreadySeeded(context.Logger, "Todos", existingCount);
            return Task.FromResult(Result.Success());
        }

        // Ensure: Connection is open for writes.
        if (context.Connection.State != ConnectionState.Open)
            context.Connection.Open();

        // Lookup: Fetch seeded category and list IDs for references.
        var (categoryId, listId) = GetReferences(context);

        // Create: Seed sample todos using raw SQL.
        var now = DateTimeOffset.UtcNow;
        var samples = BuildSamples(now, categoryId, listId);
        var inserted = 0;

        foreach (var sample in samples)
        {
            using var command = context.Connection.CreateCommand();
            command.CommandText = $"""
                INSERT INTO {TodoTable.Todos} (id, title, description, status, priority, due_at_utc, completed_at_utc,
                    list_id, category_ids, created_at_utc, created_by, tenant_id, metadata)
                VALUES (@id, @title, @description, @status, @priority, @due_at_utc, @completed_at_utc,
                    @list_id, CAST(@category_ids AS jsonb), @created_at_utc, @created_by, @tenant_id, CAST(@metadata AS jsonb))
                """;
            command.Parameters.Add(CreateParam(command, "@id", sample.Id));
            command.Parameters.Add(CreateParam(command, "@title", sample.Title));
            command.Parameters.Add(CreateParam(command, "@description", (object?)sample.Description ?? DBNull.Value));
            command.Parameters.Add(CreateParam(command, "@status", (int)sample.Status));
            command.Parameters.Add(CreateParam(command, "@priority", (int)sample.Priority));
            command.Parameters.Add(CreateParam(command, "@due_at_utc", (object?)sample.DueAtUtc ?? DBNull.Value));
            command.Parameters.Add(CreateParam(command, "@completed_at_utc", (object?)sample.CompletedAtUtc ?? DBNull.Value));
            command.Parameters.Add(CreateParam(command, "@list_id", (object?)sample.ListId ?? DBNull.Value));
            command.Parameters.Add(CreateParam(command, "@category_ids", sample.CategoryIdsJson));
            command.Parameters.Add(CreateParam(command, "@created_at_utc", sample.CreatedAtUtc));
            command.Parameters.Add(CreateParam(command, "@created_by", "system"));
            command.Parameters.Add(CreateParam(command, "@tenant_id", DefaultTenantId));
            command.Parameters.Add(CreateParam(command, "@metadata", "{}"));
            command.CommandTimeout = 30;

            command.ExecuteNonQuery();
            inserted++;
        }

        PersistenceSeederLoggers.Seeded(context.Logger, inserted, "todo items");
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<InitializationOperation>>> PreviewAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        // Check: Count existing todos to determine operations.
        var existingCount = GetExistingCount(context);

        var operations = new List<InitializationOperation>();

        if (existingCount > 0)
        {
            operations.Add(new InitializationOperation
            {
                StepName = Name,
                Description = $"Todos already exist ({existingCount} found). No changes needed.",
                Type = InitializationOperationType.DataCheck,
                EstimatedRowsAffected = 0
            });
        }
        else
        {
            var (categoryId, listId) = GetReferences(context);
            var now = DateTimeOffset.UtcNow;
            var samples = BuildSamples(now, categoryId, listId);

            foreach (var sample in samples)
            {
                operations.Add(new InitializationOperation
                {
                    StepName = Name,
                    Description = $"Insert todo: '{sample.Title}'",
                    Type = InitializationOperationType.DataChange,
                    EstimatedRowsAffected = 1
                });
            }
        }

        return Task.FromResult(Result<IReadOnlyList<InitializationOperation>>.Success(operations));
    }

    /// <summary>Counts existing todos in the database.</summary>
    private static int GetExistingCount(IInitializationContext context)
    {
        // Ensure: Connection is open for reads.
        if (context.Connection.State != ConnectionState.Open)
            context.Connection.Open();

        // Query: Count existing todos using raw SQL.
        using var command = context.Connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {TodoTable.Todos}";
        command.CommandTimeout = 30;

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    /// <summary>Fetches the first category ID and list ID for references.</summary>
    private static (Guid? CategoryId, Guid? ListId) GetReferences(IInitializationContext context)
    {
        // Ensure: Connection is open for reads.
        if (context.Connection.State != ConnectionState.Open)
            context.Connection.Open();

        // Query: Get the first category ID.
        using var catCommand = context.Connection.CreateCommand();
        catCommand.CommandText = $"SELECT id FROM {TodoTable.Categories} LIMIT 1";
        catCommand.CommandTimeout = 30;
        var catResult = catCommand.ExecuteScalar();
        var categoryId = catResult != null && catResult != DBNull.Value
            ? new Guid(catResult.ToString()!)
            : (Guid?)null;

        // Query: Get the first list ID.
        using var listCommand = context.Connection.CreateCommand();
        listCommand.CommandText = $"SELECT id FROM {TodoTable.Lists} LIMIT 1";
        listCommand.CommandTimeout = 30;
        var listResult = listCommand.ExecuteScalar();
        var listId = listResult != null && listResult != DBNull.Value
            ? new Guid(listResult.ToString()!)
            : (Guid?)null;

        return (categoryId, listId);
    }

    /// <summary>Builds sample todo data for seeding.</summary>
    private static List<TodoSample> BuildSamples(
        DateTimeOffset now,
        Guid? categoryId,
        Guid? listId)
    {
        var categoryJson = categoryId is not null
            ? System.Text.Json.JsonSerializer.Serialize(new[] { categoryId.Value })
            : "[]";

        return new List<TodoSample>
        {
            new(
                Id: Guid.NewGuid(),
                Title: "Fix login bug",
                Description: "Users cannot log in with SSO",
                Status: TodoStatus.InProgress,
                Priority: TodoPriority.High,
                DueAtUtc: now.AddDays(1),
                CompletedAtUtc: null,
                ListId: listId,
                CategoryIdsJson: categoryJson,
                CreatedAtUtc: now),
            new(
                Id: Guid.NewGuid(),
                Title: "Add search feature",
                Description: "Implement full-text search for todos",
                Status: TodoStatus.Pending,
                Priority: TodoPriority.Medium,
                DueAtUtc: now.AddDays(7),
                CompletedAtUtc: null,
                ListId: listId,
                CategoryIdsJson: categoryJson,
                CreatedAtUtc: now),
            new(
                Id: Guid.NewGuid(),
                Title: "Update documentation",
                Description: "Add API documentation for new endpoints",
                Status: TodoStatus.Completed,
                Priority: TodoPriority.Low,
                DueAtUtc: now.AddDays(-1),
                CompletedAtUtc: now,
                ListId: null,
                CategoryIdsJson: "[]",
                CreatedAtUtc: now.AddDays(-2)),
            new(
                Id: Guid.NewGuid(),
                Title: "Buy groceries",
                Description: null,
                Status: TodoStatus.Pending,
                Priority: TodoPriority.Low,
                DueAtUtc: now.AddDays(2),
                CompletedAtUtc: null,
                ListId: null,
                CategoryIdsJson: "[]",
                CreatedAtUtc: now),
            new(
                Id: Guid.NewGuid(),
                Title: "Review pull requests",
                Description: "Check and merge pending PRs",
                Status: TodoStatus.InProgress,
                Priority: TodoPriority.High,
                DueAtUtc: now,
                CompletedAtUtc: null,
                ListId: listId,
                CategoryIdsJson: categoryJson,
                CreatedAtUtc: now.AddDays(-1))
        };
    }

    /// <summary>Creates a parameter for the given command.</summary>
    private static IDbDataParameter CreateParam(IDbCommand command, string name, object? value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        return param;
    }

    /// <summary>Sample todo data for insertion.</summary>
    private readonly record struct TodoSample(
        Guid Id,
        string Title,
        string? Description,
        TodoStatus Status,
        TodoPriority Priority,
        DateTimeOffset? DueAtUtc,
        DateTimeOffset? CompletedAtUtc,
        Guid? ListId,
        string CategoryIdsJson,
        DateTimeOffset CreatedAtUtc);
}
