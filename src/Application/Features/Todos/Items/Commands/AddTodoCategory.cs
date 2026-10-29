using Application.Features.Todos.Items.Mappings;
using Application.Features.Todos.Items.Models;
using Application.Features.Todos.Mapping;

using Core.Todos.Items;

using FluentValidation;

namespace Application.Features.Todos.Items.Commands;

public static class AddTodoCategory
{
    #region Request
    public sealed record Request(Guid Id, Guid CategoryId);
    #endregion

    #region Response
    public sealed record Response : TodoResponseModel;
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

            RuleFor(command => command.Request.CategoryId)
                .NotEmpty();
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IPersistentRepository<Todo, Guid> repository,
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
                        ct),
                    cancellationToken)
                .BindAsync(todo => Result<Todo>.Ok(todo).AddCategory(request.CategoryId))
                .BindAsync(
                    (todo, ct) => repository.SaveUpdateAsync(
                        unitOfWork,
                        todo,
                        clock.GetUtcNow(),
                        actorContext.Actor(),
                        ct),
                    cancellationToken)
                .MapAsync(todo => todo.ToModel<Response>());
        }
    }
    #endregion
}
