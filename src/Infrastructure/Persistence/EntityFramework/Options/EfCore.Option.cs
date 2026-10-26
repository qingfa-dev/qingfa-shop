namespace Infrastructure.Persistence.EntityFramework.Options;

/// <summary>Configuration options for EF Core persistence adapter.</summary>
public sealed record EfCoreOption
{
    /// <summary>Database connection string.</summary>
    public string? ConnectionString { get; init; }

    /// <summary>Database provider: "PostgreSQL" or "SqlServer".</summary>
    public string Provider { get; init; } = EfCoreSettingConstant.DefaultProvider;

    /// <summary>Enable sensitive data logging (not for production).</summary>
    public bool EnableSensitiveDataLogging { get; init; }

    /// <summary>Enable detailed error messages.</summary>
    public bool EnableDetailedErrors { get; init; }

    /// <summary>Command timeout in seconds.</summary>
    public int CommandTimeoutSeconds { get; init; } = EfCoreSettingConstant.DefaultCommandTimeoutSeconds;

    /// <summary>Maximum retry attempts for transient failures.</summary>
    public int MaxRetryAttempts { get; init; } = EfCoreSettingConstant.DefaultMaxRetryAttempts;

    /// <summary>Base delay in milliseconds for exponential backoff.</summary>
    public int RetryBaseDelayMs { get; init; } = EfCoreSettingConstant.DefaultRetryBaseDelayMs;

    /// <summary>Enable connection pooling.</summary>
    public bool UsePooling { get; init; } = true;

    /// <summary>Minimum connection pool size.</summary>
    public int MinPoolSize { get; init; } = EfCoreSettingConstant.DefaultMinPoolSize;

    /// <summary>Maximum connection pool size.</summary>
    public int MaxPoolSize { get; init; } = EfCoreSettingConstant.DefaultMaxPoolSize;
}