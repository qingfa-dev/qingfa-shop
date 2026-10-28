using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Registers all EF Core concern interceptors on a <see cref="DbContextOptionsBuilder"/>.
/// </summary>
/// <remarks>
/// Applies interceptors for Lifecycle concerns (Creatable, Modifiable, Auditable,
/// SoftDeletable, Activatable, Publishable) and Foundation concerns (Versionable).
/// </remarks>
public static class InterceptorRegistry
{
    /// <summary>
    /// Registers all concern interceptors on the specified <see cref="DbContextOptionsBuilder"/>.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <returns>The same options builder for chaining.</returns>
    public static DbContextOptionsBuilder Apply(this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        optionsBuilder.AddInterceptors(
            new CreatableInterceptor(),
            new ModifiableInterceptor(),
            new AuditableInterceptor(),
            new SoftDeletableInterceptor(),
            new ActivatableInterceptor(),
            new PublishableInterceptor(),
            new VersionableInterceptor());

        return optionsBuilder;
    }
}
