using Application.Features.Todos.Categories.Mappers;
using Application.Features.Todos.Categories.Models;

using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Todos.Categories;

using FluentValidation;

namespace Application.Features.Todos.Categories.Commands;

public static class RestoreTodoCategory
{
    #region Request
    public sealed record Request(Guid Id);
    #endregion

    #region Response
    public sealed record Response : TodoCategoryResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : ICommand<Response>;
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
        IPersistentRepository<TodoCategory, Guid> repository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IUserContext actorContext,
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

            return await tenantContext.RequireTenant()
                .BindAsync(
                    (tenantId, ct) => repository.FindOwnedAsync(
                        request.Id,
                        tenantId,
                        ct,
                        includeDeleted: true),
                    cancellationToken)
                .BindAsync(category =>
                    Result<TodoCategory>.Ok(category).Restore(actorContext.Actor()))
                .BindAsync(
                    (category, ct) => repository.SaveUpdateAsync(
                        unitOfWork,
                        category,
                        clock.GetUtcNow(),
                        actorContext.Actor(),
                        ct),
                    cancellationToken)
                .MapAsync(category => category.ToModel<Response>());
        }
    }
    #endregion
}
