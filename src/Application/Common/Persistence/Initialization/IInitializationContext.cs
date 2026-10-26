namespace Application.Common.Persistence.Initialization;

using System.Data;
using Application.Common.Persistence.UnitOfWorks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Shared context provided to every initialization step.
/// Encapsulates DB access, logging, and cross-step state.
/// </summary>
public interface IInitializationContext
{
    /// <summary>
    /// Gets a database connection for the current initialization.
    /// The connection is managed by the pipeline (opened/closed automatically).
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// Gets the unit of work for transactional operations.
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Gets the logger for this initialization run.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets or sets arbitrary data shared between steps.
    /// Use for passing state (e.g., migration version, seeded counts).
    /// </summary>
    IDictionary<string, object> Items { get; }

    /// <summary>
    /// Gets the cancellation token for the entire initialization.
    /// </summary>
    CancellationToken CancellationToken { get; }
}

/// <summary>
/// Represents a single operation that would be or was performed during initialization.
/// </summary>
public readonly record struct InitializationOperation
{
    /// <summary>The step that performs this operation.</summary>
    public required string StepName { get; init; }

    /// <summary>Human-readable description of the operation.</summary>
    public required string Description { get; init; }

    /// <summary>Type of operation for filtering/display.</summary>
    public required InitializationOperationType Type { get; init; }

    /// <summary>Optional: SQL or command that would be executed.</summary>
    public string? CommandText { get; init; }

    /// <summary>Optional: Estimated rows affected.</summary>
    public long? EstimatedRowsAffected { get; init; }
}

/// <summary>Classification of initialization operations.</summary>
public enum InitializationOperationType
{
    /// <summary>Schema change (CREATE TABLE, ALTER, etc.).</summary>
    SchemaChange,

    /// <summary>Data manipulation (INSERT, UPDATE, DELETE).</summary>
    DataChange,

    /// <summary>Schema inspection (checking if table exists, etc.).</summary>
    SchemaCheck,

    /// <summary>Data inspection (counting rows, checking constraints).</summary>
    DataCheck,

    /// <summary>Custom/other operation.</summary>
    Custom,
}