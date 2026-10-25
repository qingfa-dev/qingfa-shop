namespace Application.Common.Persistence;

/// <summary>Neutral persistence failures. No provider exception type appears here —
/// neither <c>Microsoft.Data.SqlClient.SqlException</c> (Dapper) nor
/// <c>Microsoft.EntityFrameworkCore.DbUpdateException</c> (EF Core).</summary>
public static class PersistenceOutcome
{
    public static class Failure
    {
        // ---- Entity -------------------------------------------------------
        public static Error NotFound => Error.NotFound(
            Codes.EntityNotFound, "The requested entity does not exist.");

        public static Error NotFoundFor<T>(object id) => Error.NotFound(
            Codes.EntityNotFound, $"The '{typeof(T).Name}' with id '{id}' does not exist.");

        public static Error EntityRequired => Error.UnprocessableEntity(
            Codes.EntityRequired, "An entity is required.");

        public static Error EntityRequiredFor<T>() => Error.UnprocessableEntity(
            Codes.EntityRequired, $"An instance of '{typeof(T).Name}' is required.");

        // ---- Mapping (Dapper: map not registered; EF Core: translation missing) ----
        public static Error MappingMissing(Type type) => Error.InternalServerError(
            Codes.MappingMissing, $"No query map is registered for '{type.Name}'.");

        // ---- Query --------------------------------------------------------
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

        // Dapper-specific: bad parameter binding, wrong column count
        public static Error ParameterInvalid(string name) => Error.BadRequest(
            Codes.ParameterInvalid, $"The parameter '{name}' cannot be bound.");

        public static Error ResultSetMismatch(int expected, int actual) => Error.BadRequest(
            Codes.ResultSetMismatch,
            $"The result set has {actual} columns but {expected} were expected.");

        // EF Core-specific: detached entity used where tracked is required
        public static Error ChangeTrackingRequired(Type type) => Error.Conflict(
            Codes.ChangeTrackingRequired,
            $"'{type.Name}' is not tracked; attach it before updating.");

        // ---- Write --------------------------------------------------------
        public static Error DuplicateKey => Error.Conflict(
            Codes.DuplicateKey, "The entity conflicts with an existing row.");

        public static Error DuplicateKeyFor(string constraint) => Error.Conflict(
            Codes.DuplicateKey,
            $"The entity conflicts with an existing row (constraint '{constraint}').");

        public static Error DuplicateKeyFor<T>() => Error.Conflict(
            Codes.DuplicateKey, $"'{typeof(T).Name}' conflicts with an existing row.");

        public static Error ConcurrencyConflict => Error.Conflict(
            Codes.ConcurrencyConflict, "The entity was modified by another request.");

        public static Error ConcurrencyConflictFor<T>(object? key = null) => Error.Conflict(
            Codes.ConcurrencyConflict,
            key is null
                ? $"'{typeof(T).Name}' was modified by another request."
                : $"'{typeof(T).Name}' with key '{key}' was modified by another request.");

        // EF Core-specific: SaveChanges failed for a non-conflict, non-unique reason
        public static Error SaveChangesFailed(string reason) => Error.InternalServerError(
            Codes.SaveChangesFailed, $"SaveChanges failed: {reason}");

        // ---- Transaction --------------------------------------------------
        public static Error TransactionNested => Error.Conflict(
            Codes.TransactionNested, "A transaction is already open.");

        public static Error TransactionRequired => Error.Conflict(
            Codes.TransactionRequired, "No transaction is open.");

        public static Error TransactionAlreadyCompleted => Error.Conflict(
            Codes.TransactionAlreadyCompleted,
            "The transaction has already been committed or rolled back.");

        public static Error TransactionRollbackOnly => Error.Conflict(
            Codes.TransactionRollbackOnly,
            "The transaction is marked rollback-only and cannot be committed.");

        // ---- Delete -------------------------------------------------------
        public static Error DeleteSoftDeletable => Error.Conflict(
            Codes.DeleteSoftDeletable,
            "A live soft-deletable aggregate cannot be hard-deleted.");

        public static Error DeleteSoftDeletableFor<T>() => Error.Conflict(
            Codes.DeleteSoftDeletable,
            $"A live soft-deletable '{typeof(T).Name}' cannot be hard-deleted.");

        // ---- Infrastructure (both providers) ------------------------------
        public static Error Unavailable => Error.ServiceUnavailable(
            Codes.StoreUnavailable, "The persistence store is unavailable.");

        public static Error ConnectionFailed => Error.ServiceUnavailable(
            Codes.ConnectionFailed, "The connection to the persistence store failed.");

        public static Error Timeout => Error.ServiceUnavailable(
            Codes.StoreTimeout, "The persistence operation timed out.");

        public static Error CommandTimeout(int seconds) => Error.ServiceUnavailable(
            Codes.StoreTimeout, $"The command exceeded the {seconds}s timeout.");

        public static Error Cancelled => Error.ServiceUnavailable(
            Codes.StoreCancelled, "The persistence operation was cancelled.");

        public static Error Unknown => Error.InternalServerError(
            Codes.StoreUnknown, "The persistence store failed.");
    }

    /// <summary>Stable codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityNotFound              = "Persistence.Entity.NotFound";
        public const string EntityRequired              = "Persistence.Entity.Required";
        public const string MappingMissing              = "Persistence.Mapping.Missing";
        public const string QueryShapeMismatch          = "Persistence.Query.ShapeMismatch";
        public const string QueryInvalid                = "Persistence.Query.Invalid";
        public const string QueryTooComplex             = "Persistence.Query.TooComplex";
        public const string PageSizeExceeded            = "Persistence.Query.PageSizeExceeded";
        public const string ParameterInvalid            = "Persistence.Query.ParameterInvalid";
        public const string ResultSetMismatch           = "Persistence.Query.ResultSetMismatch";
        public const string ChangeTrackingRequired      = "Persistence.Query.ChangeTrackingRequired";
        public const string DuplicateKey                = "Persistence.Entity.DuplicateKey";
        public const string ConcurrencyConflict         = "Persistence.Concurrency.Conflict";
        public const string SaveChangesFailed           = "Persistence.Write.SaveChangesFailed";
        public const string TransactionNested           = "Persistence.Transaction.Nested";
        public const string TransactionRequired         = "Persistence.Transaction.Required";
        public const string TransactionAlreadyCompleted = "Persistence.Transaction.AlreadyCompleted";
        public const string TransactionRollbackOnly     = "Persistence.Transaction.RollbackOnly";
        public const string DeleteSoftDeletable         = "Persistence.Delete.SoftDeletable";
        public const string StoreUnavailable            = "Persistence.Store.Unavailable";
        public const string ConnectionFailed            = "Persistence.Store.ConnectionFailed";
        public const string StoreTimeout                = "Persistence.Store.Timeout";
        public const string StoreCancelled              = "Persistence.Store.Cancelled";
        public const string StoreUnknown                = "Persistence.Store.Unknown";
    }
}