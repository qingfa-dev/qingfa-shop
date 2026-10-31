namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Options;

/// <summary>Configuration settings for EF Core persistence adapter, bound from appsettings.</summary>
/// <remarks>
/// The connection string is resolved from the standard <c>ConnectionStrings</c> section
/// using the name specified in <see cref="ConnectionStringName"/>.
/// </remarks>
public sealed class PersistenceSetting
{
    /// <summary>
    /// Name of the connection string in the <c>ConnectionStrings</c> configuration section.
    /// </summary>
    public string? ConnectionStringName { get; set; }

    /// <summary>Database provider: "PostgreSQL".</summary>
    public string Provider { get; set; } = PersistenceConstants.Defaults.Provider;

    /// <summary>Enable sensitive data logging (not for production).</summary>
    public bool EnableSensitiveDataLogging { get; set; } = PersistenceConstants.Defaults.EnableSensitiveDataLogging;

    /// <summary>Enable detailed error messages.</summary>
    public bool EnableDetailedErrors { get; set; } = PersistenceConstants.Defaults.EnableDetailedErrors;

    /// <summary>Command timeout in seconds.</summary>
    public int CommandTimeoutSeconds { get; set; } = PersistenceConstants.Defaults.CommandTimeoutSeconds;

    /// <summary>Maximum retry attempts for transient failures.</summary>
    public int MaxRetryAttempts { get; set; } = PersistenceConstants.Defaults.MaxRetryAttempts;

    /// <summary>Base delay in milliseconds for exponential backoff.</summary>
    public int RetryBaseDelayMs { get; set; } = PersistenceConstants.Defaults.RetryBaseDelayMs;

    /// <summary>Enable connection pooling.</summary>
    public bool UsePooling { get; set; } = PersistenceConstants.Defaults.UsePooling;

    /// <summary>Minimum connection pool size.</summary>
    public int MinPoolSize { get; set; } = PersistenceConstants.Defaults.MinPoolSize;

    /// <summary>Maximum connection pool size.</summary>
    public int MaxPoolSize { get; set; } = PersistenceConstants.Defaults.MaxPoolSize;
}