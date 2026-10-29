using Application.Features.Todos.Abstractions;

using Core.Todos.Lists;

using FluentValidation;

namespace Application.Features.Todos.Lists.Commands;

public static class DeleteTodoList
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
        IPersistentRepository<TodoList, Guid> repository,
        ITodoReferenceCleaner cleaner,
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
            DateTimeOffset nowUtc = clock.GetUtcNow();

            return await tenantContext.RequireTenant()
                .BindAsync(
                    (tenantId, ct) => repository.FindOwnedAsync(
                        request.Id,
                        tenantId,
                        ct,
                        includeDeleted: true),
                    cancellationToken)
                .BindAsync(
                    async (list, ct) => (await cleaner.ClearListAsync(
                            list.TenantId,
                            list.Id,
                            nowUtc,
                            actorContext.Actor(),
                            ct))
                        .Map(_ => list),
                    cancellationToken)
                .BindAsync(
                    (list, ct) => repository.SaveDeleteAsync(
                        unitOfWork,
                        list,
                        request.Mode,
                        nowUtc,
                        actorContext.Actor(),
                        ct),
                    cancellationToken);
        }
    }
    #endregion
}
