using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Infrastructure.Persistence.EntityFramework.UnitOfWorks;

/// <summary>The category of a persistence failure, independent of the outcome type that reports it.</summary>
internal enum PersistenceFailureKind
{
    ConcurrencyConflict,
    DuplicateKey,
    ForeignKeyViolation,
    CheckConstraintViolation,
    NotNullViolation,
    DeadlockDetected,
    SerializationFailure,
    ReadOnlyViolation,
    TransactionRollbackOnly,
    ConnectionFailed,
    ConnectionRefused,
    ConnectionLost,
    LoginFailed,
    Timeout,
    Cancelled,
    SaveChangesFailed,
    ExpressionTranslationFailed,
    InvalidOperation,
    Unknown,
}

/// <summary>
/// A classified persistence failure. <paramref name="Detail"/> carries the constraint,
/// column, or provider message when the category has one. The exception type and message
/// travel with the failure so <see cref="PersistenceOutcome"/> can attach them as metadata.
/// </summary>
internal readonly record struct PersistenceFailure(
    PersistenceFailureKind Kind,
    string? Detail = null,
    string? ExceptionType = null,
    string? ExceptionMessage = null);

/// <summary>
/// Single place that maps EF Core / Npgsql exceptions to a provider-neutral failure category.
/// Callers materialize the category through <see cref="PersistenceOutcome"/>, which resolves
/// each category to its owning outcome type.
/// </summary>
internal static class PersistenceExceptionTranslator
{
    /// <summary>Classifies any EF Core / Npgsql exception.</summary>
    /// <param name="exception">The exception to classify.</param>
    /// <param name="cancellationToken">The caller's token, used to distinguish cancellation from a timeout.</param>
    /// <returns>The classified failure, including the originating exception metadata.</returns>
    public static PersistenceFailure Classify(
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        PersistenceFailure failure = ClassifyKind(exception, cancellationToken);

        // Trace: keep the originating exception available to the materialized Error.
        return failure with
        {
            ExceptionType = exception.GetType().FullName,
            ExceptionMessage = exception.Message,
        };
    }

    private static PersistenceFailure ClassifyKind(
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Priority: concurrency derives from DbUpdateException, check it before walking the chain.
        if (exception is DbUpdateConcurrencyException)
            return new(PersistenceFailureKind.ConcurrencyConflict);

        // Walk the whole chain; PostgresException is not always the direct inner exception.
        if (FindInChain<PostgresException>(exception) is { } pgEx)
            return ClassifyPostgres(pgEx);

        if (exception is DbUpdateException dbEx)
            return ClassifyDbUpdate(dbEx);

        // Cancellation: caller's token => Cancelled; DB-side cancel => Timeout.
        if (exception is OperationCanceledException oce)
        {
            return cancellationToken.IsCancellationRequested || oce.CancellationToken.IsCancellationRequested
                ? new(PersistenceFailureKind.Cancelled)
                : new(PersistenceFailureKind.Timeout);
        }

        // Npgsql wraps command timeouts as NpgsqlException -> TimeoutException.
        if (FindInChain<TimeoutException>(exception) is not null)
            return new(PersistenceFailureKind.Timeout);

        if (exception is InvalidOperationException ioe)
        {
            string message = ioe.Message;

            if (message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return new(PersistenceFailureKind.Timeout);

            if (message.Contains("translate", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("LINQ", StringComparison.OrdinalIgnoreCase))
                return new(PersistenceFailureKind.ExpressionTranslationFailed, message);

            return new(PersistenceFailureKind.InvalidOperation, message);
        }

        // Anything unrecognized is an unknown store failure, not a write failure.
        return new(PersistenceFailureKind.Unknown, exception.Message);
    }

    /// <summary>Searches the exception chain for a specific exception type.</summary>
    private static TException? FindInChain<TException>(Exception? exception)
        where TException : Exception
    {
        while (exception is not null)
        {
            if (exception is TException match)
                return match;

            exception = exception.InnerException;
        }

        return null;
    }

    /// <summary>
    /// Translates PostgreSQL SQLSTATE codes to failure categories. Constraint and column
    /// names are preserved as detail so callers can name the violated rule.
    /// </summary>
    private static PersistenceFailure ClassifyPostgres(PostgresException exception) => exception.SqlState switch
    {
        // Integrity constraint violations
        PostgresErrorCodes.UniqueViolation => new(PersistenceFailureKind.DuplicateKey, exception.ConstraintName),
        PostgresErrorCodes.ForeignKeyViolation => new(PersistenceFailureKind.ForeignKeyViolation, exception.ConstraintName),
        PostgresErrorCodes.CheckViolation => new(PersistenceFailureKind.CheckConstraintViolation, exception.ConstraintName),
        PostgresErrorCodes.NotNullViolation => new(PersistenceFailureKind.NotNullViolation, exception.ColumnName),

        // Concurrency
        PostgresErrorCodes.DeadlockDetected => new(PersistenceFailureKind.DeadlockDetected),
        PostgresErrorCodes.SerializationFailure => new(PersistenceFailureKind.SerializationFailure),

        // Transaction state
        PostgresErrorCodes.ReadOnlySqlTransaction => new(PersistenceFailureKind.ReadOnlyViolation),
        PostgresErrorCodes.InFailedSqlTransaction => new(PersistenceFailureKind.TransactionRollbackOnly),

        // Timeouts (statement_timeout, lock_timeout, cancelled statements)
        PostgresErrorCodes.QueryCanceled => new(PersistenceFailureKind.Timeout),
        PostgresErrorCodes.LockNotAvailable => new(PersistenceFailureKind.Timeout),

        // Connection exceptions
        PostgresErrorCodes.SqlClientUnableToEstablishSqlConnection => new(PersistenceFailureKind.ConnectionRefused),
        PostgresErrorCodes.ConnectionFailure => new(PersistenceFailureKind.ConnectionLost),
        PostgresErrorCodes.ConnectionDoesNotExist => new(PersistenceFailureKind.ConnectionLost),
        PostgresErrorCodes.ConnectionException => new(PersistenceFailureKind.ConnectionFailed),
        PostgresErrorCodes.SqlServerRejectedEstablishmentOfSqlConnection => new(PersistenceFailureKind.ConnectionFailed),
        PostgresErrorCodes.AdminShutdown => new(PersistenceFailureKind.ConnectionFailed),
        PostgresErrorCodes.CrashShutdown => new(PersistenceFailureKind.ConnectionFailed),
        PostgresErrorCodes.CannotConnectNow => new(PersistenceFailureKind.ConnectionFailed),
        PostgresErrorCodes.TooManyConnections => new(PersistenceFailureKind.ConnectionFailed),

        // Authentication
        PostgresErrorCodes.InvalidAuthorizationSpecification => new(PersistenceFailureKind.LoginFailed),
        PostgresErrorCodes.InvalidPassword => new(PersistenceFailureKind.LoginFailed),

        _ => new(PersistenceFailureKind.Unknown),
    };

    /// <summary>
    /// Fallback for DbUpdateException when no PostgresException is in the chain.
    /// Keyword matching infers the constraint type violated.
    /// </summary>
    private static PersistenceFailure ClassifyDbUpdate(DbUpdateException exception)
    {
        string message = exception.Message.ToLowerInvariant();

        if (message.Contains("unique") || message.Contains("duplicate"))
            return new(PersistenceFailureKind.DuplicateKey);

        if (message.Contains("foreign key") || message.Contains("reference constraint"))
            return new(PersistenceFailureKind.ForeignKeyViolation);

        if (message.Contains("check constraint"))
            return new(PersistenceFailureKind.CheckConstraintViolation);

        if (message.Contains("not null") || message.Contains("null value"))
            return new(PersistenceFailureKind.NotNullViolation);

        return new(PersistenceFailureKind.SaveChangesFailed, exception.Message);
    }
}
