using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Security.Authentication.Contexts;

using Core.Todos.Items;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="DeleteTodoCommand"/>.</summary>
/// <param name="repository">The todo repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class DeleteTodoHandler(
    IPersistentRepository<Todo, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IUserContext actorContext,
    TimeProvider clock)
    : ICommandHandler<DeleteTodoCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(
        DeleteTodoCommand command,
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
            .BindAsync(
                (todo, ct) => repository.SaveDeleteAsync(
                    unitOfWork,
                    todo,
                    command.Mode,
                    clock.GetUtcNow(),
                    actorContext.Actor(),
                    ct),
                cancellationToken);
    }
}
