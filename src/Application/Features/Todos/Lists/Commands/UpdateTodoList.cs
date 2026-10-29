using Application.Features.Todos.Lists.Models;
using Application.Features.Todos.Lists.Validators;
using Application.Features.Todos.Mapping;

using Core.Todos.Lists;

using FluentValidation;

namespace Application.Features.Todos.Lists.Commands;

public static class UpdateTodoList
{
    #region Request
    public sealed record Request : TodoListRequestModel
    {
        public Guid Id { get; set; }

        public bool ClearDescription { get; set; }
    }
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
            RuleFor(command => command.Request.Id)
                .NotEmpty();

            RuleFor(command => command.Request)
                .ApplyTodoListRules(isPatch: true);
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

            return await tenantContext.RequireTenant()
                .BindAsync(
                    (tenantId, ct) => repository.FindOwnedAsync(request.Id, tenantId, ct),
                    cancellationToken)
                .BindAsync(list => ApplyChanges(list, request))
                .BindAsync(
                    (list, ct) => repository.SaveUpdateAsync(
                        unitOfWork,
                        list,
                        clock.GetUtcNow(),
                        actorContext.Actor(),
                        ct),
                    cancellationToken)
                .MapAsync(list => list.ToModel<Response>());
        }

        private static Result<TodoList> ApplyChanges(
            TodoList list,
            Request request)
        {
            Result<TodoList> updated = Result<TodoList>.Ok(list);

            if (request.Name is not null)
            {
                updated = updated.SetName(request.Name);
            }

            if (request.ClearDescription)
            {
                updated = updated.SetDescription(null);
            }
            else if (request.Description is not null)
            {
                updated = updated.SetDescription(request.Description);
            }

            return updated;
        }
    }
    #endregion
}
