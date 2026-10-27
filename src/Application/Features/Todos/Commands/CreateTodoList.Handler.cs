using Application.Common.Contexts;
using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Todos;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="CreateTodoListCommand"/>.</summary>
/// <param name="repository">The list repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class CreateTodoListHandler(
    IPersistentRepository<TodoList, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IActorContext actorContext,
    TimeProvider clock)
    : ICommandHandler<CreateTodoListCommand, TodoListModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoListModel>> Handle(
        CreateTodoListCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await tenantContext.RequireTenant()
            .Bind(tenantId => TodoList.Create(
                id: Guid.NewGuid(),
                tenantId: tenantId,
                name: command.Name,
                nowUtc: clock.GetUtcNow(),
                actor: actorContext.Actor,
                description: command.Description))
            .BindAsync(
                (created, ct) => repository.SaveNewAsync(unitOfWork, created, ct),
                cancellationToken)
            .MapAsync(created => created.ToModel());
    }
}
