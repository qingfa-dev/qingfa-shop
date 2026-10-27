using Application.Common.Contexts;
using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Todos;
using Core.Todos.Categories;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="UpdateTodoCategoryCommand"/>.</summary>
/// <param name="repository">The category repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class UpdateTodoCategoryHandler(
    IPersistentRepository<TodoCategory, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IActorContext actorContext,
    TimeProvider clock)
    : ICommandHandler<UpdateTodoCategoryCommand, TodoCategoryModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoCategoryModel>> Handle(
        UpdateTodoCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await tenantContext.RequireTenant()
            .BindAsync(
                (tenantId, ct) => repository.FindOwnedAsync(command.Id, tenantId, ct),
                cancellationToken)
            .BindAsync(category => ApplyChanges(category, command))
            .BindAsync(
                (category, ct) => repository.SaveUpdateAsync(
                    unitOfWork,
                    category,
                    clock.GetUtcNow(),
                    actorContext.Actor,
                    ct),
                cancellationToken)
            .MapAsync(category => category.ToModel());
    }

    private static Result<TodoCategory> ApplyChanges(
        TodoCategory category,
        UpdateTodoCategoryCommand command)
    {
        Result<TodoCategory> updated = Result<TodoCategory>.Ok(category);

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
