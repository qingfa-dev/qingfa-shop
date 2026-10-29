using Application.Features.Todos.Items.Models;
using Application.Features.Todos.Items.Validators;
using Application.Features.Todos.Mapping;

using Core.Todos.Items;

using FluentValidation;

namespace Application.Features.Todos.Items.Commands;

public static class CreateTodo
{
    #region Request
    public sealed record Request : TodoRequestModel;
    #endregion

    #region Response
    public sealed record Response : TodoResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<Response>;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request)
                .ApplyTodoRules();
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IPersistentRepository<Todo, Guid> repository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IUserContext actorContext,
        TimeProvider clock)
        : ICommandHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Request request = command.Request;

            return await tenantContext.RequireTenant()
                .Bind(tenantId => Todo.Create(
                    id: Guid.NewGuid(),
                    tenantId: tenantId,
                    title: request.Title,
                    nowUtc: clock.GetUtcNow(),
                    actor: actorContext.Actor(),
                    description: request.Description,
                    priority: request.Priority ?? TodoConstant.Defaults.Priority,
                    dueAtUtc: request.DueAtUtc,
                    listId: request.ListId))
                .Bind(created => AddCategories(created, request.CategoryIds))
                .BindAsync(
                    (created, ct) => repository.SaveNewAsync(unitOfWork, created, ct),
                    cancellationToken)
                .MapAsync(created => created.ToModel<Response>());
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
    #endregion
}
