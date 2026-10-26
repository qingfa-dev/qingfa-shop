namespace Infrastructure.Persistence.EntityFramework.Options;

/// <summary>Constants for EF Core configuration.</summary>
public static class EfCoreSettingConstant
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "EfCore";

    /// <summary>Default database provider.</summary>
    public const string DefaultProvider = "PostgreSQL";

    /// <summary>Default command timeout in seconds.</summary>
    public const int DefaultCommandTimeoutSeconds = 30;

    /// <summary>Default maximum retry attempts for transient failures.</summary>
    public const int DefaultMaxRetryAttempts = 3;

    /// <summary>Default base delay in milliseconds for exponential backoff.</summary>
    public const int DefaultRetryBaseDelayMs = 100;

    /// <summary>Default connection pool minimum size.</summary>
    public const int DefaultMinPoolSize = 0;

    /// <summary>Default connection pool maximum size.</summary>
    public const int DefaultMaxPoolSize = 100;
}