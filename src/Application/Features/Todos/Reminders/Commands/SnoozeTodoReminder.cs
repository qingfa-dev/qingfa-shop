using Application.Common.EventSourcing;
using Application.Features.Todos.Reminders.Models;

using Core.Todos.Reminders;

using FluentValidation;

namespace Application.Features.Todos.Reminders.Commands;

public static class SnoozeTodoReminder
{
    public sealed record Request(Guid ReminderId, DateTimeOffset RemindAtUtc);
    public sealed record Response : TodoReminderModel;
    public sealed record UseCase(Request Request) : ICommand<Response>;

    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.ReminderId).NotEmpty();
            RuleFor(command => command.Request.RemindAtUtc).NotEmpty();
        }
    }

    public sealed class UseCaseHandler(
        IEventSourcedRepository<TodoReminder, Guid> repository,
        TimeProvider clock)
        : ICommandHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            Result<TodoReminder> loaded = await repository.LoadAsync(command.Request.ReminderId, cancellationToken);
            if (loaded.IsFailure)
                return Result<Response>.Fail(loaded.Errors);

            Result snoozed = loaded.Value.Snooze(command.Request.RemindAtUtc, clock.GetUtcNow());
            if (snoozed.IsFailure)
                return Result<Response>.Fail(snoozed.Errors);

            Result saved = await repository.SaveAsync(loaded.Value, cancellationToken);
            if (saved.IsFailure)
                return Result<Response>.Fail(saved.Errors);

            return Result<Response>.Success(ToResponse(loaded.Value));
        }

        internal static Response ToResponse(TodoReminder reminder) => new()
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
