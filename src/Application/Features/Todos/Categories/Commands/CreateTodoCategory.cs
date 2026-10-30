using Application.Features.Todos.Categories.Mappers;
using Application.Features.Todos.Categories.Models;
using Application.Features.Todos.Categories.Validators;

using Core.Todos.Categories;

using FluentValidation;

namespace Application.Features.Todos.Categories.Commands;

public static class CreateTodoCategory
{
    #region Request
    public sealed record Request : TodoCategoryRequestModel;
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
            RuleFor(command => command.Request)
                .ApplyTodoCategoryRules();
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

            Result<Response> response = await tenantContext.RequireTenant()
                .Bind(tenantId => TodoCategory.Create(
                    id: Guid.NewGuid(),
                    tenantId: tenantId,
                    name: request.Name,
                    nowUtc: clock.GetUtcNow(),
                    actor: actorContext.Actor(),
                    description: request.Description))
                .BindAsync(
                    (created, ct) => repository.SaveNewAsync(unitOfWork, created, ct),
                    cancellationToken)
                .MapAsync(created => created.ToModel<Response>());

            return response.IsSuccess
                ? response.WithStatus(ResultConstant.StatusCode.Created)
                : response;
        }
    }
    #endregion
}
