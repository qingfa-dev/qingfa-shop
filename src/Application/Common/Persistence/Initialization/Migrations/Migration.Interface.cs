namespace Application.Common.Persistence.Initialization.Migrations;

using Application.Common.Persistence.Initialization;

/// <summary>
/// Specialized step for database migrations.
/// Inherits common metadata and execution from <see cref="IInitializationStep"/>.
/// </summary>
public interface IPersistenceMigrator : IInitializationStep
{
    /// <summary>
    /// Gets the migration version (e.g., "20240101000000").
    /// Used for version tracking and ordering.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the SHA-256 checksum of the migration script.
    /// Used to detect drift between applied and current migration.
    /// </summary>
    string Checksum { get; }

    /// <summary>
    /// Determines if an exception is transient (retryable).
    /// This is a cross-cutting concern; consider moving to a retry policy decorator.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns>True if the operation should be retried.</returns>
    bool IsTransient(Exception exception);
}