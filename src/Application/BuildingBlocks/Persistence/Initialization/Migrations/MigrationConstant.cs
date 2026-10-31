namespace Application.BuildingBlocks.Persistence.Initialization.Migrations;

/// <summary>Constants for migration operations.</summary>
public static class MigrationConstant
{
    /// <summary>Hard limits for migrations.</summary>
    public static class Limits
    {
        /// <summary>Default order for migrations when not explicitly set.</summary>
        public const int DefaultOrder = 100;

        /// <summary>Maximum number of migrations that can be registered.</summary>
        public const int MaxMigrations = 1000;

        /// <summary>Maximum length of a migration name.</summary>
        public const int MaxNameLength = 256;
    }

    /// <summary>Migration history table constants.</summary>
    public static class HistoryTable
    {
        /// <summary>The name of the table that tracks applied migrations.</summary>
        public const string Name = "__MigrationHistory";

        /// <summary>The schema of the migration history table (empty string for default schema).</summary>
        public const string Schema = "";

        /// <summary>Format string for migration version (e.g., "yyyyMMddHHmmss").</summary>
        public const string VersionFormat = "yyyyMMddHHmmss";

        /// <summary>Prefix for auto-generated migration names.</summary>
        public const string AutoNamePrefix = "Migration_";

        /// <summary>Column names in the migration history table.</summary>
        public static class Columns
        {
            /// <summary>Column storing the migration ID/version.</summary>
            public const string MigrationId = "MigrationId";

            /// <summary>Column storing the migration name.</summary>
            public const string Name = "Name";

            /// <summary>Column storing the migration order.</summary>
            public const string Order = "Order";

            /// <summary>Column storing the UTC timestamp when the migration was applied.</summary>
            public const string AppliedAt = "AppliedAt";

            /// <summary>Column storing the UTC timestamp when the migration finished.</summary>
            public const string CompletedAt = "CompletedAt";

            /// <summary>Column storing the migration status.</summary>
            public const string Status = "Status";

            /// <summary>Column storing the error message if the migration failed.</summary>
            public const string ErrorMessage = "ErrorMessage";

            /// <summary>Column storing the checksum of the migration script.</summary>
            public const string Checksum = "Checksum";
        }
    }

    /// <summary>Migration status names.</summary>
    public static class Status
    {
        /// <summary>Migration is pending execution.</summary>
        public const string Pending = "Pending";

        /// <summary>Migration is currently executing.</summary>
        public const string Running = "Running";

        /// <summary>Migration completed successfully.</summary>
        public const string Completed = "Completed";

        /// <summary>Migration failed.</summary>
        public const string Failed = "Failed";

        /// <summary>Migration was skipped.</summary>
        public const string Skipped = "Skipped";
    }
}