using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Security.Authentication.Contexts;
using Application.Features.Todos.Abstractions;

using Core.Todos.Categories;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="DeleteTodoCategoryCommand"/>.</summary>
/// <param name="repository">The category repository.</param>
/// <param name="cleaner">Removes the category reference from todos.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class DeleteTodoCategoryHandler(
    IPersistentRepository<TodoCategory, Guid> repository,
    ITodoReferenceCleaner cleaner,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IUserContext actorContext,
    TimeProvider clock)
    : ICommandHandler<DeleteTodoCategoryCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(
        DeleteTodoCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DateTimeOffset nowUtc = clock.GetUtcNow();

        return await tenantContext.RequireTenant()
            .BindAsync(
                (tenantId, ct) => repository.FindOwnedAsync(
                    command.Id,
                    tenantId,
                    ct,
                    includeDeleted: true),
                cancellationToken)
            .BindAsync(
                async (category, ct) => (await cleaner.RemoveCategoryAsync(
                        category.TenantId,
                        category.Id,
                        nowUtc,
                        actorContext.Actor(),
                        ct))
                    .Map(_ => category),
                cancellationToken)
            .BindAsync(
                (category, ct) => repository.SaveDeleteAsync(
                    unitOfWork,
                    category,
                    command.Mode,
                    nowUtc,
                    actorContext.Actor(),
                    ct),
                cancellationToken);
    }
}
