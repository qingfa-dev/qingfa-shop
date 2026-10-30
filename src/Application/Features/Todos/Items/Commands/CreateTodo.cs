using Application.Common.EventSourcing;
using Application.Features.Todos.Items.Mappings;
using Application.Features.Todos.Items.Models;
using Application.Features.Todos.Items.Validators;

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

            Result<Guid> tenant = tenantContext.RequireTenant();
            if (tenant.IsFailure)
                return Result<Response>.Fail(tenant.Errors);

            DateTimeOffset nowUtc = clock.GetUtcNow();
            string? actor = actorContext.Actor();

            Result<Todo> created = Todo.Create(
                id: Guid.NewGuid(),
                tenantId: tenant.Value,
                title: request.Title,
                nowUtc: nowUtc,
                actor: actor,
                description: request.Description,
                priority: request.Priority ?? TodoConstant.Defaults.Priority,
                dueAtUtc: request.DueAtUtc,
                listId: request.ListId);
            if (created.IsFailure)
                return Result<Response>.Fail(created.Errors);

            foreach (Guid categoryId in request.CategoryIds)
            {
                Result added = created.Value.AddCategory(categoryId, nowUtc, actor);
                if (added.IsFailure)
                    return Result<Response>.Fail(added.Errors);
            }

            Result saved = await repository
                .SaveAsync(created.Value, cancellationToken)
                .ConfigureAwait(false);

            return saved.IsFailure
                ? Result<Response>.Fail(saved.Errors)
                : Result<Response>.Success(created.Value.ToModel<Response>());
        }
    }
    #endregion
}
