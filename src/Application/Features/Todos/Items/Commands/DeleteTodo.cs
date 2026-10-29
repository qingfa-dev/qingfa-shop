using Core.Todos.Items;

using FluentValidation;

namespace Application.Features.Todos.Items.Commands;

public static class DeleteTodo
{
    #region Request
    public sealed record Request(Guid Id, DeleteMode Mode = DeleteMode.Soft);
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand;
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
        IPersistentRepository<Todo, Guid> repository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IUserContext actorContext,
        TimeProvider clock)
        : ICommandHandler<UseCase>
    {
        public async Task<Result> Handle(
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
                .BindAsync(
                    (todo, ct) => repository.SaveDeleteAsync(
                        unitOfWork,
                        todo,
                        request.Mode,
                        clock.GetUtcNow(),
                        actorContext.Actor(),
                        ct),
                    cancellationToken);
        }
    }
    #endregion
}
