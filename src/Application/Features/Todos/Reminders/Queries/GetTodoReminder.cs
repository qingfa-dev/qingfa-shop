using Application.Common.Mediators.Queries;

using Application.Common.Persistence.Repositories;
using Application.Features.Todos.Reminders.Models;

using FluentValidation;

namespace Application.Features.Todos.Reminders.Queries;

public static class GetTodoReminder
{
    public sealed record Request(Guid ReminderId);
    public sealed record Response : TodoReminderModel;
    public sealed record UseCase(Request Request) : IQuery<Response>;

    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(query => query.Request.ReminderId).NotEmpty();
        }
    }

    public sealed class UseCaseHandler(IReadableRepository<TodoReminderModel, Guid> repository)
        : IQueryHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(UseCase query, CancellationToken cancellationToken)
        {
            Result<TodoReminderModel> result = await repository.GetByIdAsync(
                query.Request.ReminderId,
                cancellationToken);

            return result.IsFailure
                ? Result<Response>.Fail(result.Errors)
                : Result<Response>.Success(new Response
                {
                    Id = result.Value.Id,
                    TodoId = result.Value.TodoId,
                    TenantId = result.Value.TenantId,
                    RemindAtUtc = result.Value.RemindAtUtc,
                    Status = result.Value.Status,
                    SnoozeCount = result.Value.SnoozeCount,
                    DismissedAtUtc = result.Value.DismissedAtUtc,
                    Version = result.Value.Version,
                });
        }
    }
}
