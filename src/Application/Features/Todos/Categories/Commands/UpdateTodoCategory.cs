using Application.Features.Todos.Categories.Mappers;
using Application.Features.Todos.Categories.Models;
using Application.Features.Todos.Categories.Validators;
using Application.Features.Todos.Mapping;

using Core.Todos.Categories;

using FluentValidation;

namespace Application.Features.Todos.Categories.Commands;

public static class UpdateTodoCategory
{
    #region Request
    public sealed record Request : TodoCategoryRequestModel
    {
        public Guid Id { get; set; }

        public bool ClearDescription { get; set; }
    }
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

            RuleFor(command => command.Request)
                .ApplyTodoCategoryRules(isPatch: true);
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
                    (tenantId, ct) => repository.FindOwnedAsync(request.Id, tenantId, ct),
                    cancellationToken)
                .BindAsync(category => ApplyChanges(category, request))
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

        private static Result<TodoCategory> ApplyChanges(
            TodoCategory category,
            Request request)
        {
            Result<TodoCategory> updated = Result<TodoCategory>.Ok(category);

            if (request.Name is not null)
            {
                updated = updated.SetName(request.Name);
            }

            if (request.ClearDescription)
            {
                updated = updated.SetDescription(null);
            }
            else if (request.Description is not null)
            {
                updated = updated.SetDescription(request.Description);
            }

            return updated;
        }
    }
    #endregion
}
