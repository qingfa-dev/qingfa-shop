using Application.Common.EventSourcing;
using Application.Features.Todos.Common;

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

            Result<Guid> tenant = tenantContext.RequireTenant();
            if (tenant.IsFailure)
                return Result.Fail(tenant.Errors);

            Result<Todo> loaded = await repository
                .LoadOwnedAsync(request.Id, tenant.Value, cancellationToken)
                .ConfigureAwait(false);
            if (loaded.IsFailure)
                return Result.Fail(loaded.Errors);

            Result deleted = loaded.Value.Delete(
                request.Mode == DeleteMode.Hard,
                clock.GetUtcNow(),
                actorContext.Actor());
            if (deleted.IsFailure)
                return deleted;

            return await repository
                .SaveAsync(loaded.Value, cancellationToken)
                .ConfigureAwait(false);
        }
    }
    #endregion
}
