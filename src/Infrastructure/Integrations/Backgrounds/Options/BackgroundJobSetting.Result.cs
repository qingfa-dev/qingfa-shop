namespace Infrastructure.Integrations.Backgrounds.Options;

/// <summary>
/// Result types for BackgroundJobSetting operations.
/// Contains error definitions for validation failures.
/// </summary>
/// <remarks>
/// This class provides standardized error types for background job configuration validation.
/// All errors are of type Error.Validation and include descriptive messages.
/// </remarks>
/// <Boundary>Infrastructure - Error definitions</Boundary>
public static class BackgroundJobSettingResult
{
    /// <summary>
    /// Error types for background job configuration validation failures.
    /// </summary>
    /// <remarks>
    /// These error types are used by BackgroundJobSettingValidator to provide
    /// specific error messages for different validation scenarios.
    /// </remarks>
    public static class Failure
    {
        /// <summary>
        /// Error thrown when DashboardPath is empty or null.
        /// </summary>
        /// <returns>An Error.Validation instance with code and message.</returns>
        /// <remarks>
        /// This error is thrown by the NotEmpty() validator rule for DashboardPath.
        /// It indicates that the dashboard path configuration is missing.
        /// </remarks>
        /// <Exception>Thrown during configuration validation</Exception>
        public static Error DashboardPathRequired => Error.Validation(
            code: "BackgroundJobs.DashboardPath.Required",
            message: "BackgroundJobs.DashboardPath is required.");

        /// <summary>
        /// Error thrown when DashboardPath exceeds maximum allowed length.
        /// </summary>
        /// <returns>An Error.Validation instance with code and message.</returns>
        /// <remarks>
        /// This error is thrown by the MaximumLength() validator rule for DashboardPath.
        /// The message includes the maximum allowed length for clarity.
        /// </remarks>
        /// <Exception>Thrown during configuration validation</Exception>
        public static Error DashboardPathTooLong => Error.Validation(
            code: "BackgroundJobs.DashboardPath.TooLong",
            message: $"BackgroundJobs.DashboardPath must not exceed {BackgroundJobDefaults.Constraints.DashboardPathMaxLength} characters.");

        /// <summary>
        /// Error thrown when CachingEnabled is true but no valid connection string exists.
        /// </summary>
        /// <returns>An Error.Validation instance with code and message.</returns>
        /// <remarks>
        /// This error is thrown by the Must() validator rule when CachingEnabled is true.
        /// It ensures that distributed caching has the required Redis connection.
        /// </remarks>
        /// <Exception>Thrown during configuration validation</Exception>
        public static Error CachingConnectionStringMissing => Error.Validation(
            code: "BackgroundJobs.Caching.ConnectionStringMissing",
            message: "A Redis connection string is required when CachingEnabled is true.");
    }
}