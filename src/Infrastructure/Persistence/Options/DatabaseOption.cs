namespace Infrastructure.Persistence.Options;

/// <summary>
/// Defines constant keys for database connection strings and configuration options.
/// </summary>
public static class DatabaseOption
{
    /// <summary>The default connection string key used when not running in Aspire.</summary>
    public const string Default = "DefaultConnection";

    /// <summary>The connection string key used when running in a .NET Aspire environment.</summary>
    public const string Aspire = "AspireConnection";

    /// <summary>The key used for in-memory database configuration in testing scenarios.</summary>
    public const string InMemory = "InMemoryDatabase";
}
