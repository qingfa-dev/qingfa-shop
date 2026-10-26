namespace Application.Common.Persistence.Initialization.Resetters;

public static class SchemaResetOutcome
{
    public static class Failure
    {
        public static Error ResetFailed(string reason) =>
            Error.InternalServerError(
                "Database.SchemaReset.Failed",
                $"Database schema reset failed: {reason}");

        public static Error StoreUnavailable =>
            Error.ServiceUnavailable(
                "Database.SchemaReset.StoreUnavailable",
                "The database is unavailable during schema reset.");

        public static Error Timeout =>
            Error.ServiceUnavailable(
                "Database.SchemaReset.Timeout",
                "Database schema reset timed out.");

        public static Error Cancelled =>
            Error.ServiceUnavailable(
                "Database.SchemaReset.Cancelled",
                "Database schema reset was cancelled.");
    }
}