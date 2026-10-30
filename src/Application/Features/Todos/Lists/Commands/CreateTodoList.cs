using Application.Features.Todos.Lists.Models;
using Application.Features.Todos.Lists.Validators;
using Application.Features.Todos.Lists.Mappings;

using Core.Todos.Lists;

using FluentValidation;

namespace Application.Features.Todos.Lists.Commands;

public static class CreateTodoList
{
    #region Request
    public sealed record Request : TodoListRequestModel;
    #endregion

    #region Response
    public sealed record Response : TodoListResponseModel;
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
                .ApplyTodoListRules();
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IPersistentRepository<TodoList, Guid> repository,
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

            Result<Response> response = await tenantContext.RequireTenant()
                .Bind(tenantId => TodoList.Create(
                    id: Guid.NewGuid(),
                    tenantId: tenantId,
                    name: request.Name,
                    nowUtc: clock.GetUtcNow(),
                    actor: actorContext.Actor(),
                    description: request.Description))
                .BindAsync(
                    (created, ct) => repository.SaveNewAsync(unitOfWork, created, ct),
                    cancellationToken)
                .MapAsync(created => created.ToModel<Response>());

            return response.IsSuccess
                ? response.WithStatus(ResultConstant.StatusCode.Created)
                : response;
        }
    }
    #endregion
}
