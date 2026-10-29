namespace Infrastructure.Integrations.Backgrounds.Options;

/// <summary>
/// Configuration options for Background Jobs.
/// When CachingEnabled is true, distributed caching is required for Hangfire storage.
/// </summary>
/// <remarks>
/// This class represents the configuration contract for background job processing.
/// It defines the settings that control Hangfire behavior and storage configuration.
/// </remarks>
/// <Boundary>API/Presentation - Configuration binding</Boundary>
public sealed class BackgroundJobSetting
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "BackgroundJobs";

    /// <summary>
    /// Enable or disable background job processing.
    /// </summary>
    /// <value>True to enable background jobs, false to disable.</value>
    /// <remarks>
    /// When disabled, all background job middleware and services are skipped.
    /// This is useful for environments where background processing is not required.
    /// </remarks>
    /// <Contract>Default value is true, can be overridden via configuration</Contract>
    public bool Enabled { get; set; } = BackgroundJobDefaults.Defaults.Enabled;

    /// <summary>
    /// The URL path for the Hangfire dashboard.
    /// </summary>
    /// <value>The relative URL path for accessing the Hangfire dashboard.</value>
    /// <remarks>
    /// The dashboard provides monitoring and management capabilities for background jobs.
    /// Must be a valid URL path without leading slash.
    /// </remarks>
    /// <Contract>Default value is "/jobs", must be validated for length</Contract>
    public string DashboardPath { get; set; } = BackgroundJobDefaults.Defaults.DashboardPath;

    /// <summary>
    /// Enable distributed caching for Hangfire storage.
    /// When true, requires a connection string for distributed cache (Redis).
    /// When false, uses in-memory storage.
    /// </summary>
    /// <value>True to use distributed caching, false to use in-memory storage.</value>
    /// <remarks>
    /// CachingEnabled=true requires a valid Redis connection string in configuration.
    /// CachingEnabled=false uses in-memory storage which is not suitable for production.
    /// The storage choice impacts job durability and scalability.
    /// </remarks>
    /// <Contract>Default value is false, validation ensures connection string when true</Contract>
    public bool CachingEnabled { get; set; } = BackgroundJobDefaults.Defaults.CachingEnabled;
}