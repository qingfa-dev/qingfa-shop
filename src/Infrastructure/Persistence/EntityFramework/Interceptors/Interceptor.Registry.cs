using Application.Common.Events;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Registers the EF Core interceptors that enforce persistence-time concerns.
/// </summary>
/// <remarks>
/// <para>
/// Only concerns with a save-boundary invariant become interceptors:
/// Lifecycle stamps and soft deletion (<see cref="LifecycleInterceptor"/>) and the
/// Foundation optimistic-concurrency version (<see cref="VersionableInterceptor"/>).
/// Domain events are dispatched by <see cref="DomainEventInterceptor"/> after a successful save.
/// </para>
/// <para>
/// The remaining Foundation concerns are not stamps: <c>Identifiable</c> and
/// <c>Referenceable</c> are owned by the entity or its generator, while <c>Tenantable</c> and
/// <c>StoreScoped</c> belong to request scoping and query filters (Authentication is a separate
/// capability). Content concerns (localization, media, metafields, slugs, tags) and Organization
/// concerns (categories, collections, hierarchy, position) are domain behavior. They are
/// validated by their concern validators and persisted by entity configurations; stamping them
/// at save time would hide the owning aggregate's decisions.
/// </para>
/// </remarks>
public static class InterceptorRegistry
{
    /// <summary>
    /// Registers every persistence interceptor on the specified <see cref="DbContextOptionsBuilder"/>.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="domainEventPublisher">The publisher used after a successful save.</param>
    /// <param name="domainEventLogger">The optional logger for domain-event publication.</param>
    /// <returns>The same options builder for chaining.</returns>
    public static DbContextOptionsBuilder Apply(
        this DbContextOptionsBuilder optionsBuilder,
        IDomainEventPublisher domainEventPublisher,
        ILogger<DomainEventInterceptor>? domainEventLogger = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(domainEventPublisher);

        optionsBuilder.AddInterceptors(
            new LifecycleInterceptor(),
            new VersionableInterceptor(),
            new DomainEventInterceptor(domainEventPublisher, domainEventLogger));

        return optionsBuilder;
    }
}
