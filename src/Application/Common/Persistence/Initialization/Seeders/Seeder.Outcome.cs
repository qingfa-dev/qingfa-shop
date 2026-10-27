namespace Application.Common.Persistence.Initialization.Seeders;

public static class DataSeederOutcome
{
    public static class Failure
    {
        public static Error SeedFailed(string seederName) =>
            Error.InternalServerError(
                "Database.Seeder.Failed",
                $"The '{seederName}' seeder failed.");

        public static Error StoreUnavailable(string seederName) =>
            Error.ServiceUnavailable(
                "Database.Seeder.StoreUnavailable",
                $"The database is unavailable while running '{seederName}'.");

        public static Error Timeout(string seederName) =>
            Error.ServiceUnavailable(
                "Database.Seeder.Timeout",
                $"The '{seederName}' seeder timed out.");

        public static Error Cancelled(string seederName) =>
            Error.ServiceUnavailable(
                "Database.Seeder.Cancelled",
                $"The '{seederName}' seeder was cancelled.");

        public static Error Unexpected(string seederName) =>
            Error.InternalServerError(
                "Database.Seeder.Unexpected",
                $"An unexpected error occurred in the '{seederName}' seeder.");
    }
}