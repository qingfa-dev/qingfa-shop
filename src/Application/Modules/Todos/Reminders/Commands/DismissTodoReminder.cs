using Application.BuildingBlocks.EventSourcing;

using Core.Modules.Todos.Reminders;

using FluentValidation;

namespace Application.Modules.Todos.Reminders.Commands;

public static class DismissTodoReminder
{
    #region Request
    public sealed record Request(Guid ReminderId, Guid TodoId);
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.ReminderId)
                .NotEmpty();

            RuleFor(command => command.Request.TodoId)
                .NotEmpty();
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IEventSourcedRepository<TodoReminder, Guid> repository,
        ITenantContext tenantContext,
        TimeProvider clock)
        : ICommandHandler<UseCase>
    {
        public async Task<Result> Handle(
            UseCase command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            Result<Guid> tenant = tenantContext.RequireTenant();
            if (tenant.IsFailure)
                return Result.Fail(tenant.Errors);

            Result<TodoReminder> loaded = await repository
                .LoadAsync(command.Request.ReminderId, cancellationToken)
                .ConfigureAwait(false);
            if (loaded.IsFailure)
                return Result.Fail(loaded.Errors);

            // Guard: a stream outside the caller's tenant or todo is reported as missing.
            if (loaded.Value.TenantId != tenant.Value ||
                loaded.Value.TodoId != command.Request.TodoId)
            {
                return Result.Fail(TodoReminderOutcome.Failure.NotFound);
            }

            Result dismissed = loaded.Value.Dismiss(clock.GetUtcNow());
            if (dismissed.IsFailure)
                return Result.Fail(dismissed.Errors);

            Result saved = await repository
                .SaveAsync(loaded.Value, cancellationToken)
                .ConfigureAwait(false);

            return saved.IsFailure
                ? Result.Fail(saved.Errors)
                : Result.NoContent();
        }
    }
    #endregion
}
