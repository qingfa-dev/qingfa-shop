using Infrastructure.Persistence.EntityFramework.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence.EntityFramework.Contexts;


/// <summary>
/// Design-time factory for EF Core migrations.
/// Required so the dotnet-ef tools can instantiate ApplicationDbContext without DI.
/// </summary>
public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>Creates a new ApplicationDbContext configured for PostgreSQL migrations.</summary>
    /// <param name="args">Command-line arguments (unused).</param>
    /// <returns>A configured ApplicationDbContext instance.</returns>
    public ApplicationDbContext CreateDbContext(string[]? args = null)
    {
        // Configure: Use a default connection string for design-time only.
        // The actual connection string is provided at runtime via DI.
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=qingfa_shop;Username=postgres;Password=postgres",
            npgsql => npgsql.UsePersistenceMigrationHistory());

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}