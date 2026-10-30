using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Security.Authorization.Attributes;
using Application.Common.Security.Authorization.Registry;
using Application.Features.Todos.Items.Commands;
using Application.Features.Todos.Items.Queries;

using Carter;

using Core.Common.Monad.Results;

using MediatR;

using Web.Api.Endpoints.Results;

namespace Web.Api.Endpoints.Todos;

/// <summary>Carter module for todo items and their transitions.</summary>
public sealed class TodoEndpoint : ICarterModule
{
    private const string BasePath = "/api/todos";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BasePath)
            .WithTags("Todos")
            .RequireAuthorization();

        group.MapGet("/", SearchAsync)
            .WithName("SearchTodos")
            .WithSummary("List todo items for the current tenant.")
            .HasPermission(PermissionContext.Todo.Items.Read);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetTodo")
            .WithSummary("Get one todo item.")
            .HasPermission(PermissionContext.Todo.Items.Read);

        group.MapPost("/", CreateAsync)
            .WithName("CreateTodo")
            .WithSummary("Create a todo item.")
            .HasPermission(PermissionContext.Todo.Items.Create);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateTodo")
            .WithSummary("Update a todo item.")
            .HasPermission(PermissionContext.Todo.Items.Update);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTodo")
            .WithSummary("Delete a todo item, softly by default.")
            .HasPermission(PermissionContext.Todo.Items.Delete);

        group.MapPost("/{id:guid}/start", StartAsync)
            .WithName("StartTodo")
            .WithSummary("Move a todo into progress.")
            .HasPermission(PermissionContext.Todo.Items.Update);

        group.MapPost("/{id:guid}/complete", CompleteAsync)
            .WithName("CompleteTodo")
            .WithSummary("Complete a todo.")
            .HasPermission(PermissionContext.Todo.Items.Update);

        group.MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("CancelTodo")
            .WithSummary("Cancel a todo.")
            .HasPermission(PermissionContext.Todo.Items.Update);

        group.MapPost("/{id:guid}/reopen", ReopenAsync)
            .WithName("ReopenTodo")
            .WithSummary("Reopen a finished todo.")
            .HasPermission(PermissionContext.Todo.Items.Update);

        group.MapPost("/{id:guid}/restore", RestoreAsync)
            .WithName("RestoreTodo")
            .WithSummary("Restore a soft-deleted todo.")
            .HasPermission(PermissionContext.Todo.Items.Update);

        group.MapPost("/{id:guid}/categories/{categoryId:guid}", AddCategoryAsync)
            .WithName("AddTodoCategory")
            .WithSummary("Attach a category to a todo.")
            .HasPermission(PermissionContext.Todo.Items.Update);

        group.MapDelete("/{id:guid}/categories/{categoryId:guid}", RemoveCategoryAsync)
            .WithName("RemoveTodoCategory")
            .WithSummary("Detach a category from a todo.")
            .HasPermission(PermissionContext.Todo.Items.Update);

        group.MapPut("/{id:guid}/list", AssignListAsync)
            .WithName("AssignTodoToList")
            .WithSummary("Move a todo into a list.")
            .HasPermission(PermissionContext.Todo.Items.Update);

        group.MapDelete("/{id:guid}/list", ClearListAsync)
            .WithName("ClearTodoList")
            .WithSummary("Remove a todo from its list.")
            .HasPermission(PermissionContext.Todo.Items.Update);
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] QueryParameters parameters,
        bool includeDeleted,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new SearchTodos.UseCase(new SearchTodos.Request(parameters, includeDeleted)),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetAsync(
        Guid id,
        bool includeDeleted,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetTodo.UseCase(new GetTodo.Request(id, includeDeleted)),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> CreateAsync(
        CreateTodo.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new CreateTodo.UseCase(request), cancellationToken))
            .ToApiResult(value => $"/api/todos/{value.Id}");

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateTodo.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        request.Id = id;
        return (await mediator.Send(new UpdateTodo.UseCase(request), cancellationToken))
            .ToApiResult();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        DeleteMode mode,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new DeleteTodo.UseCase(new DeleteTodo.Request(id, mode)),
                cancellationToken))
            .ToApiResult();

    private static Task<IResult> StartAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => SendAsync(mediator, new StartTodo.UseCase(new StartTodo.Request(id)), cancellationToken);

    private static Task<IResult> CompleteAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => SendAsync(mediator, new CompleteTodo.UseCase(new CompleteTodo.Request(id)), cancellationToken);

    private static Task<IResult> CancelAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => SendAsync(mediator, new CancelTodo.UseCase(new CancelTodo.Request(id)), cancellationToken);

    private static Task<IResult> ReopenAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => SendAsync(mediator, new ReopenTodo.UseCase(new ReopenTodo.Request(id)), cancellationToken);

    private static Task<IResult> RestoreAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => SendAsync(mediator, new RestoreTodo.UseCase(new RestoreTodo.Request(id)), cancellationToken);

    private static Task<IResult> AddCategoryAsync(
        Guid id,
        Guid categoryId,
        IMediator mediator,
        CancellationToken cancellationToken)
        => SendAsync(
            mediator,
            new AddTodoCategory.UseCase(new AddTodoCategory.Request(id, categoryId)),
            cancellationToken);

    private static Task<IResult> RemoveCategoryAsync(
        Guid id,
        Guid categoryId,
        IMediator mediator,
        CancellationToken cancellationToken)
        => SendAsync(
            mediator,
            new RemoveTodoCategory.UseCase(new RemoveTodoCategory.Request(id, categoryId)),
            cancellationToken);

    private static Task<IResult> AssignListAsync(
        Guid id,
        AssignListRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => SendAsync(
            mediator,
            new AssignTodoToList.UseCase(new AssignTodoToList.Request(id, request.ListId)),
            cancellationToken);

    private static Task<IResult> ClearListAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => SendAsync(mediator, new ClearTodoList.UseCase(new ClearTodoList.Request(id)), cancellationToken);

    private static async Task<IResult> SendAsync<T>(
        IMediator mediator,
        IRequest<Result<T>> request,
        CancellationToken cancellationToken)
        => (await mediator.Send(request, cancellationToken)).ToApiResult();

    /// <summary>List assignment body.</summary>
    public sealed record AssignListRequest(Guid ListId);
}
