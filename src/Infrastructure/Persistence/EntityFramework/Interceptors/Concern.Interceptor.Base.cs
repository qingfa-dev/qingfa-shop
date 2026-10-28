using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Base class for concern interceptors that stamp entities during <see cref="DbContext.SaveChanges()"/>.
/// </summary>
/// <remarks>
/// Applies the concern on both the synchronous and asynchronous save paths so a caller
/// cannot bypass lifecycle stamps by using <see cref="DbContext.SaveChanges()"/>.
/// </remarks>
public abstract class ConcernInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public sealed override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public sealed override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>Applies the concern to the tracked entities of the given context.</summary>
    /// <param name="context">The context being saved, or <c>null</c> when it is unavailable.</param>
    protected abstract void Apply(DbContext? context);
}
