using Application.Common.EventSourcing;
using Application.Features.Todos.Reminders.Models;

using Core.Todos.Reminders;

using FluentValidation;

namespace Application.Features.Todos.Reminders.Commands;

public static class DismissTodoReminder
{
    public sealed record Request(Guid ReminderId);
    public sealed record Response : TodoReminderModel;
    public sealed record UseCase(Request Request) : ICommand<Response>;

    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(command => command.Request.ReminderId).NotEmpty();
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

            Result dismissed = loaded.Value.Dismiss(clock.GetUtcNow());
            if (dismissed.IsFailure)
                return Result<Response>.Fail(dismissed.Errors);

            Result saved = await repository.SaveAsync(loaded.Value, cancellationToken);
            if (saved.IsFailure)
                return Result<Response>.Fail(saved.Errors);

            return Result<Response>.Success(new Response
            {
                Id = loaded.Value.Id,
                TodoId = loaded.Value.TodoId,
                TenantId = loaded.Value.TenantId,
                RemindAtUtc = loaded.Value.RemindAtUtc,
                Status = loaded.Value.Status,
                SnoozeCount = loaded.Value.SnoozeCount,
                DismissedAtUtc = loaded.Value.DismissedAtUtc,
                Version = loaded.Value.Version,
            });
        }
    }
}
