namespace Application.BuildingBlocks.Persistence.Initialization.Resetters;

public static class SchemaResetOutcome
{
    public static class Failure
    {
        public static Error ResetFailed(string reason) =>
            Error.InternalServerError(
                Codes.ResetFailed,
                $"Database schema reset failed: {reason}");

        public static Error StoreUnavailable =>
            Error.ServiceUnavailable(
                Codes.StoreUnavailable,
                "The database is unavailable during schema reset.");

        public static Error Timeout =>
            Error.ServiceUnavailable(
                Codes.Timeout,
                "Database schema reset timed out.");

        public static Error Cancelled =>
            Error.ServiceUnavailable(
                Codes.Cancelled,
                "Database schema reset was cancelled.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ResetFailed = "Database.SchemaReset.Failed";
        public const string StoreUnavailable = "Database.SchemaReset.StoreUnavailable";
        public const string Timeout = "Database.SchemaReset.Timeout";
        public const string Cancelled = "Database.SchemaReset.Cancelled";
    }
}