namespace Application.BuildingBlocks.Persistence.Initialization.Seeders;

/// <summary>Constants for seeding operations.</summary>
public static class SeederConstant
{
    /// <summary>Hard limits for seeders.</summary>
    public static class Limits
    {
        /// <summary>Default order for seeders when not explicitly set.</summary>
        public const int DefaultOrder = 100;

        /// <summary>Maximum number of seeders that can be registered.</summary>
        public const int MaxSeeders = 500;

        /// <summary>Default batch size for seeding operations.</summary>
        public const int DefaultBatchSize = 1000;

        /// <summary>Maximum batch size for seeding operations.</summary>
        public const int MaxBatchSize = 5000;

        /// <summary>Minimum batch size for seeding operations.</summary>
        public const int MinBatchSize = 1;
    }

    /// <summary>Seeder status names.</summary>
    public static class Status
    {
        /// <summary>Seeder is pending execution.</summary>
        public const string Pending = "Pending";

        /// <summary>Seeder is currently executing.</summary>
        public const string Running = "Running";

        /// <summary>Seeder completed successfully.</summary>
        public const string Completed = "Completed";

        /// <summary>Seeder failed.</summary>
        public const string Failed = "Failed";

        /// <summary>Seeder was skipped (no data to seed).</summary>
        public const string Skipped = "Skipped";
    }
}