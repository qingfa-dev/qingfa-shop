using Application.Common.Messaging.Serialization;

using Infrastructure.Persistence.EntityFramework.Outbox;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Interceptors;

/// <summary>
/// Registers the EF Core interceptors that enforce persistence-time concerns.
/// </summary>
/// <remarks>
/// <para>
/// Only concerns with a save-boundary invariant become interceptors:
/// Lifecycle stamps and soft deletion (<see cref="LifecycleInterceptor"/>), the
/// Foundation optimistic-concurrency version (<see cref="VersionableInterceptor"/>), and
/// outbox staging (<see cref="OutboxInterceptor"/>).
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
    /// <param name="domainEventSerializer">The serializer used to stage aggregate events.</param>
    /// <param name="outboxLogger">The optional logger for outbox staging.</param>
    /// <returns>The same options builder for chaining.</returns>
    public static DbContextOptionsBuilder Apply(
        this DbContextOptionsBuilder optionsBuilder,
        IDomainEventSerializer domainEventSerializer,
        ILogger<OutboxInterceptor>? outboxLogger = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(domainEventSerializer);

        optionsBuilder.AddInterceptors(
            new LifecycleInterceptor(),
            new VersionableInterceptor(),
            new OutboxInterceptor(domainEventSerializer, TimeProvider.System, outboxLogger));

        return optionsBuilder;
    }
}
