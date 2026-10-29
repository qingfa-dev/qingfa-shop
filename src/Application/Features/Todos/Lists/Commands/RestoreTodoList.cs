using Application.Features.Todos.Lists.Models;
using Application.Features.Todos.Lists.Mappings;

using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Todos.Lists;

using FluentValidation;

namespace Application.Features.Todos.Lists.Commands;

public static class RestoreTodoList
{
    #region Request
    public sealed record Request(Guid Id);
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
                    (tenantId, ct) => repository.FindOwnedAsync(
                        request.Id,
                        tenantId,
                        ct,
                        includeDeleted: true),
                    cancellationToken)
                .BindAsync(list =>
                    Result<TodoList>.Ok(list).Restore(actorContext.Actor()))
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
    }
    #endregion
}
