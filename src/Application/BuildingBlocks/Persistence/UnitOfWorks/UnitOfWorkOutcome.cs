namespace Application.BuildingBlocks.Persistence.UnitOfWorks;

/// <summary>
/// Unit-of-work failures. Owns work execution, write constraints, store connections,
/// and exception translation outcomes used by <c>EfCoreUnitOfWork</c>.
/// </summary>
/// <remarks>
/// Transaction lifecycle, deadlock, serialization, and read-only failures belong to
/// <see cref="TransactionOutcome"/>. Consumers use those members directly instead of
/// duplicating them here.
/// </remarks>
public static class UnitOfWorkOutcome
{
    public static class Failure
    {
        // ---- Work ----

        public static Error WorkRequired => Error.UnprocessableEntity(
            Codes.WorkRequired, "A work function is required.");

        // ---- Write constraints ----

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

        /// <summary>SaveChanges failed for a non-conflict, non-unique reason</summary>
        public static Error SaveChangesFailed(string reason) => Error.InternalServerError(
            Codes.SaveChangesFailed, $"SaveChanges failed: {reason}");

        // ---- Store connections ----

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

        public static Error Cancelled => Error.ServiceUnavailable(
            Codes.StoreCancelled, "The persistence operation was cancelled.");

        public static Error Unknown => Error.InternalServerError(
            Codes.StoreUnknown, "The persistence store failed.");

        // ---- Query expression translation ----

        public static Error ExpressionTranslationFailed(string reason) => Error.InternalServerError(
            Codes.ExpressionTranslationFailed, $"Could not translate the query expression: {reason}");

        public static Error InvalidOperation => Error.InternalServerError(
            Codes.InvalidOperation, "The operation is invalid in its current state.");

        public static Error InvalidOperationFor(string reason) => Error.InternalServerError(
            Codes.InvalidOperation, $"The operation is invalid: {reason}");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        // ---- Work ----
        public const string WorkRequired = "UnitOfWork.Work.Required";

        // ---- Write constraints ----
        public const string DuplicateKey         = "UnitOfWork.Entity.DuplicateKey";
        public const string ForeignKeyViolation  = "UnitOfWork.Entity.ForeignKeyViolation";
        public const string CheckConstraintViolation = "UnitOfWork.Entity.CheckConstraintViolation";
        public const string NotNullViolation     = "UnitOfWork.Entity.NotNullViolation";
        public const string ConcurrencyConflict  = "UnitOfWork.Concurrency.Conflict";
        public const string SaveChangesFailed    = "UnitOfWork.Write.SaveChangesFailed";

        // ---- Store connections ----
        public const string ConnectionFailed = "UnitOfWork.Store.ConnectionFailed";
        public const string ConnectionRefused = "UnitOfWork.Store.ConnectionRefused";
        public const string ConnectionLost = "UnitOfWork.Store.ConnectionLost";
        public const string LoginFailed = "UnitOfWork.Store.LoginFailed";
        public const string StoreTimeout = "UnitOfWork.Store.Timeout";
        public const string StoreCancelled = "UnitOfWork.Store.Cancelled";
        public const string StoreUnknown = "UnitOfWork.Store.Unknown";

        // ---- Query expression translation ----
        public const string ExpressionTranslationFailed = "UnitOfWork.Query.ExpressionTranslationFailed";
        public const string InvalidOperation          = "UnitOfWork.Operation.Invalid";
    }
}
