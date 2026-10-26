namespace Application.Common.Persistence.Initialization.Migrations;

using Core.Common.Monad.Results;

public static class MigrationOutcome
{
    public static class Failure
    {
        // ---- Migration Execution ----
        public static Error MigrationFailed(string migratorName) =>
            Error.InternalServerError(
                "Database.Migration.Failed",
                $"Migrations failed for '{migratorName}'.");

        public static Error StoreUnavailable(string migratorName) =>
            Error.ServiceUnavailable(
                "Database.Migration.StoreUnavailable",
                $"The database is unavailable while migrating '{migratorName}'.");

        public static Error Timeout(string migratorName) =>
            Error.ServiceUnavailable(
                "Database.Migration.Timeout",
                $"Database migration timed out for '{migratorName}'.");

        public static Error Cancelled(string migratorName) =>
            Error.ServiceUnavailable(
                "Database.Migration.Cancelled",
                $"Database migration was cancelled for '{migratorName}'.");

        // ---- Validation ----
        public static Error NameNull(string parameterName = "name") => Error.Validation(
            Codes.NameNull, $"{parameterName} cannot be null.");

        public static Error NameNullOrWhitespace(string parameterName = "name") => Error.Validation(
            Codes.NameNullOrWhitespace, $"{parameterName} cannot be empty or whitespace.");

        public static Error NameExceedsMaxLength(string parameterName = "name") => Error.Validation(
            Codes.NameExceedsMaxLength,
            $"{parameterName} cannot exceed {MigrationConstant.Limits.MaxNameLength} characters.");

        public static Error OrderNegative(string parameterName = "order") => Error.Validation(
            Codes.OrderNegative, $"{parameterName} cannot be negative.");

        public static Error CountNegative(string parameterName = "count") => Error.Validation(
            Codes.CountNegative, $"{parameterName} cannot be negative.");

        public static Error CountExceedsMax(string parameterName = "count") => Error.Validation(
            Codes.CountExceedsMax,
            $"{parameterName} cannot exceed {MigrationConstant.Limits.MaxMigrations} migrations.");

        public static Error VersionNull(string parameterName = "version") => Error.Validation(
            Codes.VersionNull, $"{parameterName} cannot be null.");

        public static Error VersionNullOrWhitespace(string parameterName = "version") => Error.Validation(
            Codes.VersionNullOrWhitespace, $"{parameterName} cannot be empty or whitespace.");

        public static Error VersionInvalidFormat(string parameterName = "version") => Error.Validation(
            Codes.VersionInvalidFormat,
            $"{parameterName} must match format 'yyyyMMddHHmmss' (14 digits).");

        public static Error ChecksumNull(string parameterName = "checksum") => Error.Validation(
            Codes.ChecksumNull, $"{parameterName} cannot be null.");

        public static Error ChecksumNullOrWhitespace(string parameterName = "checksum") => Error.Validation(
            Codes.ChecksumNullOrWhitespace, $"{parameterName} cannot be empty or whitespace.");

        public static Error ChecksumInvalidFormat(string parameterName = "checksum") => Error.Validation(
            Codes.ChecksumInvalidFormat,
            $"{parameterName} must be a valid SHA-256 hex string (64 characters).");
    }

    /// <summary>Stable codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string NameNull                    = "Migration.Name.Null";
        public const string NameNullOrWhitespace        = "Migration.Name.NullOrWhitespace";
        public const string NameExceedsMaxLength        = "Migration.Name.ExceedsMaxLength";
        public const string OrderNegative               = "Migration.Order.Negative";
        public const string CountNegative               = "Migration.Count.Negative";
        public const string CountExceedsMax             = "Migration.Count.ExceedsMax";
        public const string VersionNull                 = "Migration.Version.Null";
        public const string VersionNullOrWhitespace     = "Migration.Version.NullOrWhitespace";
        public const string VersionInvalidFormat        = "Migration.Version.InvalidFormat";
        public const string ChecksumNull                = "Migration.Checksum.Null";
        public const string ChecksumNullOrWhitespace    = "Migration.Checksum.NullOrWhitespace";
        public const string ChecksumInvalidFormat       = "Migration.Checksum.InvalidFormat";
    }
}