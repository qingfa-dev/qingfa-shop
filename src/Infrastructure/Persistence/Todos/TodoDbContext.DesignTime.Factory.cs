
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence.Todos;
/// <summary>
/// Design-time factory for EF Core migrations.
/// Required so the dotnet-ef tools can instantiate TodoDbContext without DI.
/// </summary>
public sealed class TodoDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TodoDbContext>
{
    /// <summary>Creates a new TodoDbContext configured for PostgreSQL migrations.</summary>
    /// <param name="args">Command-line arguments (unused).</param>
    /// <returns>A configured TodoDbContext instance.</returns>
    public TodoDbContext CreateDbContext(string[]? args = null)
    {
        // Configure: Use a default connection string for design-time only.
        // The actual connection string is provided at runtime via DI.
        var optionsBuilder = new DbContextOptionsBuilder<TodoDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=todos;Username=postgres;Password=postgres");

        return new TodoDbContext(optionsBuilder.Options);
    }
}
