using Application.Common.EventSourcing;
using Application.Common.Persistence.Repositories;

using Core.Todos.Items;

namespace Application.Features.Todos.Common;

/// <summary>Tenant-scoped loading for the event-sourced todo stream.</summary>
public static class TodoEventRepositoryExtension
{
    /// <summary>Loads a todo and fails when it is missing or owned by another tenant.</summary>
    public static async Task<Result<Todo>> LoadOwnedAsync(
        this IEventSourcedRepository<Todo, Guid> repository,
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        Result<Todo> loaded = await repository
            .LoadAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return loaded.Ensure(
            todo => todo.TenantId == tenantId,
            RepositoryOutcome.Failure.NotFoundFor<Todo>(id));
    }
}
