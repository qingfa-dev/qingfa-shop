namespace Infrastructure.Persistence.EntityFramework.DbContext;

using Microsoft.EntityFrameworkCore;

/// <summary>Factory for creating pooled DbContext instances.</summary>
/// <remarks>
/// Implements IDbContextFactory for EF Core's built-in pooling support.
/// Use this for short-lived contexts in request-scoped operations.
/// </remarks>
public sealed class EfCoreDbContextFactory<TContext> : IDbContextFactory<TContext>
    where TContext : EfCoreDbContext
{
    private readonly DbContextOptions<TContext> _options;

    /// <summary>Creates a new factory with the given options.</summary>
    public EfCoreDbContextFactory(DbContextOptions<TContext> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Creates a new DbContext instance from the pool.</summary>
    public TContext CreateDbContext()
    {
        return (TContext)Activator.CreateInstance(typeof(TContext), _options)!;
    }
}