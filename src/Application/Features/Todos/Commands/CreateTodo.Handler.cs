using Application.Common.Contexts;
using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;
using Application.Common.Persistence.UnitOfWorks;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Todos.Items;

namespace Application.Features.Todos.Commands;

/// <summary>Handles <see cref="CreateTodoCommand"/>.</summary>
/// <param name="repository">The todo repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="tenantContext">The ambient tenant.</param>
/// <param name="actorContext">The ambient actor.</param>
/// <param name="clock">The clock used for timestamps.</param>
public sealed class CreateTodoHandler(
    IPersistentRepository<Todo, Guid> repository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IActorContext actorContext,
    TimeProvider clock)
    : ICommandHandler<CreateTodoCommand, TodoModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoModel>> Handle(
        CreateTodoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await tenantContext.RequireTenant()
            .Bind(tenantId => Todo.Create(
                id: Guid.NewGuid(),
                tenantId: tenantId,
                title: command.Title,
                nowUtc: clock.GetUtcNow(),
                actor: actorContext.Actor,
                description: command.Description,
                priority: command.Priority,
                dueAtUtc: command.DueAtUtc,
                listId: command.ListId))
            .Bind(created => AddCategories(created, command.CategoryIds))
            .BindAsync(
                (created, ct) => repository.SaveNewAsync(unitOfWork, created, ct),
                cancellationToken)
            .MapAsync(created => created.ToModel());
    }

    private static Result<Todo> AddCategories(
        Result<Todo> created,
        IReadOnlyCollection<Guid>? categoryIds)
    {
        if (categoryIds is null || categoryIds.Count == 0)
        {
            return created;
        }

        return categoryIds.Aggregate(
            created,
            (current, categoryId) => current.AddCategory(categoryId));
    }
}
