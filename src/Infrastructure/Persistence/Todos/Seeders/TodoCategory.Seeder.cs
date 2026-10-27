
using System.Data;

using Application.Common.Persistence.Initialization;
using Application.Common.Persistence.Initialization.Seeders;

using Core.Todos;

using Microsoft.Extensions.Logging;

using static Application.Common.Persistence.Initialization.Seeders.PersistenceSeederLoggers;

namespace Infrastructure.Persistence.Todos.Seeders;
/// <summary>
/// Seeds default todo categories into the database.
/// Skips seeding if categories already exist (idempotent).
/// </summary>
public sealed class TodoCategorySeeder : IPersistenceSeeder<TodoCategory, Guid>
{
    /// <inheritdoc />
    public string Name => "seeder:todo-categories";

    /// <inheritdoc />
    public string DisplayName => "Seed Todo Categories";

    /// <inheritdoc />
    public int Order => 10;

    /// <inheritdoc />
    public string[] DependsOn => [];

    /// <inheritdoc />
    public bool IsRequired => false;

    /// <inheritdoc />
    Type? IPersistenceSeeder.EntityType => typeof(TodoCategory);

    /// <inheritdoc />
    Type? IPersistenceSeeder.KeyType => typeof(Guid);

    /// <summary>Default tenant ID for seeded data.</summary>
    private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Default categories to seed.</summary>
    private static readonly (string Name, string? Description)[] DefaultCategories =
    [
        ("Bug", "Issues that need to be fixed"),
        ("Feature", "New functionality requests"),
        ("Improvement", "Enhancements to existing features"),
        ("Documentation", "Documentation tasks")
    ];

    /// <inheritdoc />
    public Task<Result> ExecuteAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        // Guard: Check if categories already exist to ensure idempotency.
        var existingCount = GetExistingCount(context);
        if (existingCount > 0)
        {
            AlreadySeeded(context.Logger, "Todo categories", existingCount);
            return Task.FromResult(Result.Success());
        }

        // Ensure: Connection is open for writes.
        if (context.Connection.State != ConnectionState.Open)
            context.Connection.Open();

        // Create: Seed default categories using raw SQL.
        var now = DateTimeOffset.UtcNow;
        var inserted = 0;

        foreach (var (name, description) in DefaultCategories)
        {
            var id = Guid.NewGuid();

            using var command = context.Connection.CreateCommand();
            command.CommandText = """
                INSERT INTO todo_categories (id, name, description, created_at_utc, created_by, tenant_id, metadata)
                VALUES (@id, @name, @description, @created_at_utc, @created_by, @tenant_id, CAST(@metadata AS jsonb))
                """;
            command.Parameters.Add(CreateParam(command, "@id", id));
            command.Parameters.Add(CreateParam(command, "@name", name));
            command.Parameters.Add(CreateParam(command, "@description", (object?)description ?? DBNull.Value));
            command.Parameters.Add(CreateParam(command, "@created_at_utc", now));
            command.Parameters.Add(CreateParam(command, "@created_by", "system"));
            command.Parameters.Add(CreateParam(command, "@tenant_id", DefaultTenantId));
            command.Parameters.Add(CreateParam(command, "@metadata", "{}"));
            command.CommandTimeout = 30;

            command.ExecuteNonQuery();
            inserted++;
        }

        Seeded(context.Logger, inserted, "todo categories");
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<InitializationOperation>>> PreviewAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        // Check: Count existing categories to determine operations.
        var existingCount = GetExistingCount(context);

        var operations = new List<InitializationOperation>();

        if (existingCount > 0)
        {
            operations.Add(new InitializationOperation
            {
                StepName = Name,
                Description = $"Todo categories already exist ({existingCount} found). No changes needed.",
                Type = InitializationOperationType.DataCheck,
                EstimatedRowsAffected = 0
            });
        }
        else
        {
            foreach (var (name, _) in DefaultCategories)
            {
                operations.Add(new InitializationOperation
                {
                    StepName = Name,
                    Description = $"Insert todo category: '{name}'",
                    Type = InitializationOperationType.DataChange,
                    EstimatedRowsAffected = 1
                });
            }
        }

        return Task.FromResult(Result<IReadOnlyList<InitializationOperation>>.Success(operations));
    }

    /// <summary>Counts existing todo categories in the database.</summary>
    private static int GetExistingCount(IInitializationContext context)
    {
        // Ensure: Connection is open for reads.
        if (context.Connection.State != ConnectionState.Open)
            context.Connection.Open();

        // Query: Count existing categories using raw SQL.
        using var command = context.Connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM todo_categories";
        command.CommandTimeout = 30;

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    /// <summary>Creates a parameter for the given command.</summary>
    private static IDbDataParameter CreateParam(IDbCommand command, string name, object? value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        return param;
    }
}
