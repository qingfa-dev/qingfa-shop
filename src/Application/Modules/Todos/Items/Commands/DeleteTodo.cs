using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Todos.Common;

using Core.Modules.Todos.Items;

using FluentValidation;

namespace Application.Modules.Todos.Items.Commands;

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
        IEventSourcedRepository<Todo, Guid> repository,
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
            string? actor = actorContext.Actor();

            Result<Todo> deleted = await tenantContext.RequireTenant()
                .BindAsync(
                    (tenantId, ct) => repository.LoadOwnedAsync(
                        request.Id,
                        tenantId,
                        ct,
                        includeDeleted: true),
                    cancellationToken)
                .BindAsync(todo => todo.Delete(
                    request.Mode == DeleteMode.Hard,
                    nowUtc,
                    actor))
                .ThenAsync(
                    (todo, ct) => repository.SaveAsync(todo, ct),
                    cancellationToken);

            Result outcome = deleted.ToResult();
            return outcome.IsSuccess
                ? outcome.WithStatus(ResultConstant.StatusCode.NoContent)
                : outcome;
        }
    }
    #endregion
}
