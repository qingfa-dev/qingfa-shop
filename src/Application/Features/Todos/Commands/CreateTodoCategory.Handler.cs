using Application.Common.Contexts;
using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Todos;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="CreateTodoCategoryCommand"/>.</summary>
/// <param name="repository">The category repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class CreateTodoCategoryHandler(
    IPersistentRepository<TodoCategory, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IActorContext actorContext,
    TimeProvider clock)
    : ICommandHandler<CreateTodoCategoryCommand, TodoCategoryModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoCategoryModel>> Handle(
        CreateTodoCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await tenantContext.RequireTenant()
            .Bind(tenantId => TodoCategory.Create(
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
