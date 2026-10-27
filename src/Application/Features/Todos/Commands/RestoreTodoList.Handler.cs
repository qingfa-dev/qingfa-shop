using Application.Common.Contexts;
using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Todos.Lists;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="RestoreTodoListCommand"/>.</summary>
/// <param name="repository">The list repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class RestoreTodoListHandler(
    IPersistentRepository<TodoList, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IActorContext actorContext,
    TimeProvider clock)
    : ICommandHandler<RestoreTodoListCommand, TodoListModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoListModel>> Handle(
        RestoreTodoListCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await tenantContext.RequireTenant()
            .BindAsync(
                (tenantId, ct) => repository.FindOwnedAsync(
                    command.Id,
                    tenantId,
                    ct,
                    includeDeleted: true),
                cancellationToken)
            .BindAsync(list => Result<TodoList>.Ok(list).Restore(actorContext.Actor))
            .BindAsync(
                (list, ct) => repository.SaveUpdateAsync(
                    unitOfWork,
                    list,
                    clock.GetUtcNow(),
                    actorContext.Actor,
                    ct),
                cancellationToken)
            .MapAsync(list => list.ToModel());
    }
}
