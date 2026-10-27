using Application.Common.Contexts;
using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Todos;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="UpdateTodoListCommand"/>.</summary>
/// <param name="repository">The list repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class UpdateTodoListHandler(
    IPersistentRepository<TodoList, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IActorContext actorContext,
    TimeProvider clock)
    : ICommandHandler<UpdateTodoListCommand, TodoListModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoListModel>> Handle(
        UpdateTodoListCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await tenantContext.RequireTenant()
            .BindAsync(
                (tenantId, ct) => repository.FindOwnedAsync(command.Id, tenantId, ct),
                cancellationToken)
            .BindAsync(list => ApplyChanges(list, command))
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

    private static Result<TodoList> ApplyChanges(TodoList list, UpdateTodoListCommand command)
    {
        Result<TodoList> updated = Result<TodoList>.Ok(list);

        if (command.Name is not null)
        {
            updated = updated.SetName(command.Name);
        }

        if (command.ClearDescription)
        {
            updated = updated.SetDescription(null);
        }
        else if (command.Description is not null)
        {
            updated = updated.SetDescription(command.Description);
        }

        return updated;
    }
}
