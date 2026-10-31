using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Todos.Reminders.Mappings;
using Application.Modules.Todos.Reminders.Models;
using Application.Modules.Todos.Reminders.Validators;

using Core.Modules.Todos.Reminders;

using FluentValidation;

namespace Application.Modules.Todos.Reminders.Commands;

public static class SnoozeTodoReminder
{
    #region Request
    public sealed record Request : TodoReminderRequestModel
    {
        /// <summary>Gets or sets the todo the reminder belongs to.</summary>
        public Guid TodoId { get; set; }

        /// <summary>Gets or sets the reminder to move forward.</summary>
        public Guid ReminderId { get; set; }
    }
    #endregion

    #region Response
    public sealed record Response : TodoReminderResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<Response>;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.TodoId)
                .NotEmpty();

            RuleFor(command => command.Request.ReminderId)
                .NotEmpty();

            RuleFor(command => command.Request)
                .ApplyTodoReminderRules();
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IEventSourcedRepository<TodoReminder, Guid> repository,
        ITenantContext tenantContext,
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

            Result<TodoReminder> loaded = await repository
                .LoadAsync(request.ReminderId, cancellationToken)
                .ConfigureAwait(false);
            if (loaded.IsFailure)
                return Result<Response>.Fail(loaded.Errors);

            // Guard: a stream outside the caller's tenant or todo is reported as missing.
            if (loaded.Value.TenantId != tenant.Value ||
                loaded.Value.TodoId != request.TodoId)
            {
                return Result<Response>.Fail(TodoReminderOutcome.Failure.NotFound);
            }

            Result snoozed = loaded.Value.Snooze(request.RemindAtUtc, clock.GetUtcNow());
            if (snoozed.IsFailure)
                return Result<Response>.Fail(snoozed.Errors);

            Result saved = await repository
                .SaveAsync(loaded.Value, cancellationToken)
                .ConfigureAwait(false);

            return saved.IsFailure
                ? Result<Response>.Fail(saved.Errors)
                : Result<Response>.Success(loaded.Value.ToModel<Response>());
        }
    }
    #endregion
}
