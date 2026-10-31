using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Persistence.Repositories;

using Core.Modules.Todos.Items;

namespace Application.Modules.Todos.Common;

/// <summary>Tenant-scoped loading for the event-sourced todo stream.</summary>
public static class TodoEventRepositoryExtension
{
    /// <summary>
    /// Loads a todo and fails when it is missing, owned by another tenant,
    /// or soft-deleted while <paramref name="includeDeleted"/> is false.
    /// </summary>
    /// <param name="repository">The todo event-stream repository.</param>
    /// <param name="id">The stream identifier.</param>
    /// <param name="tenantId">The tenant that must own the stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="includeDeleted">Whether a soft-deleted stream is visible.</param>
    /// <returns>The owned todo, or a not-found failure.</returns>
    public static async Task<Result<Todo>> LoadOwnedAsync(
        this IEventSourcedRepository<Todo, Guid> repository,
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default,
        bool includeDeleted = false)
    {
        ArgumentNullException.ThrowIfNull(repository);

        Result<Todo> loaded = await repository
            .LoadAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return loaded.Ensure(
            todo => todo.TenantId == tenantId && (includeDeleted || !todo.IsDeleted),
            RepositoryOutcome.Failure.NotFoundFor<Todo>(id));
    }
}
