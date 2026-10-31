using Application.BuildingBlocks.Persistence.Capabilities;
using Application.BuildingBlocks.Persistence.Repositories;
using Application.BuildingBlocks.Querying.Mapping;

using Core.BuildingBlocks.Domain.Entities;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Repositories;

/// <summary>
/// EF Core implementation of the persistent repository pattern.
/// Bridges the provider-neutral repository interfaces to EF Core's DbContext.
/// Split into partial classes by concern: Read, Write, Delete, Search, Projection, Streaming.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity key type.</typeparam>
public partial class EfCoreRepository<TEntity, TKey> : IPersistentRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    protected readonly DbContext _context;
    private readonly IQueryTranslator<TEntity> _translator;
    private readonly QueryMap<TEntity> _map;

    /// <summary>
    /// What query shapes this provider can translate (expressions, search, ranking, etc.).
    /// Used by callers to check feature support before constructing queries.
    /// </summary>
    public IQueryCapabilities Capabilities { get; }

    public EfCoreRepository(
        DbContext context,
        IQueryTranslator<TEntity> translator,
        IQueryMapProvider mapProvider,
        IQueryCapabilities capabilities)
    {
        _context = context;
        _translator = translator;
        Capabilities = capabilities;

        // Guard: fail fast at DI resolution rather than at the first query, so the missing
        // map is discovered when the repository is constructed, not deep in a request pipeline.
        if (!mapProvider.TryGet<TEntity>(out QueryMap<TEntity>? map))
        {
            throw new InvalidOperationException(
                $"No query map is registered for '{typeof(TEntity).Name}'. " +
                "Register a QueryMap via IQueryMapProvider before resolving the repository.");
        }

        _map = map!;
    }

    /// <summary>
    /// Translates EF Core and database provider exceptions through the shared classifier,
    /// so every persistence surface reports the same owning outcome.
    /// </summary>
    private static Error TranslateException(Exception ex) =>
        PersistenceExceptionTranslator.Classify(ex).ToOutcome();

    /// <summary>
    /// Detaches any tracked instance that shares the supplied entity's key.
    /// EF Core rejects a second instance with the same key, so the stale tracked copy must be
    /// detached before the caller's detached entity can be attached and updated.
    /// </summary>
    /// <remarks>
    /// Reads use <c>AsNoTracking</c>, so update and delete receive detached copies.
    /// If the same key is still tracked (for example, after a create in the same
    /// scope), EF rejects a second instance with that key. Detaching the stale copy
    /// lets the caller's instance become the single tracked instance.
    /// </remarks>
    /// <param name="entity">The detached entity that will be attached.</param>
    private void DetachTrackedCopy(TEntity entity)
    {
        List<EntityEntry<TEntity>> stale = _context.ChangeTracker
            .Entries<TEntity>()
            .Where(entry => !ReferenceEquals(entry.Entity, entity) &&
                EqualityComparer<TKey>.Default.Equals(entry.Entity.Id, entity.Id))
            .ToList();

        foreach (EntityEntry<TEntity> entry in stale)
        {
            entry.State = EntityState.Detached;
        }
    }
}
