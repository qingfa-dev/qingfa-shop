using Infrastructure.Persistence.EntityFramework.Conventions;

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
        modelBuilder.ApplyPersistenceConventions(Database.ProviderName);
    }
}
