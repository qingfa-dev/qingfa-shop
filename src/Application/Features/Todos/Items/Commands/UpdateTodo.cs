using Application.Common.EventSourcing;
using Application.Features.Todos.Common;
using Application.Features.Todos.Items.Mappings;
using Application.Features.Todos.Items.Models;
using Application.Features.Todos.Items.Validators;

using Core.Todos.Items;

using FluentValidation;

namespace Application.Features.Todos.Items.Commands;

public static class UpdateTodo
{
    #region Request
    public sealed record Request : TodoRequestModel
    {
        public Guid Id { get; set; }

        public bool ClearDescription { get; set; }

        public bool ClearDueAt { get; set; }

        public bool ClearList { get; set; }
    }
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

            RuleFor(command => command.Request)
                .ApplyTodoRules(isPatch: true);
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

            Todo todo = loaded.Value;
            string title = request.Title ?? todo.Title;
            string? description = request.ClearDescription
                ? null
                : request.Description ?? todo.Description;
            TodoPriority priority = request.Priority ?? todo.Priority;
            DateTimeOffset? dueAtUtc = request.ClearDueAt
                ? null
                : request.DueAtUtc ?? todo.DueAtUtc;
            Guid? listId = request.ListId ?? (request.ClearList ? null : todo.ListId);

            Result updated = todo.Update(
                title,
                description,
                priority,
                dueAtUtc,
                listId,
                clock.GetUtcNow(),
                actorContext.Actor());
            if (updated.IsFailure)
                return Result<Response>.Fail(updated.Errors);

            Result saved = await repository
                .SaveAsync(todo, cancellationToken)
                .ConfigureAwait(false);

            return saved.IsFailure
                ? Result<Response>.Fail(saved.Errors)
                : Result<Response>.Success(todo.ToModel<Response>());
        }
    }
    #endregion
}
