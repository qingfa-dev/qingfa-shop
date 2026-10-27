using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Contexts;

/// <summary>
/// Single EF Core DbContext for the entire application.
/// Entity types are discovered from the <c>IEntityTypeConfiguration</c>
/// implementations in the assembly; no DbSet properties are required.
/// </summary>
public sealed class ApplicationDbContext : EfCoreDbContext
{
    /// <summary>Creates a new ApplicationDbContext with the given options.</summary>
    /// <param name="options">The EF Core DbContext options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
