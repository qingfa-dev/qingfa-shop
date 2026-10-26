namespace Application.Common.Persistence.Repositories;

/// <summary>Constants for repository operations.</summary>
public static class RepositoryConstant
{
    /// <summary>Hard limits for repository operations.</summary>
    public static class Limits
    {
        /// <summary>Default page size when the request does not specify one.</summary>
        public const int DefaultPageSize = 20;

        /// <summary>Maximum page size a request may ask for.</summary>
        public const int MaxPageSize = 200;

        /// <summary>Minimum page size a request may ask for.</summary>
        public const int MinPageSize = 1;

        /// <summary>Default batch size for bulk operations.</summary>
        public const int DefaultBatchSize = 1000;

        /// <summary>Maximum batch size for bulk operations.</summary>
        public const int MaxBatchSize = 5000;

        /// <summary>Minimum batch size for bulk operations.</summary>
        public const int MinBatchSize = 1;

        /// <summary>Maximum length of a table name.</summary>
        public const int MaxTableNameLength = 128;

        /// <summary>Maximum length of a column name.</summary>
        public const int MaxColumnNameLength = 128;

        /// <summary>Maximum length of an index name.</summary>
        public const int MaxIndexNameLength = 128;

        /// <summary>Maximum length of a constraint name.</summary>
        public const int MaxConstraintNameLength = 128;

        /// <summary>Maximum length of a parameter name.</summary>
        public const int MaxParameterNameLength = 128;
    }

    /// <summary>SQL dialect tokens for provider-agnostic query building.</summary>
    public static class Dialect
    {
        /// <summary>Token for the current timestamp function.</summary>
        public const string CurrentTimestamp = "CURRENT_TIMESTAMP";

        /// <summary>Token for the current UTC timestamp function.</summary>
        public const string CurrentUtcTimestamp = "CURRENT_TIMESTAMP";

        /// <summary>Token for generating a new UUID/GUID.</summary>
        public const string NewGuid = "NEWID()";

        /// <summary>Token for generating a new sequential UUID/GUID.</summary>
        public const string NewSequentialGuid = "NEWSEQUENTIALID()";

        /// <summary>Token for parameter prefix (varies by provider: @, :, ?).</summary>
        public const string ParameterPrefix = "@";

        /// <summary>Token for quoting identifiers (varies by provider: ", [], `).</summary>
        public const string IdentifierQuote = "\"";

        /// <summary>Token for string concatenation operator.</summary>
        public const string ConcatOperator = "||";

        /// <summary>Token for boolean true literal.</summary>
        public const string TrueLiteral = "1";

        /// <summary>Token for boolean false literal.</summary>
        public const string FalseLiteral = "0";

        /// <summary>Token for limiting results (TOP, LIMIT, FETCH FIRST).</summary>
        public const string LimitKeyword = "LIMIT";

        /// <summary>Token for offset (OFFSET).</summary>
        public const string OffsetKeyword = "OFFSET";
    }

    /// <summary>Repository error codes extending <see cref="PersistenceOutcome.Codes"/>.</summary>
    public static class ErrorCodes
    {
        // ---- Command ----
        /// <summary>The command timed out.</summary>
        public const string CommandTimeout = "Persistence.Command.Timeout";

        /// <summary>The command was cancelled.</summary>
        public const string CommandCancelled = "Persistence.Command.Cancelled";

        /// <summary>The command text is invalid or malformed.</summary>
        public const string CommandInvalid = "Persistence.Command.Invalid";

        // ---- Connection ----
        /// <summary>The connection string is missing or invalid.</summary>
        public const string ConnectionStringInvalid = "Persistence.Connection.StringInvalid";

        /// <summary>The connection pool is exhausted.</summary>
        public const string PoolExhausted = "Persistence.Connection.PoolExhausted";

        /// <summary>The connection was broken during operation.</summary>
        public const string ConnectionBroken = "Persistence.Connection.Broken";

        // ---- Schema ----
        /// <summary>The database schema does not match the expected model.</summary>
        public const string SchemaMismatch = "Persistence.Schema.Mismatch";

        /// <summary>A required table is missing.</summary>
        public const string TableMissing = "Persistence.Schema.TableMissing";

        /// <summary>A required column is missing.</summary>
        public const string ColumnMissing = "Persistence.Schema.ColumnMissing";

        /// <summary>A required index is missing.</summary>
        public const string IndexMissing = "Persistence.Schema.IndexMissing";

        /// <summary>A constraint violation occurred (FK, CK, etc).</summary>
        public const string ConstraintViolation = "Persistence.Schema.ConstraintViolation";

        // ---- Bulk Operations ----
        /// <summary>A bulk insert operation failed.</summary>
        public const string BulkInsertFailed = "Persistence.Bulk.InsertFailed";

        /// <summary>A bulk update operation failed.</summary>
        public const string BulkUpdateFailed = "Persistence.Bulk.UpdateFailed";

        /// <summary>A bulk delete operation failed.</summary>
        public const string BulkDeleteFailed = "Persistence.Bulk.DeleteFailed";

        /// <summary>A bulk operation was cancelled.</summary>
        public const string BulkCancelled = "Persistence.Bulk.Cancelled";

        /// <summary>The batch size exceeds the maximum allowed.</summary>
        public const string BatchSizeExceeded = "Persistence.Batch.SizeExceeded";

        // ---- Concurrency ----
        /// <summary>Optimistic concurrency token is missing.</summary>
        public const string ConcurrencyTokenMissing = "Persistence.Concurrency.TokenMissing";

        /// <summary>Optimistic concurrency token format is invalid.</summary>
        public const string ConcurrencyTokenInvalid = "Persistence.Concurrency.TokenInvalid";

        // ---- Provider ----
        /// <summary>The requested persistence provider is not registered.</summary>
        public const string ProviderNotRegistered = "Persistence.Provider.NotRegistered";

        /// <summary>The requested persistence provider is not supported.</summary>
        public const string ProviderNotSupported = "Persistence.Provider.NotSupported";

        /// <summary>The provider-specific feature is not available.</summary>
        public const string ProviderFeatureUnavailable = "Persistence.Provider.FeatureUnavailable";
    }
}