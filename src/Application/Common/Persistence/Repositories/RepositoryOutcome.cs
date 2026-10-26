namespace Application.Common.Persistence.Repositories;

using Core.Common.Monad.Results;

/// <summary>Repository validation failures.</summary>
public static class RepositoryOutcome
{
    public static class Failure
    {
        // ---- Page ----
        public static Error PageSizeTooSmall(string parameterName = "pageSize") => Error.Validation(
            Codes.PageSizeTooSmall,
            $"{parameterName} must be at least {RepositoryConstant.Limits.MinPageSize}.");

        public static Error PageSizeTooLarge(string parameterName = "pageSize") => Error.Validation(
            Codes.PageSizeTooLarge,
            $"{parameterName} cannot exceed {RepositoryConstant.Limits.MaxPageSize}.");

        // ---- Batch ----
        public static Error BatchSizeTooSmall(string parameterName = "batchSize") => Error.Validation(
            Codes.BatchSizeTooSmall,
            $"{parameterName} must be at least {RepositoryConstant.Limits.MinBatchSize}.");

        public static Error BatchSizeTooLarge(string parameterName = "batchSize") => Error.Validation(
            Codes.BatchSizeTooLarge,
            $"{parameterName} cannot exceed {RepositoryConstant.Limits.MaxBatchSize}.");

        // ---- Identifiers ----
        public static Error TableNameTooLong(string parameterName = "tableName") => Error.Validation(
            Codes.TableNameTooLong,
            $"{parameterName} cannot exceed {RepositoryConstant.Limits.MaxTableNameLength} characters.");

        public static Error ColumnNameTooLong(string parameterName = "columnName") => Error.Validation(
            Codes.ColumnNameTooLong,
            $"{parameterName} cannot exceed {RepositoryConstant.Limits.MaxColumnNameLength} characters.");

        public static Error IndexNameTooLong(string parameterName = "indexName") => Error.Validation(
            Codes.IndexNameTooLong,
            $"{parameterName} cannot exceed {RepositoryConstant.Limits.MaxIndexNameLength} characters.");

        public static Error ConstraintNameTooLong(string parameterName = "constraintName") => Error.Validation(
            Codes.ConstraintNameTooLong,
            $"{parameterName} cannot exceed {RepositoryConstant.Limits.MaxConstraintNameLength} characters.");

        public static Error ParameterNameTooLong(string parameterName = "parameterName") => Error.Validation(
            Codes.ParameterNameTooLong,
            $"{parameterName} cannot exceed {RepositoryConstant.Limits.MaxParameterNameLength} characters.");

        // ---- Command ----
        public static Error CommandTimeout(string parameterName = "command") => Error.Validation(
            Codes.CommandTimeout, $"{parameterName} timed out.");

        public static Error CommandCancelled(string parameterName = "command") => Error.Validation(
            Codes.CommandCancelled, $"{parameterName} was cancelled.");

        public static Error CommandInvalid(string parameterName = "command", string? reason = null) => Error.Validation(
            Codes.CommandInvalid,
            $"{parameterName} is invalid or malformed.{(!string.IsNullOrEmpty(reason) ? $" Reason: {reason}" : "")}");

        // ---- Connection ----
        public static Error ConnectionStringInvalid(string parameterName = "connectionString") => Error.Validation(
            Codes.ConnectionStringInvalid, $"{parameterName} is missing or invalid.");

        public static Error PoolExhausted(string parameterName = "pool") => Error.Validation(
            Codes.PoolExhausted, $"The connection {parameterName} is exhausted.");

        public static Error ConnectionBroken(string parameterName = "connection") => Error.Validation(
            Codes.ConnectionBroken, $"{parameterName} was broken during operation.");

        // ---- Schema ----
        public static Error SchemaMismatch(string parameterName = "schema") => Error.Validation(
            Codes.SchemaMismatch, $"The database {parameterName} does not match the expected model.");

        public static Error TableMissing(string parameterName = "table") => Error.Validation(
            Codes.TableMissing, $"A required {parameterName} is missing.");

        public static Error ColumnMissing(string parameterName = "column") => Error.Validation(
            Codes.ColumnMissing, $"A required {parameterName} is missing.");

        public static Error IndexMissing(string parameterName = "index") => Error.Validation(
            Codes.IndexMissing, $"A required {parameterName} is missing.");

        public static Error ConstraintViolation(string parameterName = "constraint", string? constraintName = null) => Error.Validation(
            Codes.ConstraintViolation,
            $"A {parameterName} violation occurred.{(!string.IsNullOrEmpty(constraintName) ? $" Constraint: {constraintName}" : "")}");

        // ---- Bulk Operations ----
        public static Error BulkInsertFailed(string parameterName = "bulkInsert", string? reason = null) => Error.Validation(
            Codes.BulkInsertFailed,
            $"Bulk {parameterName} failed.{(!string.IsNullOrEmpty(reason) ? $" Reason: {reason}" : "")}");

        public static Error BulkUpdateFailed(string parameterName = "bulkUpdate", string? reason = null) => Error.Validation(
            Codes.BulkUpdateFailed,
            $"Bulk {parameterName} failed.{(!string.IsNullOrEmpty(reason) ? $" Reason: {reason}" : "")}");

        public static Error BulkDeleteFailed(string parameterName = "bulkDelete", string? reason = null) => Error.Validation(
            Codes.BulkDeleteFailed,
            $"Bulk {parameterName} failed.{(!string.IsNullOrEmpty(reason) ? $" Reason: {reason}" : "")}");

        public static Error BulkCancelled(string parameterName = "bulk") => Error.Validation(
            Codes.BulkCancelled, $"Bulk {parameterName} operation was cancelled.");

        public static Error BatchSizeExceeded(int requested, int max, string parameterName = "batchSize") => Error.Validation(
            Codes.BatchSizeExceeded,
            $"Requested {parameterName} {requested} exceeds the maximum of {max}.");

        // ---- Concurrency ----
        public static Error ConcurrencyTokenMissing(string parameterName = "concurrencyToken") => Error.Validation(
            Codes.ConcurrencyTokenMissing, $"Optimistic {parameterName} is missing.");

        public static Error ConcurrencyTokenInvalid(string parameterName = "concurrencyToken") => Error.Validation(
            Codes.ConcurrencyTokenInvalid, $"Optimistic {parameterName} format is invalid.");

        // ---- Provider ----
        public static Error ProviderNotRegistered(string providerName) => Error.Validation(
            Codes.ProviderNotRegistered, $"The persistence provider '{providerName}' is not registered.");

        public static Error ProviderNotSupported(string providerName) => Error.Validation(
            Codes.ProviderNotSupported, $"The persistence provider '{providerName}' is not supported.");

        public static Error ProviderFeatureUnavailable(string feature, string providerName) => Error.Validation(
            Codes.ProviderFeatureUnavailable,
            $"Feature '{feature}' is not available for provider '{providerName}'.");
    }

    /// <summary>Stable codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        // ---- Page ----
        public const string PageSizeTooSmall        = "Repository.PageSize.TooSmall";
        public const string PageSizeTooLarge        = "Repository.PageSize.TooLarge";

        // ---- Batch ----
        public const string BatchSizeTooSmall       = "Repository.BatchSize.TooSmall";
        public const string BatchSizeTooLarge       = "Repository.BatchSize.TooLarge";

        // ---- Identifiers ----
        public const string TableNameTooLong        = "Repository.Identifier.TableNameTooLong";
        public const string ColumnNameTooLong       = "Repository.Identifier.ColumnNameTooLong";
        public const string IndexNameTooLong        = "Repository.Identifier.IndexNameTooLong";
        public const string ConstraintNameTooLong   = "Repository.Identifier.ConstraintNameTooLong";
        public const string ParameterNameTooLong    = "Repository.Identifier.ParameterNameTooLong";

        // ---- Command ----
        public const string CommandTimeout          = "Repository.Command.Timeout";
        public const string CommandCancelled        = "Repository.Command.Cancelled";
        public const string CommandInvalid          = "Repository.Command.Invalid";

        // ---- Connection ----
        public const string ConnectionStringInvalid = "Repository.Connection.StringInvalid";
        public const string PoolExhausted           = "Repository.Connection.PoolExhausted";
        public const string ConnectionBroken        = "Repository.Connection.Broken";

        // ---- Schema ----
        public const string SchemaMismatch          = "Repository.Schema.Mismatch";
        public const string TableMissing            = "Repository.Schema.TableMissing";
        public const string ColumnMissing           = "Repository.Schema.ColumnMissing";
        public const string IndexMissing            = "Repository.Schema.IndexMissing";
        public const string ConstraintViolation     = "Repository.Schema.ConstraintViolation";

        // ---- Bulk Operations ----
        public const string BulkInsertFailed        = "Repository.Bulk.InsertFailed";
        public const string BulkUpdateFailed        = "Repository.Bulk.UpdateFailed";
        public const string BulkDeleteFailed        = "Repository.Bulk.DeleteFailed";
        public const string BulkCancelled           = "Repository.Bulk.Cancelled";
        public const string BatchSizeExceeded       = "Repository.Batch.SizeExceeded";

        // ---- Concurrency ----
        public const string ConcurrencyTokenMissing = "Repository.Concurrency.TokenMissing";
        public const string ConcurrencyTokenInvalid = "Repository.Concurrency.TokenInvalid";

        // ---- Provider ----
        public const string ProviderNotRegistered   = "Repository.Provider.NotRegistered";
        public const string ProviderNotSupported    = "Repository.Provider.NotSupported";
        public const string ProviderFeatureUnavailable = "Repository.Provider.FeatureUnavailable";
    }
}