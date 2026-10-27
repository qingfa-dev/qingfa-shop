namespace Application.Common.Persistence;

/// <summary>Shared constants for the persistence layer.</summary>
/// <remarks>
/// This class contains cross-cutting constants used across multiple persistence sub-components.
/// Area-specific constants live in their respective folders (Migrations, Seeders, UnitOfWorks, Repositories, Capabilities).
/// </remarks>
public static class PersistenceConstant
{
    /// <summary>Configuration section names.</summary>
    public static class Configuration
    {
        /// <summary>Configuration section for persistence settings.</summary>
        public const string SectionName = "Persistence";
    }

    /// <summary>Common persistence error codes for migrations, seeders, and resetters.</summary>
    public static class ErrorCodes
    {
        // ---- Migration ----
        /// <summary>A migration failed to apply.</summary>
        public const string MigrationFailed = "Persistence.Migration.Failed";

        /// <summary>A migration was cancelled.</summary>
        public const string MigrationCancelled = "Persistence.Migration.Cancelled";

        /// <summary>A migration checksum does not match.</summary>
        public const string MigrationChecksumMismatch = "Persistence.Migration.ChecksumMismatch";

        /// <summary>A migration is already applied.</summary>
        public const string MigrationAlreadyApplied = "Persistence.Migration.AlreadyApplied";

        /// <summary>A migration is missing from the assembly.</summary>
        public const string MigrationMissing = "Persistence.Migration.Missing";

        /// <summary>Cyclic dependency detected between migrations.</summary>
        public const string MigrationCyclicDependency = "Persistence.Migration.CyclicDependency";

        // ---- Seeder ----
        /// <summary>A seeder failed to execute.</summary>
        public const string SeederFailed = "Persistence.Seeder.Failed";

        /// <summary>A seeder was cancelled.</summary>
        public const string SeederCancelled = "Persistence.Seeder.Cancelled";

        /// <summary>Seed data validation failed.</summary>
        public const string SeedDataInvalid = "Persistence.Seeder.DataInvalid";

        /// <summary>Seed data conflicts with existing data.</summary>
        public const string SeedDataConflict = "Persistence.Seeder.DataConflict";

        // ---- Resetter ----
        /// <summary>A resetter failed to execute.</summary>
        public const string ResetterFailed = "Persistence.Resetter.Failed";

        /// <summary>A resetter was cancelled.</summary>
        public const string ResetterCancelled = "Persistence.Resetter.Cancelled";
    }
}