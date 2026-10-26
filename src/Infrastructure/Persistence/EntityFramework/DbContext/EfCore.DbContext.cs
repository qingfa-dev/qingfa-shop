namespace Infrastructure.Persistence.EntityFramework.DbContext;

using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence.EntityFramework.Options;

/// <summary>Base DbContext for EF Core persistence adapter.</summary>
/// <remarks>
/// Configures provider-specific options, retry policy, and query tracking.
/// Derived contexts must define DbSets and entity configurations.
/// </remarks>
public abstract class EfCoreDbContext : DbContext
{
    private readonly EfCoreOption _option;

    /// <summary>Creates a new EfCoreDbContext with the given options.</summary>
    protected EfCoreDbContext(DbContextOptions options, EfCoreOption? option = null)
        : base(options)
    {
        _option = option ?? new EfCoreOption();
    }

    /// <summary>Configures the database provider and options.</summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            ConfigureProvider(optionsBuilder);
        }

        base.OnConfiguring(optionsBuilder);
    }

    private void ConfigureProvider(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _option.ConnectionString 
            ?? throw new InvalidOperationException("Connection string is required.");

        optionsBuilder
            .EnableSensitiveDataLogging(_option.EnableSensitiveDataLogging)
            .EnableDetailedErrors(_option.EnableDetailedErrors)
            .ConfigureWarnings(w => w.Default(WarningBehavior.Throw));

        if (_option.UsePooling)
        {
            // Pooling is configured via connection string for Npgsql/SQL Server
        }

        switch (_option.Provider.ToLowerInvariant())
        {
            case "postgresql":
                optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    ConfigureNpgsqlOptions(npgsqlOptions);
                });
                break;

            case "sqlserver":
                optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
                {
                    ConfigureSqlServerOptions(sqlOptions);
                });
                break;

            default:
                throw new NotSupportedException($"Provider '{_option.Provider}' is not supported.");
        }
    }

    private void ConfigureNpgsqlOptions(Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.NpgsqlDbContextOptionsBuilder npgsqlOptions)
    {
        npgsqlOptions
            .CommandTimeout(_option.CommandTimeoutSeconds)
            .EnableRetryOnFailure(_option.MaxRetryAttempts, 
                TimeSpan.FromMilliseconds(_option.RetryBaseDelayMs), null);
    }

    private void ConfigureSqlServerOptions(Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder sqlOptions)
    {
        sqlOptions
            .CommandTimeout(_option.CommandTimeoutSeconds)
            .EnableRetryOnFailure(_option.MaxRetryAttempts,
                TimeSpan.FromMilliseconds(_option.RetryBaseDelayMs), null);
    }

    /// <summary>Override to configure entity mappings.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        base.OnModelCreating(modelBuilder);
    }
}