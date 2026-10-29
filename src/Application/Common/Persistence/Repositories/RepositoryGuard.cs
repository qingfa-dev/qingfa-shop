namespace Application.Common.Persistence.Repositories;
/// <summary>
/// Guard utilities for validating repository-related values against <see cref="RepositoryConstant"/> limits.
/// </summary>
/// <remarks>
/// Returns <see cref="Result"/> instead of throwing, enabling monadic validation composition.
/// </remarks>
public static class RepositoryGuard
{
    #region Entity Validation

    /// <summary>
    /// Validates that an entity is not null.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to validate.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with entity required error.</returns>
    public static Result ValidateEntity<TEntity>(TEntity? entity) where TEntity : class
    {
        if (entity is null)
        {
            return Result.Fail(RepositoryOutcome.Failure.EntityRequiredFor<TEntity>());
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that an entity collection is not null.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entities">The entity collection to validate.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with entity required error.</returns>
    public static Result ValidateEntityCollection<TEntity>(IEnumerable<TEntity>? entities) where TEntity : class
    {
        if (entities is null)
        {
            return Result.Fail(RepositoryOutcome.Failure.EntityRequiredFor<TEntity>());
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates that an ID value is not default/null.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="id">The ID to validate.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateId<TKey>(TKey? id)
    {
        if (id is null)
        {
            return Result.Fail(RepositoryOutcome.Failure.QueryInvalid("ID cannot be null."));
        }

        if (id is IComparable comparable && comparable.CompareTo(default(TKey)!) == 0)
        {
            return Result.Fail(RepositoryOutcome.Failure.QueryInvalid("ID cannot be a default value."));
        }

        return Result.Ok();
    }

    #endregion

    #region Pagination

    /// <summary>
    /// Validates a page size.
    /// </summary>
    /// <param name="pageSize">The page size to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidatePageSize(
        int pageSize,
        string parameterName = "pageSize")
    {
        if (pageSize < RepositoryConstant.Limits.MinPageSize)
        {
            return Result.Fail(RepositoryOutcome.Failure.PageSizeTooSmall(parameterName));
        }

        if (pageSize > RepositoryConstant.Limits.MaxPageSize)
        {
            return Result.Fail(RepositoryOutcome.Failure.PageSizeTooLarge(parameterName));
        }

        return Result.Ok();
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Validates a batch size.
    /// </summary>
    /// <param name="batchSize">The batch size to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateBatchSize(
        int batchSize,
        string parameterName = "batchSize")
    {
        if (batchSize < RepositoryConstant.Limits.MinBatchSize)
        {
            return Result.Fail(RepositoryOutcome.Failure.BatchSizeTooSmall(parameterName));
        }

        if (batchSize > RepositoryConstant.Limits.MaxBatchSize)
        {
            return Result.Fail(RepositoryOutcome.Failure.BatchSizeTooLarge(parameterName));
        }

        return Result.Ok();
    }

    #endregion

    #region Identifier Lengths

    /// <summary>
    /// Validates a table name length.
    /// </summary>
    /// <param name="tableName">The table name to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateTableName(
        string? tableName,
        string parameterName = "tableName")
    {
        if (tableName is null)
        {
            return Result.Ok(); // Null may be valid in some contexts
        }

        if (tableName.Length > RepositoryConstant.Limits.MaxTableNameLength)
        {
            return Result.Fail(RepositoryOutcome.Failure.TableNameTooLong(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates a column name length.
    /// </summary>
    /// <param name="columnName">The column name to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateColumnName(
        string? columnName,
        string parameterName = "columnName")
    {
        if (columnName is null)
        {
            return Result.Ok();
        }

        if (columnName.Length > RepositoryConstant.Limits.MaxColumnNameLength)
        {
            return Result.Fail(RepositoryOutcome.Failure.ColumnNameTooLong(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates an index name length.
    /// </summary>
    /// <param name="indexName">The index name to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateIndexName(
        string? indexName,
        string parameterName = "indexName")
    {
        if (indexName is null)
        {
            return Result.Ok();
        }

        if (indexName.Length > RepositoryConstant.Limits.MaxIndexNameLength)
        {
            return Result.Fail(RepositoryOutcome.Failure.IndexNameTooLong(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates a constraint name length.
    /// </summary>
    /// <param name="constraintName">The constraint name to validate.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateConstraintName(
        string? constraintName,
        string parameterName = "constraintName")
    {
        if (constraintName is null)
        {
            return Result.Ok();
        }

        if (constraintName.Length > RepositoryConstant.Limits.MaxConstraintNameLength)
        {
            return Result.Fail(RepositoryOutcome.Failure.ConstraintNameTooLong(parameterName));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Validates a parameter name length.
    /// </summary>
    /// <param name="parameterName">The parameter name to validate.</param>
    /// <param name="name">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateParameterName(
        string? parameterName,
        string name = "parameterName")
    {
        if (parameterName is null)
        {
            return Result.Ok();
        }

        if (parameterName.Length > RepositoryConstant.Limits.MaxParameterNameLength)
        {
            return Result.Fail(RepositoryOutcome.Failure.ParameterNameTooLong(name));
        }

        return Result.Ok();
    }

    #endregion

    #region Identifier Validation (Combined)

    /// <summary>
    /// Validates all identifier lengths at once.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columnName">The column name.</param>
    /// <param name="indexName">The index name.</param>
    /// <param name="constraintName">The constraint name.</param>
    /// <param name="parameterName">The parameter name.</param>
    /// <returns>A successful Result if all valid; otherwise a failed Result with combined errors.</returns>
    public static Result ValidateIdentifiers(
        string? tableName = null,
        string? columnName = null,
        string? indexName = null,
        string? constraintName = null,
        string? parameterName = null)
    {
        var results = new List<Result>
        {
            ValidateTableName(tableName, "tableName"),
            ValidateColumnName(columnName, "columnName"),
            ValidateIndexName(indexName, "indexName"),
            ValidateConstraintName(constraintName, "constraintName"),
            ValidateParameterName(parameterName, "parameterName")
        };

        return ResultExtension.Combine([.. results]);
    }

    #endregion

    #region Command Validation

    /// <summary>
    /// Validates a command timeout.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateCommandTimeout(
        int timeoutSeconds,
        string parameterName = "timeoutSeconds")
    {
        // Delegate to TransactionGuard for command timeout validation
        return TransactionGuard.ValidateCommandTimeout(timeoutSeconds, parameterName);
    }

    /// <summary>
    /// Creates a command timeout error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with command timeout error.</returns>
    public static Result CommandTimeout(
        string parameterName = "command")
        => Result.Fail(RepositoryOutcome.Failure.CommandTimeout(parameterName));

    /// <summary>
    /// Creates a command cancelled error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with command cancelled error.</returns>
    public static Result CommandCancelled(
        string parameterName = "command")
        => Result.Fail(RepositoryOutcome.Failure.CommandCancelled(parameterName));

    /// <summary>
    /// Creates a command invalid error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <param name="reason">Optional reason for the invalid command.</param>
    /// <returns>A failed Result with command invalid error.</returns>
    public static Result CommandInvalid(
        string parameterName = "command",
        string? reason = null)
        => Result.Fail(RepositoryOutcome.Failure.CommandInvalid(parameterName, reason));

    #endregion

    #region Connection Validation

    /// <summary>
    /// Creates a connection string invalid error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with connection string invalid error.</returns>
    public static Result ConnectionStringInvalid(
        string parameterName = "connectionString")
        => Result.Fail(RepositoryOutcome.Failure.ConnectionStringInvalid(parameterName));

    /// <summary>
    /// Creates a pool exhausted error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with pool exhausted error.</returns>
    public static Result PoolExhausted(
        string parameterName = "pool")
        => Result.Fail(RepositoryOutcome.Failure.PoolExhausted(parameterName));

    /// <summary>
    /// Creates a connection broken error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with connection broken error.</returns>
    public static Result ConnectionBroken(
        string parameterName = "connection")
        => Result.Fail(RepositoryOutcome.Failure.ConnectionBroken(parameterName));

    #endregion

    #region Schema Validation

    /// <summary>
    /// Creates a schema mismatch error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with schema mismatch error.</returns>
    public static Result SchemaMismatch(
        string parameterName = "schema")
        => Result.Fail(RepositoryOutcome.Failure.SchemaMismatch(parameterName));

    /// <summary>
    /// Creates a table missing error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with table missing error.</returns>
    public static Result TableMissing(
        string parameterName = "table")
        => Result.Fail(RepositoryOutcome.Failure.TableMissing(parameterName));

    /// <summary>
    /// Creates a column missing error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with column missing error.</returns>
    public static Result ColumnMissing(
        string parameterName = "column")
        => Result.Fail(RepositoryOutcome.Failure.ColumnMissing(parameterName));

    /// <summary>
    /// Creates an index missing error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with index missing error.</returns>
    public static Result IndexMissing(
        string parameterName = "index")
        => Result.Fail(RepositoryOutcome.Failure.IndexMissing(parameterName));

    /// <summary>
    /// Creates a constraint violation error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <param name="constraintName">Optional constraint name.</param>
    /// <returns>A failed Result with constraint violation error.</returns>
    public static Result ConstraintViolation(
        string parameterName = "constraint",
        string? constraintName = null)
        => Result.Fail(RepositoryOutcome.Failure.ConstraintViolation(parameterName, constraintName));

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Creates a bulk insert failed error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <param name="reason">Optional reason.</param>
    /// <returns>A failed Result with bulk insert failed error.</returns>
    public static Result BulkInsertFailed(
        string parameterName = "bulkInsert",
        string? reason = null)
        => Result.Fail(RepositoryOutcome.Failure.BulkInsertFailed(parameterName, reason));

    /// <summary>
    /// Creates a bulk update failed error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <param name="reason">Optional reason.</param>
    /// <returns>A failed Result with bulk update failed error.</returns>
    public static Result BulkUpdateFailed(
        string parameterName = "bulkUpdate",
        string? reason = null)
        => Result.Fail(RepositoryOutcome.Failure.BulkUpdateFailed(parameterName, reason));

    /// <summary>
    /// Creates a bulk delete failed error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <param name="reason">Optional reason.</param>
    /// <returns>A failed Result with bulk delete failed error.</returns>
    public static Result BulkDeleteFailed(
        string parameterName = "bulkDelete",
        string? reason = null)
        => Result.Fail(RepositoryOutcome.Failure.BulkDeleteFailed(parameterName, reason));

    /// <summary>
    /// Creates a bulk cancelled error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with bulk cancelled error.</returns>
    public static Result BulkCancelled(
        string parameterName = "bulk")
        => Result.Fail(RepositoryOutcome.Failure.BulkCancelled(parameterName));

    /// <summary>
    /// Validates batch size against maximum.
    /// </summary>
    /// <param name="requested">The requested batch size.</param>
    /// <param name="max">The maximum allowed batch size.</param>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A successful Result if valid; otherwise a failed Result with validation error.</returns>
    public static Result ValidateBatchSizeExceeded(
        int requested,
        int max,
        string parameterName = "batchSize")
    {
        if (requested > max)
        {
            return Result.Fail(RepositoryOutcome.Failure.BatchSizeExceeded(requested, max, parameterName));
        }

        return Result.Ok();
    }

    #endregion

    #region Concurrency

    /// <summary>
    /// Creates a concurrency token missing error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with concurrency token missing error.</returns>
    public static Result ConcurrencyTokenMissing(
        string parameterName = "concurrencyToken")
        => Result.Fail(RepositoryOutcome.Failure.ConcurrencyTokenMissing(parameterName));

    /// <summary>
    /// Creates a concurrency token invalid error.
    /// </summary>
    /// <param name="parameterName">The parameter name for error context.</param>
    /// <returns>A failed Result with concurrency token invalid error.</returns>
    public static Result ConcurrencyTokenInvalid(
        string parameterName = "concurrencyToken")
        => Result.Fail(RepositoryOutcome.Failure.ConcurrencyTokenInvalid(parameterName));

    #endregion

    #region Provider

    /// <summary>
    /// Creates a provider not registered error.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>A failed Result with provider not registered error.</returns>
    public static Result ProviderNotRegistered(
        string providerName)
        => Result.Fail(RepositoryOutcome.Failure.ProviderNotRegistered(providerName));

    /// <summary>
    /// Creates a provider not supported error.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>A failed Result with provider not supported error.</returns>
    public static Result ProviderNotSupported(
        string providerName)
        => Result.Fail(RepositoryOutcome.Failure.ProviderNotSupported(providerName));

    /// <summary>
    /// Creates a provider feature unavailable error.
    /// </summary>
    /// <param name="feature">The feature name.</param>
    /// <param name="providerName">The provider name.</param>
    /// <returns>A failed Result with provider feature unavailable error.</returns>
    public static Result ProviderFeatureUnavailable(
        string feature,
        string providerName)
        => Result.Fail(RepositoryOutcome.Failure.ProviderFeatureUnavailable(feature, providerName));

    #endregion
}