namespace Application.BuildingBlocks.Persistence.UnitOfWorks;

/// <summary>Constants for transaction and unit of work operations.</summary>
public static class TransactionConstant
{
    /// <summary>Hard limits for transactions and commands.</summary>
    public static class Limits
    {
        /// <summary>Default command timeout in seconds when not explicitly configured.</summary>
        public const int DefaultCommandTimeoutSeconds = PersistenceConstant.Commands.TimeoutSeconds;

        /// <summary>Maximum command timeout in seconds a caller may request.</summary>
        public const int MaxCommandTimeoutSeconds = 300;

        /// <summary>Minimum command timeout in seconds a caller may request.</summary>
        public const int MinCommandTimeoutSeconds = 1;

        /// <summary>Default maximum retry attempts for transient failures.</summary>
        public const int DefaultMaxRetryAttempts = 3;

        /// <summary>Maximum retry attempts a caller may request.</summary>
        public const int MaxRetryAttempts = 10;

        /// <summary>Default base delay in milliseconds for exponential backoff.</summary>
        public const int DefaultRetryBaseDelayMs = 100;

        /// <summary>Maximum base delay in milliseconds a caller may request.</summary>
        public const int MaxRetryBaseDelayMs = 10000;

        /// <summary>Default minimum connection pool size.</summary>
        public const int DefaultPoolMinSize = 0;

        /// <summary>Default maximum connection pool size.</summary>
        public const int DefaultPoolMaxSize = 100;

        /// <summary>Maximum connection pool size a caller may request.</summary>
        public const int MaxPoolSize = 500;
    }

    /// <summary>Isolation level names as strings (matches <see cref="System.Data.IsolationLevel"/> names).</summary>
    public static class IsolationLevel
    {
        /// <summary>ReadUncommitted isolation level.</summary>
        public const string ReadUncommitted = "ReadUncommitted";

        /// <summary>ReadCommitted isolation level.</summary>
        public const string ReadCommitted = "ReadCommitted";

        /// <summary>RepeatableRead isolation level.</summary>
        public const string RepeatableRead = "RepeatableRead";

        /// <summary>Serializable isolation level.</summary>
        public const string Serializable = "Serializable";

        /// <summary>Snapshot isolation level.</summary>
        public const string Snapshot = "Snapshot";

        /// <summary>Unspecified isolation level (uses provider default).</summary>
        public const string Unspecified = "Unspecified";
    }

    /// <summary>Transaction state names.</summary>
    public static class State
    {
        /// <summary>Transaction is active and can be committed or rolled back.</summary>
        public const string Active = "Active";

        /// <summary>Transaction has been committed successfully.</summary>
        public const string Committed = "Committed";

        /// <summary>Transaction has been rolled back.</summary>
        public const string RolledBack = "RolledBack";

        /// <summary>Transaction is marked for rollback only.</summary>
        public const string RollbackOnly = "RollbackOnly";

        /// <summary>Transaction has failed and cannot be committed.</summary>
        public const string Failed = "Failed";
    }

    /// <summary>Transaction error codes for isolation levels and deadlocks.</summary>
    public static class ErrorCodes
    {
        /// <summary>The transaction isolation level is not supported.</summary>
        public const string IsolationLevelUnsupported = "Persistence.Transaction.IsolationLevelUnsupported";

        /// <summary>The transaction has been deadlocked.</summary>
        public const string Deadlock = "Persistence.Transaction.Deadlock";

        /// <summary>The transaction was chosen as the deadlock victim.</summary>
        public const string DeadlockVictim = "Persistence.Transaction.DeadlockVictim";
    }
}