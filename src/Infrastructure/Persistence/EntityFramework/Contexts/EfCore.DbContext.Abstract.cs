using Application.Common.Persistence;

using Infrastructure.Persistence.EntityFramework.Conventions;
using Infrastructure.Persistence.EntityFramework.Providers;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Contexts;

/// <summary>Base DbContext for EF Core persistence adapter.</summary>
/// <remarks>
/// Configuration is applied via DI (AddDbContext). This base class applies
/// default conventions and entity configurations from the Infrastructure assembly.
/// </remarks>
public abstract class EfCoreDbContext : DbContext
{
    /// <summary>Creates a new EfCoreDbContext with the given options.</summary>
    protected EfCoreDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>Override to configure entity mappings.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Schema: module tables live in their own schema; the default keeps
        // unconfigured entities from leaking into "public".
        modelBuilder.HasDefaultSchema(PersistenceConstant.Schema.Default);

        // Detect the database provider for provider-specific conventions.
        PersistenceProviderKind providerKind = PersistenceProviderDetector.Detect(Database.ProviderName);

        // Apply: entity configurations first so conventions can see every entity type.
        // Configurations own explicit mappings; conventions only fill the gaps.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EfCoreDbContext).Assembly);
        modelBuilder.ApplyDefaultConventions(providerKind);
    }
}
