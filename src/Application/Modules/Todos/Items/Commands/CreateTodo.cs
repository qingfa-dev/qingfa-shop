using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Todos.Items.Mappings;
using Application.Modules.Todos.Items.Models;
using Application.Modules.Todos.Items.Validators;

using Core.Modules.Todos.Items;

using FluentValidation;

namespace Application.Modules.Todos.Items.Commands;

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
        IEventSourcedRepository<Todo, Guid> repository,
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
            DateTimeOffset nowUtc = clock.GetUtcNow();
            string? actor = actorContext.Actor();

            Result<Response> response = await tenantContext.RequireTenant()
                .Bind(tenantId => Todo.Create(
                    id: Guid.NewGuid(),
                    tenantId: tenantId,
                    title: request.Title,
                    nowUtc: nowUtc,
                    actor: actor,
                    description: request.Description,
                    priority: request.Priority ?? TodoConstant.Defaults.Priority,
                    dueAtUtc: request.DueAtUtc,
                    listId: request.ListId))
                .Bind(created => AddCategories(created, request.CategoryIds, nowUtc, actor))
                .ThenAsync(
                    (todo, ct) => repository.SaveAsync(todo, ct),
                    cancellationToken)
                .MapAsync(todo => todo.ToModel<Response>());

            return response.IsSuccess
                ? response.WithStatus(ResultConstant.StatusCode.Created)
                : response;
        }

        private static Result<Todo> AddCategories(
            Result<Todo> created,
            IReadOnlyCollection<Guid>? categoryIds,
            DateTimeOffset nowUtc,
            string? actor)
            => categoryIds is null || categoryIds.Count == 0
                ? created
                : categoryIds.Aggregate(
                    created,
                    (current, categoryId) => current.Bind(
                        todo => todo.AddCategory(categoryId, nowUtc, actor)));
        }
    #endregion
}
