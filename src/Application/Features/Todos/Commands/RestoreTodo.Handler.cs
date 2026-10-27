using Application.Common.Contexts;
using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Todos.Items;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="RestoreTodoCommand"/>.</summary>
/// <param name="repository">The todo repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class RestoreTodoHandler(
    IPersistentRepository<Todo, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IActorContext actorContext,
    TimeProvider clock)
    : ICommandHandler<RestoreTodoCommand, TodoModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoModel>> Handle(
        RestoreTodoCommand command,
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
            .BindAsync(todo => Result<Todo>.Ok(todo).Restore(actorContext.Actor))
            .BindAsync(
                (todo, ct) => repository.SaveUpdateAsync(
                    unitOfWork,
                    todo,
                    clock.GetUtcNow(),
                    actorContext.Actor,
                    ct),
                cancellationToken)
            .MapAsync(todo => todo.ToModel());
    }
}
