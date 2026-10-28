using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Security.Authentication.Contexts;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Todos.Categories;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="RestoreTodoCategoryCommand"/>.</summary>
/// <param name="repository">The category repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class RestoreTodoCategoryHandler(
    IPersistentRepository<TodoCategory, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IUserContext actorContext,
    TimeProvider clock)
    : ICommandHandler<RestoreTodoCategoryCommand, TodoCategoryModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoCategoryModel>> Handle(
        RestoreTodoCategoryCommand command,
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
            .BindAsync(category => Result<TodoCategory>.Ok(category).Restore(actorContext.Actor()))
            .BindAsync(
                (category, ct) => repository.SaveUpdateAsync(
                    unitOfWork,
                    category,
                    clock.GetUtcNow(),
                    actorContext.Actor(),
                    ct),
                cancellationToken)
            .MapAsync(category => category.ToModel());
    }
}
