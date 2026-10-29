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
                    (tenantId, ct) => repository.FindOwnedAsync(request.Id, tenantId, ct),
                    cancellationToken)
                .BindAsync(todo => ApplyChanges(todo, request))
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

        private static Result<Todo> ApplyChanges(Todo todo, Request request)
        {
            Result<Todo> updated = Result<Todo>.Ok(todo);

            if (request.Title is not null)
            {
                updated = updated.SetTitle(request.Title);
            }

            if (request.ClearDescription)
            {
                updated = updated.SetDescription(null);
            }
            else if (request.Description is not null)
            {
                updated = updated.SetDescription(request.Description);
            }

            if (request.Priority is { } priority)
            {
                updated = updated.SetPriority(priority);
            }

            if (request.ClearDueAt)
            {
                updated = updated.ClearDueDate();
            }
            else if (request.DueAtUtc is { } dueAtUtc)
            {
                updated = updated.SetDueDate(dueAtUtc);
            }

            if (request.ListId is { } listId)
            {
                updated = updated.AssignToList(listId);
            }
            else if (request.ClearList)
            {
                updated = updated.ClearList();
            }

            return updated;
        }
    }
    #endregion
}
