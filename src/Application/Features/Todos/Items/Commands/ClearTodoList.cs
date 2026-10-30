using Application.Common.EventSourcing;
using Application.Features.Todos.Common;
using Application.Features.Todos.Items.Mappings;
using Application.Features.Todos.Items.Models;

using Core.Todos.Items;

using FluentValidation;

namespace Application.Features.Todos.Items.Commands;

public static class ClearTodoList
{
    #region Request
    public sealed record Request(Guid Id);
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
        }
    }
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IEventSourcedRepository<Todo, Guid> repository,
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
            DateTimeOffset nowUtc = clock.GetUtcNow();
            string? actor = actorContext.Actor();

            return await tenantContext.RequireTenant()
                .BindAsync(
                    (tenantId, ct) => repository.LoadOwnedAsync(request.Id, tenantId, ct),
                    cancellationToken)
                .BindAsync(todo => todo.ClearList(nowUtc, actor))
                .ThenAsync(
                    (todo, ct) => repository.SaveAsync(todo, ct),
                    cancellationToken)
                .MapAsync(todo => todo.ToModel<Response>());
        }
    }
    #endregion
}
