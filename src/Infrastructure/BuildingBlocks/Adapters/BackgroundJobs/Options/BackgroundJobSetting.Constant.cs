namespace Infrastructure.BuildingBlocks.Adapters.BackgroundJobs.Options;

/// <summary>
/// Default values and validation constraints for background job configurations.
/// </summary>
/// <remarks>
/// This class contains all the constant values used throughout the background job system.
/// It separates defaults, constraints, and environment-specific settings for better organization.
/// </remarks>
/// <Boundary>Infrastructure - Configuration constants</Boundary>
public static class BackgroundJobDefaults
{
    /// <summary>
    /// Default configuration values for background job settings.
    /// </summary>
    /// <remarks>
    /// These are the default values used when configuration is not explicitly set.
    /// They represent the recommended production configuration.
    /// </remarks>
    public static class Defaults
    {
        /// <summary>
        /// Default value for the Enabled property.
        /// </summary>
        /// <value>True to enable background job processing by default.</value>
        public const bool Enabled = true;

        /// <summary>
        /// Default dashboard path for Hangfire monitoring.
        /// </summary>
        /// <value>"/jobs" is the standard path for the Hangfire dashboard.</value>
        public const string DashboardPath = "/jobs";

        /// <summary>
        /// Default value for CachingEnabled property.
        /// </summary>
        /// <value>False to use in-memory storage by default.</value>
        /// <remarks>
        /// In-memory storage is the default for development and testing.
        /// Production deployments should set CachingEnabled to true.
        /// </remarks>
        public const bool CachingEnabled = false;
    }

    /// <summary>
    /// Validation constraints for background job configuration properties.
    /// </summary>
    /// <remarks>
    /// These constraints are enforced by the BackgroundJobSettingValidator.
    /// They ensure configuration values are within acceptable ranges.
    /// </remarks>
    public static class Constraints
    {
        /// <summary>
        /// Maximum allowed length for the dashboard path.
        /// </summary>
        /// <value>2048 characters is the maximum URL path length.</value>
        public const int DashboardPathMaxLength = 2048;
    }

    /// <summary>
    /// Environment-specific constant values.
    /// </summary>
    /// <remarks>
    /// These constants are used to identify specific environments in configuration.
    /// They help with environment-aware configuration and behavior.
    /// </remarks>
    public static class Environments
    {
        /// <summary>
        /// The development environment identifier.
        /// </summary>
        /// <value>"Development" is the standard .NET development environment name.</value>
        public const string Development = "Development";
    }
}