using Application.Common.EventSourcing;
using Application.Features.Todos.Reminders.Mappings;
using Application.Features.Todos.Reminders.Models;
using Application.Features.Todos.Reminders.Validators;

using Core.Todos.Reminders;

using FluentValidation;

namespace Application.Features.Todos.Reminders.Commands;

public static class ScheduleTodoReminder
{
    #region Request
    public sealed record Request : TodoReminderRequestModel
    {
        /// <summary>Gets or sets the todo the reminder belongs to.</summary>
        public Guid TodoId { get; set; }
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

            Result<TodoReminder> created = TodoReminder.Create(
                id: Guid.NewGuid(),
                todoId: request.TodoId,
                tenantId: tenant.Value,
                remindAtUtc: request.RemindAtUtc,
                nowUtc: clock.GetUtcNow());
            if (created.IsFailure)
                return Result<Response>.Fail(created.Errors);

            Result saved = await repository
                .SaveAsync(created.Value, cancellationToken)
                .ConfigureAwait(false);

            return saved.IsFailure
                ? Result<Response>.Fail(saved.Errors)
                : Result<Response>.Created(created.Value.ToModel<Response>());
        }
    }
    #endregion
}
