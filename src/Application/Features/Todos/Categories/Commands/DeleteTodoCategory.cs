using Application.Features.Todos.Abstractions;

using Core.Todos.Categories;

using FluentValidation;

namespace Application.Features.Todos.Categories.Commands;

public static class DeleteTodoCategory
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
    /// <summary>Deletes a category and removes its reference from every todo.</summary>
    /// <remarks>
    /// Each todo stream commits independently, so a failed category delete can
    /// leave references cleared. The operation is idempotent and safe to retry.
    /// </remarks>
    public sealed class UseCaseHandler(
        IPersistentRepository<TodoCategory, Guid> repository,
        ITodoReferenceCleaner cleaner,
        IUnitOfWork unitOfWork,
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

            Result outcome = await tenantContext.RequireTenant()
                .BindAsync(
                    (tenantId, ct) => repository.FindOwnedAsync(
                        request.Id,
                        tenantId,
                        ct,
                        includeDeleted: true),
                    cancellationToken)
                .BindAsync(
                    async (category, ct) => (await cleaner.RemoveCategoryAsync(
                            category.TenantId,
                            category.Id,
                            nowUtc,
                            actorContext.Actor(),
                            ct))
                        .Map(_ => category),
                    cancellationToken)
                .BindAsync(
                    (category, ct) => repository.SaveDeleteAsync(
                        unitOfWork,
                        category,
                        request.Mode,
                        nowUtc,
                        actorContext.Actor(),
                        ct),
                    cancellationToken);

            return outcome.IsSuccess
                ? outcome.WithStatus(ResultConstant.StatusCode.NoContent)
                : outcome;
        }
    }
    #endregion
}
