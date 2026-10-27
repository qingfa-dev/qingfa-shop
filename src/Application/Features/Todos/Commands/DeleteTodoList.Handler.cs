using Application.Common.Contexts;
using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Abstractions;

using Core.Todos;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="DeleteTodoListCommand"/>.</summary>
/// <param name="repository">The list repository.</param>
/// <param name="cleaner">Clears the list reference from todos.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class DeleteTodoListHandler(
    IPersistentRepository<TodoList, Guid> repository,
    ITodoReferenceCleaner cleaner,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IActorContext actorContext,
    TimeProvider clock)
    : ICommandHandler<DeleteTodoListCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(
        DeleteTodoListCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DateTimeOffset nowUtc = clock.GetUtcNow();

        return await tenantContext.RequireTenant()
            .BindAsync(
                (tenantId, ct) => repository.FindOwnedAsync(
                    command.Id,
                    tenantId,
                    ct,
                    includeDeleted: true),
                cancellationToken)
            .BindAsync(
                async (list, ct) => (await cleaner.ClearListAsync(
                        list.TenantId,
                        list.Id,
                        nowUtc,
                        actorContext.Actor,
                        ct))
                    .Map(_ => list),
                cancellationToken)
            .BindAsync(
                (list, ct) => repository.SaveDeleteAsync(
                    unitOfWork,
                    list,
                    command.Mode,
                    nowUtc,
                    actorContext.Actor,
                    ct),
                cancellationToken);
    }
}
