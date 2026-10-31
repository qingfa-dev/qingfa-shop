namespace Application.BuildingBlocks.Persistence.Initialization.Seeders;

public static class DataSeederOutcome
{
    public static class Failure
    {
        public static Error SeedFailed(string seederName) =>
            Error.InternalServerError(
                Codes.SeedFailed,
                $"The '{seederName}' seeder failed.");

        public static Error StoreUnavailable(string seederName) =>
            Error.ServiceUnavailable(
                Codes.StoreUnavailable,
                $"The database is unavailable while running '{seederName}'.");

        public static Error Timeout(string seederName) =>
            Error.ServiceUnavailable(
                Codes.Timeout,
                $"The '{seederName}' seeder timed out.");

        public static Error Cancelled(string seederName) =>
            Error.ServiceUnavailable(
                Codes.Cancelled,
                $"The '{seederName}' seeder was cancelled.");

        public static Error Unexpected(string seederName) =>
            Error.InternalServerError(
                Codes.Unexpected,
                $"An unexpected error occurred in the '{seederName}' seeder.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string SeedFailed = "Database.Seeder.Failed";
        public const string StoreUnavailable = "Database.Seeder.StoreUnavailable";
        public const string Timeout = "Database.Seeder.Timeout";
        public const string Cancelled = "Database.Seeder.Cancelled";
        public const string Unexpected = "Database.Seeder.Unexpected";
    }
}