using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Todos.Common;
using Application.Modules.Todos.Items.Mappings;
using Application.Modules.Todos.Items.Models;
using Application.Modules.Todos.Items.Validators;

using Core.Modules.Todos.Items;

using FluentValidation;

namespace Application.Modules.Todos.Items.Commands;

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
            DateTimeOffset nowUtc = clock.GetUtcNow();
            string? actor = actorContext.Actor();

            return await tenantContext.RequireTenant()
                .BindAsync(
                    (tenantId, ct) => repository.LoadOwnedAsync(request.Id, tenantId, ct),
                    cancellationToken)
                .BindAsync(
                    (todo, _) => Task.FromResult(Apply(todo, request, nowUtc, actor)),
                    cancellationToken)
                .ThenAsync(
                    (todo, ct) => repository.SaveAsync(todo, ct),
                    cancellationToken)
                .MapAsync(todo => todo.ToModel<Response>());
        }

        private static Result<Todo> Apply(
            Todo todo,
            Request request,
            DateTimeOffset nowUtc,
            string? actor)
        {
            string title = request.Title ?? todo.Title;
            string? description = request.ClearDescription
                ? null
                : request.Description ?? todo.Description;
            TodoPriority priority = request.Priority ?? todo.Priority;
            DateTimeOffset? dueAtUtc = request.ClearDueAt
                ? null
                : request.DueAtUtc ?? todo.DueAtUtc;
            Guid? listId = request.ListId
                ?? (request.ClearList ? null : todo.ListId);

            return todo.Update(title, description, priority, dueAtUtc, listId, nowUtc, actor);
        }
    }
    #endregion
}
