using Application.Common.Contexts;
using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Todos.Items;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="CompleteTodoCommand"/>.</summary>
/// <param name="repository">The todo repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class CompleteTodoHandler(
    IPersistentRepository<Todo, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IActorContext actorContext,
    TimeProvider clock)
    : ICommandHandler<CompleteTodoCommand, TodoModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoModel>> Handle(
        CompleteTodoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DateTimeOffset nowUtc = clock.GetUtcNow();

        return await tenantContext.RequireTenant()
            .BindAsync(
                (tenantId, ct) => repository.FindOwnedAsync(command.Id, tenantId, ct),
                cancellationToken)
            .BindAsync(todo => Result<Todo>.Ok(todo).Complete(nowUtc))
            .BindAsync(
                (todo, ct) => repository.SaveUpdateAsync(
                    unitOfWork,
                    todo,
                    nowUtc,
                    actorContext.Actor,
                    ct),
                cancellationToken)
            .MapAsync(todo => todo.ToModel());
    }
}
