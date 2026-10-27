namespace Application.Common.Persistence.Repositories;

/// <summary>
/// Repository-layer failures. Covers entity, write, delete, query, store, and validation outcomes.
/// Use <see cref="UnitOfWorks.TransactionOutcome"/> for transaction-specific failures and
/// <see cref="UnitOfWorks.UnitOfWorkOutcome"/> for unit-of-work-specific failures.
/// </summary>
public static class RepositoryOutcome
{
    public static class Failure
    {
        // ---- Entity ----
        public static Error NotFound => Error.NotFound(
            Codes.EntityNotFound, "The requested entity does not exist.");

        public static Error NotFoundFor<T>(object id) => Error.NotFound(
            Codes.EntityNotFound, $"The '{typeof(T).Name}' with id '{id}' does not exist.");

        public static Error EntityRequired => Error.UnprocessableEntity(
            Codes.EntityRequired, "An entity is required.");

        public static Error EntityRequiredFor<T>() => Error.UnprocessableEntity(
            Codes.EntityRequired, $"An instance of '{typeof(T).Name}' is required.");

        /// <summary>Mapping (Dapper: map not registered; EF Core: translation missing)</summary>
        public static Error MappingMissing(Type type) => Error.InternalServerError(
            Codes.MappingMissing, $"No query map is registered for '{type.Name}'.");

        // ---- Write ----
        public static Error DuplicateKey => Error.Conflict(
            Codes.DuplicateKey, "The entity conflicts with an existing row.");

        public static Error DuplicateKeyFor(string constraint) => Error.Conflict(
            Codes.DuplicateKey,
            $"The entity conflicts with an existing row (constraint '{constraint}').");

        public static Error DuplicateKeyFor<T>() => Error.Conflict(
            Codes.DuplicateKey, $"'{typeof(T).Name}' conflicts with an existing row.");

        public static Error ForeignKeyViolation => Error.Conflict(
            Codes.ForeignKeyViolation, "The operation violates a foreign key constraint.");

        public static Error ForeignKeyViolationFor(string constraint) => Error.Conflict(
            Codes.ForeignKeyViolation,
            $"The operation violates a foreign key constraint ('{constraint}').");

        public static Error CheckConstraintViolation => Error.Conflict(
            Codes.CheckConstraintViolation, "The operation violates a check constraint.");

        public static Error CheckConstraintViolationFor(string constraint) => Error.Conflict(
            Codes.CheckConstraintViolation,
            $"The operation violates a check constraint ('{constraint}').");

        public static Error NotNullViolation => Error.Conflict(
            Codes.NotNullViolation, "A required column cannot accept a null value.");

        public static Error NotNullViolationFor(string column) => Error.Conflict(
            Codes.NotNullViolation, $"The column '{column}' cannot accept a null value.");

        public static Error ConcurrencyConflict => Error.Conflict(
            Codes.ConcurrencyConflict, "The entity was modified by another request.");

        public static Error ConcurrencyConflictFor<T>(object? key = null) => Error.Conflict(
            Codes.ConcurrencyConflict,
            key is null
                ? $"'{typeof(T).Name}' was modified by another request."
                : $"'{typeof(T).Name}' with key '{key}' was modified by another request.");

        public static Error DeadlockDetected => Error.Conflict(
            Codes.DeadlockDetected, "The transaction was deadlocked and cannot proceed.");

        public static Error SerializationFailure => Error.Conflict(
            Codes.SerializationFailure,
            "The transaction failed due to a serialization conflict. Retry the operation.");

        /// <summary>SaveChanges failed for a non-conflict, non-unique reason</summary>
        public static Error SaveChangesFailed(string reason) => Error.InternalServerError(
            Codes.SaveChangesFailed, $"SaveChanges failed: {reason}");

        // ---- Delete ----
        public static Error DeleteSoftDeletable => Error.Conflict(
            Codes.DeleteSoftDeletable,
            "A live soft-deletable aggregate cannot be hard-deleted.");

        public static Error DeleteSoftDeletableFor<T>() => Error.Conflict(
            Codes.DeleteSoftDeletable,
            $"A live soft-deletable '{typeof(T).Name}' cannot be hard-deleted.");

        // ---- Query ----
        public static Error ShapeMismatch => Error.BadRequest(
            Codes.QueryShapeMismatch,
            "Use the entity read method for entity queries and the tabular method for projection queries.");

        public static Error QueryInvalid(string reason) => Error.BadRequest(
            Codes.QueryInvalid, reason);

        public static Error QueryTooComplex(int maxDepth) => Error.BadRequest(
            Codes.QueryTooComplex,
            $"The query exceeds the maximum supported expression depth of {maxDepth}.");

        public static Error PageSizeExceeded(int requested, int max) => Error.BadRequest(
            Codes.PageSizeExceeded,
            $"Requested page size {requested} exceeds the adapter limit of {max}.");

        /// <summary>Dapper-specific: bad parameter binding, wrong column count</summary>
        public static Error ParameterInvalid(string name) => Error.BadRequest(
            Codes.ParameterInvalid, $"The parameter '{name}' cannot be bound.");

        public static Error ResultSetMismatch(int expected, int actual) => Error.BadRequest(
            Codes.ResultSetMismatch,
            $"The result set has {actual} columns but {expected} were expected.");

        /// <summary>EF Core-specific: detached entity used where tracked is required</summary>
        public static Error ChangeTrackingRequired(Type type) => Error.Conflict(
            Codes.ChangeTrackingRequired,
            $"'{type.Name}' is not tracked; attach it before updating.");

        public static Error ExpressionTranslationFailed(string reason) => Error.InternalServerError(
            Codes.ExpressionTranslationFailed, $"Could not translate the query expression: {reason}");

        public static Error InvalidOperation => Error.InternalServerError(
            Codes.InvalidOperation, "The operation is invalid in its current state.");

        public static Error InvalidOperationFor(string reason) => Error.InternalServerError(
            Codes.InvalidOperation, $"The operation is invalid: {reason}");

        // ---- Store ----
        public static Error Unavailable => Error.ServiceUnavailable(
            Codes.StoreUnavailable, "The persistence store is unavailable.");

        public static Error ConnectionFailed => Error.ServiceUnavailable(
            Codes.ConnectionFailed, "The connection to the persistence store failed.");

        public static Error ConnectionRefused => Error.ServiceUnavailable(
            Codes.ConnectionRefused, "The persistence store refused the connection.");

        public static Error ConnectionLost => Error.ServiceUnavailable(
            Codes.ConnectionLost, "The connection to the persistence store was lost.");

        public static Error LoginFailed => Error.ServiceUnavailable(
            Codes.LoginFailed, "Authentication to the persistence store failed.");

        public static Error Timeout => Error.ServiceUnavailable(
            Codes.StoreTimeout, "The persistence operation timed out.");

        public static Error StoreCommandTimeout(int seconds) => Error.ServiceUnavailable(
            Codes.StoreTimeout, $"The command exceeded the {seconds}s timeout.");

        public static Error Cancelled => Error.ServiceUnavailable(
            Codes.StoreCancelled, "The persistence operation was cancelled.");

        public static Error Unknown => Error.InternalServerError(
            Codes.StoreUnknown, "The persistence store failed.");

        // ---- Validation: Page ----
        public static Error PageSizeTooSmall(string parameterName = "pageSize") => Error.Validation(
            Codes.PageSizeTooSmall,
            $"{parameterName} must be at least {RepositoryConstant.Limits.MinPageSize}.");

        public static Error PageSizeTooLarge(string parameterName = "pageSize") => Error.Validation(
            Codes.PageSizeTooLarge,
            $"{parameterName} cannot exceed {RepositoryConstant.Limits.MaxPageSize}.");

        // ---- Validation: Batch ----
        public static Error BatchSizeTooSmall(string parameterName = "batchSize") => Error.Validation(
            Codes.BatchSizeTooSmall,
            $"{parameterName} must be at least {RepositoryConstant.Limits.MinBatchSize}.");

        public static Error BatchSizeTooLarge(string parameterName = "batchSize") => Error.Validation(
            Codes.BatchSizeTooLarge,
            $"{parameterName} cannot exceed {RepositoryConstant.Limits.MaxBatchSize}.");

        // ---- Validation: Identifiers ----
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

        // ---- Validation: Command ----
        public static Error CommandTimeout(string parameterName = "command") => Error.Validation(
            Codes.CommandTimeout, $"{parameterName} timed out.");

        public static Error CommandCancelled(string parameterName = "command") => Error.Validation(
            Codes.CommandCancelled, $"{parameterName} was cancelled.");

        public static Error CommandInvalid(string parameterName = "command", string? reason = null) => Error.Validation(
            Codes.CommandInvalid,
            $"{parameterName} is invalid or malformed.{(!string.IsNullOrEmpty(reason) ? $" Reason: {reason}" : "")}");

        // ---- Validation: Connection ----
        public static Error ConnectionStringInvalid(string parameterName = "connectionString") => Error.Validation(
            Codes.ConnectionStringInvalid, $"{parameterName} is missing or invalid.");

        public static Error PoolExhausted(string parameterName = "pool") => Error.Validation(
            Codes.PoolExhausted, $"The connection {parameterName} is exhausted.");

        public static Error ConnectionBroken(string parameterName = "connection") => Error.Validation(
            Codes.ConnectionBroken, $"{parameterName} was broken during operation.");

        // ---- Validation: Schema ----
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

        // ---- Validation: Bulk Operations ----
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

        // ---- Validation: Concurrency ----
        public static Error ConcurrencyTokenMissing(string parameterName = "concurrencyToken") => Error.Validation(
            Codes.ConcurrencyTokenMissing, $"Optimistic {parameterName} is missing.");

        public static Error ConcurrencyTokenInvalid(string parameterName = "concurrencyToken") => Error.Validation(
            Codes.ConcurrencyTokenInvalid, $"Optimistic {parameterName} format is invalid.");

        // ---- Validation: Provider ----
        public static Error ProviderNotRegistered(string providerName) => Error.Validation(
            Codes.ProviderNotRegistered, $"The persistence provider '{providerName}' is not registered.");

        public static Error ProviderNotSupported(string providerName) => Error.Validation(
            Codes.ProviderNotSupported, $"The persistence provider '{providerName}' is not supported.");

        public static Error ProviderFeatureUnavailable(string feature, string providerName) => Error.Validation(
            Codes.ProviderFeatureUnavailable,
            $"Feature '{feature}' is not available for provider '{providerName}'.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        // ---- Entity ----
        public const string EntityNotFound = "Repository.Entity.NotFound";
        public const string EntityRequired = "Repository.Entity.Required";
        public const string MappingMissing = "Repository.Mapping.Missing";

        // ---- Write ----
        public const string DuplicateKey         = "Repository.Entity.DuplicateKey";
        public const string ForeignKeyViolation  = "Repository.Entity.ForeignKeyViolation";
        public const string CheckConstraintViolation = "Repository.Entity.CheckConstraintViolation";
        public const string NotNullViolation     = "Repository.Entity.NotNullViolation";
        public const string ConcurrencyConflict  = "Repository.Concurrency.Conflict";
        public const string DeadlockDetected     = "Repository.Concurrency.Deadlock";
        public const string SerializationFailure = "Repository.Concurrency.SerializationFailure";
        public const string SaveChangesFailed    = "Repository.Write.SaveChangesFailed";

        // ---- Delete ----
        public const string DeleteSoftDeletable = "Repository.Delete.SoftDeletable";

        // ---- Query ----
        public const string QueryShapeMismatch        = "Repository.Query.ShapeMismatch";
        public const string QueryInvalid              = "Repository.Query.Invalid";
        public const string QueryTooComplex           = "Repository.Query.TooComplex";
        public const string PageSizeExceeded          = "Repository.Query.PageSizeExceeded";
        public const string ParameterInvalid          = "Repository.Query.ParameterInvalid";
        public const string ResultSetMismatch         = "Repository.Query.ResultSetMismatch";
        public const string ChangeTrackingRequired    = "Repository.Query.ChangeTrackingRequired";
        public const string ExpressionTranslationFailed = "Repository.Query.ExpressionTranslationFailed";
        public const string InvalidOperation          = "Repository.Operation.Invalid";

        // ---- Store ----
        public const string StoreUnavailable = "Repository.Store.Unavailable";
        public const string ConnectionFailed = "Repository.Store.ConnectionFailed";
        public const string ConnectionRefused = "Repository.Store.ConnectionRefused";
        public const string ConnectionLost = "Repository.Store.ConnectionLost";
        public const string LoginFailed = "Repository.Store.LoginFailed";
        public const string StoreTimeout = "Repository.Store.Timeout";
        public const string StoreCancelled = "Repository.Store.Cancelled";
        public const string StoreUnknown = "Repository.Store.Unknown";

        // ---- Validation: Page ----
        public const string PageSizeTooSmall        = "Repository.PageSize.TooSmall";
        public const string PageSizeTooLarge        = "Repository.PageSize.TooLarge";

        // ---- Validation: Batch ----
        public const string BatchSizeTooSmall       = "Repository.BatchSize.TooSmall";
        public const string BatchSizeTooLarge       = "Repository.BatchSize.TooLarge";

        // ---- Validation: Identifiers ----
        public const string TableNameTooLong        = "Repository.Identifier.TableNameTooLong";
        public const string ColumnNameTooLong       = "Repository.Identifier.ColumnNameTooLong";
        public const string IndexNameTooLong        = "Repository.Identifier.IndexNameTooLong";
        public const string ConstraintNameTooLong   = "Repository.Identifier.ConstraintNameTooLong";
        public const string ParameterNameTooLong    = "Repository.Identifier.ParameterNameTooLong";

        // ---- Validation: Command ----
        public const string CommandTimeout          = "Repository.Command.Timeout";
        public const string CommandCancelled        = "Repository.Command.Cancelled";
        public const string CommandInvalid          = "Repository.Command.Invalid";

        // ---- Validation: Connection ----
        public const string ConnectionStringInvalid = "Repository.Connection.StringInvalid";
        public const string PoolExhausted           = "Repository.Connection.PoolExhausted";
        public const string ConnectionBroken        = "Repository.Connection.Broken";

        // ---- Validation: Schema ----
        public const string SchemaMismatch          = "Repository.Schema.Mismatch";
        public const string TableMissing            = "Repository.Schema.TableMissing";
        public const string ColumnMissing           = "Repository.Schema.ColumnMissing";
        public const string IndexMissing            = "Repository.Schema.IndexMissing";
        public const string ConstraintViolation     = "Repository.Schema.ConstraintViolation";

        // ---- Validation: Bulk Operations ----
        public const string BulkInsertFailed        = "Repository.Bulk.InsertFailed";
        public const string BulkUpdateFailed        = "Repository.Bulk.UpdateFailed";
        public const string BulkDeleteFailed        = "Repository.Bulk.DeleteFailed";
        public const string BulkCancelled           = "Repository.Bulk.Cancelled";
        public const string BatchSizeExceeded       = "Repository.Batch.SizeExceeded";

        // ---- Validation: Concurrency ----
        public const string ConcurrencyTokenMissing = "Repository.Concurrency.TokenMissing";
        public const string ConcurrencyTokenInvalid = "Repository.Concurrency.TokenInvalid";

        // ---- Validation: Provider ----
        public const string ProviderNotRegistered   = "Repository.Provider.NotRegistered";
        public const string ProviderNotSupported    = "Repository.Provider.NotSupported";
        public const string ProviderFeatureUnavailable = "Repository.Provider.FeatureUnavailable";
    }
}