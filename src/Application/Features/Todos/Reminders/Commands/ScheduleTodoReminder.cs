using Application.Common.Persistence.UnitOfWorks;
using Application.Common.Security.Authentication.Contexts;
using Application.Common.Persistence.Repositories;
using Application.Common.EventSourcing;
using Application.Features.Todos.Reminders.Models;

using Core.Todos.Reminders;

using FluentValidation;

namespace Application.Features.Todos.Reminders.Commands;

public static class ScheduleTodoReminder
{
    public sealed record Request(Guid TodoId, DateTimeOffset RemindAtUtc);
    public sealed record Response : TodoReminderModel;
    public sealed record UseCase(Request Request) : ICommand<Response>;

    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.TodoId).NotEmpty();
            RuleFor(command => command.Request.RemindAtUtc).NotEmpty();
        }
    }

    public sealed class UseCaseHandler(
        IEventSourcedRepository<TodoReminder, Guid> repository,
        ITenantContext tenantContext,
        TimeProvider clock)
        : ICommandHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            Result<Guid> tenant = tenantContext.RequireTenant();
            if (tenant.IsFailure)
                return Result<Response>.Fail(tenant.Errors);

            DateTimeOffset nowUtc = clock.GetUtcNow();
            Result<TodoReminder> created = TodoReminder.Create(
                Guid.NewGuid(),
                command.Request.TodoId,
                tenant.Value,
                command.Request.RemindAtUtc,
                nowUtc);

            if (created.IsFailure)
                return Result<Response>.Fail(created.Errors);

            Result saved = await repository.SaveAsync(created.Value, cancellationToken);
            if (saved.IsFailure)
                return Result<Response>.Fail(saved.Errors);

            return Result<Response>.Success(ToResponse(created.Value));
        }

        private static Response ToResponse(TodoReminder reminder) => new()
        {
            Id = reminder.Id,
            TodoId = reminder.TodoId,
            TenantId = reminder.TenantId,
            RemindAtUtc = reminder.RemindAtUtc,
            Status = reminder.Status,
            SnoozeCount = reminder.SnoozeCount,
            DismissedAtUtc = reminder.DismissedAtUtc,
            Version = reminder.Version,
        };
    }
}
