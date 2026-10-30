using Application.Common.EventSourcing;
using Application.Features.Todos.Common;
using Application.Features.Todos.Items.Mappings;
using Application.Features.Todos.Items.Models;

using Core.Todos.Items;

using FluentValidation;

namespace Application.Features.Todos.Items.Commands;

public static class AssignTodoToList
{
    #region Request
    public sealed record Request(Guid Id, Guid ListId);
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

            RuleFor(command => command.Request.ListId)
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

            Result<Guid> tenant = tenantContext.RequireTenant();
            if (tenant.IsFailure)
                return Result<Response>.Fail(tenant.Errors);

            Result<Todo> loaded = await repository
                .LoadOwnedAsync(request.Id, tenant.Value, cancellationToken)
                .ConfigureAwait(false);
            if (loaded.IsFailure)
                return Result<Response>.Fail(loaded.Errors);

            Result assigned = loaded.Value.AssignToList(
                request.ListId,
                clock.GetUtcNow(),
                actorContext.Actor());
            if (assigned.IsFailure)
                return Result<Response>.Fail(assigned.Errors);

            Result saved = await repository
                .SaveAsync(loaded.Value, cancellationToken)
                .ConfigureAwait(false);

            return saved.IsFailure
                ? Result<Response>.Fail(saved.Errors)
                : Result<Response>.Success(loaded.Value.ToModel<Response>());
        }
    }
    #endregion
}
