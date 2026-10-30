using Application.Common.Mediators.Queries;
using Application.Features.Todos.Reminders.Mappings;
using Application.Features.Todos.Reminders.Models;

using FluentValidation;

using Core.Todos.Reminders;

namespace Application.Features.Todos.Reminders.Queries;

public static class GetTodoReminder
{
    #region Request
    public sealed record Request(Guid ReminderId, Guid? TodoId = null);
    #endregion

    #region Response
    public sealed record Response : TodoReminderResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : IQuery<Response>;
    #endregion

    #region Validator
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        public UseCaseValidator()
        {
            RuleFor(query => query.Request.ReminderId)
                .NotEmpty();
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IReadableRepository<TodoReminderModel, Guid> repository,
        ITenantContext tenantContext)
        : IQueryHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(query.Request);

            Result<Guid> tenant = tenantContext.RequireTenant();
            if (tenant.IsFailure)
                return Result<Response>.Fail(tenant.Errors);

            Result<TodoReminderModel> found = await repository
                .GetByIdAsync(query.Request.ReminderId, cancellationToken)
                .ConfigureAwait(false);
            if (found.IsFailure)
                return Result<Response>.Fail(found.Errors);

            // Guard: a projection row outside the caller's tenant or todo is reported as missing.
            bool owned = found.Value.TenantId == tenant.Value &&
                (query.Request.TodoId is not Guid todoId || found.Value.TodoId == todoId);

            return owned
                ? Result<Response>.Success(found.Value.ToModel<Response>())
                : Result<Response>.Fail(TodoReminderOutcome.Failure.NotFound);
        }
    }
    #endregion
}
