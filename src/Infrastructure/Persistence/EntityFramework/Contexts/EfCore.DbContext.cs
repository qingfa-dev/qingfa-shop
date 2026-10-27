using Microsoft.EntityFrameworkCore;

using Application.Common.Persistence;

namespace Infrastructure.Persistence.EntityFramework.Contexts;
/// <summary>Base DbContext for EF Core persistence adapter.</summary>
/// <remarks>
/// Configuration is applied via DI (AddDbContext). This base class only applies
/// entity configurations from the assembly.
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
        // Schema: module tables live in their own schema; the default keeps
        // unconfigured entities from leaking into "public".
        modelBuilder.HasDefaultSchema(PersistenceConstant.Schema.Default);

        // Apply all IEntityTypeConfiguration from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        base.OnModelCreating(modelBuilder);
    }
}