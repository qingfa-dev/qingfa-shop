using Application.BuildingBlocks.Persistence.Repositories;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Security.Authorization.Attributes;
using Application.Modules.Todos.Lists.Commands;
using Application.Modules.Todos.Lists.Queries;
using Application.Modules.Todos.Permissions;

using Carter;

using MediatR;

using Infrastructure.BuildingBlocks.Adapters.Serialization;

namespace Web.Api.Endpoints.Todos;

/// <summary>Carter module for todo lists.</summary>
public sealed class TodoListEndpoint : ICarterModule
{
    private const string BasePath = "/api/todo-lists";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BasePath)
            .WithTags("TodoLists")
            .RequireAuthorization();

        group.MapGet("/", SearchAsync)
            .WithName("SearchTodoLists")
            .WithSummary("List todo lists for the current tenant.")
            .HasPermission(TodoPermissionCatalog.Lists.Read);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetTodoList")
            .WithSummary("Get one todo list.")
            .HasPermission(TodoPermissionCatalog.Lists.Read);

        group.MapPost("/", CreateAsync)
            .WithName("CreateTodoList")
            .WithSummary("Create a todo list.")
            .HasPermission(TodoPermissionCatalog.Lists.Create);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateTodoList")
            .WithSummary("Update a todo list.")
            .HasPermission(TodoPermissionCatalog.Lists.Update);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTodoList")
            .WithSummary("Delete a todo list, softly by default.")
            .HasPermission(TodoPermissionCatalog.Lists.Delete);

        group.MapPost("/{id:guid}/restore", RestoreAsync)
            .WithName("RestoreTodoList")
            .WithSummary("Restore a soft-deleted todo list.")
            .HasPermission(TodoPermissionCatalog.Lists.Update);
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] QueryParameters parameters,
        bool includeDeleted,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new SearchTodoLists.UseCase(new SearchTodoLists.Request(parameters, includeDeleted)),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetAsync(
        Guid id,
        bool includeDeleted,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetTodoList.UseCase(new GetTodoList.Request(id, includeDeleted)),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> CreateAsync(
        CreateTodoList.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new CreateTodoList.UseCase(request), cancellationToken))
            .ToApiResult(value => $"/api/todo-lists/{value.Id}");

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateTodoList.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        request.Id = id;
        return (await mediator.Send(new UpdateTodoList.UseCase(request), cancellationToken))
            .ToApiResult();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        DeleteMode mode,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new DeleteTodoList.UseCase(new DeleteTodoList.Request(id, mode)),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> RestoreAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new RestoreTodoList.UseCase(new RestoreTodoList.Request(id)),
                cancellationToken))
            .ToApiResult();
}
