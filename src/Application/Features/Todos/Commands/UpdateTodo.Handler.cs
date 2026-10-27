using Application.Common.Contexts;
using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Todos;
using Core.Todos.Items;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="UpdateTodoCommand"/>.</summary>
/// <param name="repository">The todo repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class UpdateTodoHandler(
    IPersistentRepository<Todo, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IActorContext actorContext,
    TimeProvider clock)
    : ICommandHandler<UpdateTodoCommand, TodoModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoModel>> Handle(
        UpdateTodoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await tenantContext.RequireTenant()
            .BindAsync(
                (tenantId, ct) => repository.FindOwnedAsync(command.Id, tenantId, ct),
                cancellationToken)
            .BindAsync(todo => ApplyChanges(todo, command))
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

    private static Result<Todo> ApplyChanges(Todo todo, UpdateTodoCommand command)
    {
        Result<Todo> updated = Result<Todo>.Ok(todo);

        if (command.Title is not null)
        {
            updated = updated.SetTitle(command.Title);
        }

        if (command.ClearDescription)
        {
            updated = updated.SetDescription(null);
        }
        else if (command.Description is not null)
        {
            updated = updated.SetDescription(command.Description);
        }

        if (command.Priority is { } priority)
        {
            updated = updated.SetPriority(priority);
        }

        if (command.ClearDueAt)
        {
            updated = updated.ClearDueDate();
        }
        else if (command.DueAtUtc is { } dueAtUtc)
        {
            updated = updated.SetDueDate(dueAtUtc);
        }

        if (command.ListId is { } listId)
        {
            updated = updated.AssignToList(listId);
        }
        else if (command.ClearList)
        {
            updated = updated.ClearList();
        }

        return updated;
    }
}
