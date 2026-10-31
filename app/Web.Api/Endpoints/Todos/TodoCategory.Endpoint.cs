using Application.BuildingBlocks.Persistence.Repositories;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Security.Authorization.Attributes;
using Application.Modules.Todos.Categories.Commands;
using Application.Modules.Todos.Categories.Queries;
using Application.Modules.Todos.Permissions;

using Carter;

using MediatR;

using Infrastructure.BuildingBlocks.Adapters.Serialization;

namespace Web.Api.Endpoints.Todos;

/// <summary>Carter module for todo categories.</summary>
public sealed class TodoCategoryEndpoint : ICarterModule
{
    private const string BasePath = "/api/todo-categories";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BasePath)
            .WithTags("TodoCategories")
            .RequireAuthorization();

        group.MapGet("/", SearchAsync)
            .WithName("SearchTodoCategories")
            .WithSummary("List todo categories for the current tenant.")
            .HasPermission(TodoPermissionCatalog.Categories.Read);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetTodoCategory")
            .WithSummary("Get one todo category.")
            .HasPermission(TodoPermissionCatalog.Categories.Read);

        group.MapPost("/", CreateAsync)
            .WithName("CreateTodoCategory")
            .WithSummary("Create a todo category.")
            .HasPermission(TodoPermissionCatalog.Categories.Create);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateTodoCategory")
            .WithSummary("Update a todo category.")
            .HasPermission(TodoPermissionCatalog.Categories.Update);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTodoCategory")
            .WithSummary("Delete a todo category, softly by default.")
            .HasPermission(TodoPermissionCatalog.Categories.Delete);

        group.MapPost("/{id:guid}/restore", RestoreAsync)
            .WithName("RestoreTodoCategory")
            .WithSummary("Restore a soft-deleted todo category.")
            .HasPermission(TodoPermissionCatalog.Categories.Update);
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] QueryParameters parameters,
        bool includeDeleted,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new SearchTodoCategories.UseCase(
                    new SearchTodoCategories.Request(parameters, includeDeleted)),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetAsync(
        Guid id,
        bool includeDeleted,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetTodoCategory.UseCase(new GetTodoCategory.Request(id, includeDeleted)),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> CreateAsync(
        CreateTodoCategory.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new CreateTodoCategory.UseCase(request), cancellationToken))
            .ToApiResult(value => $"/api/todo-categories/{value.Id}");

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateTodoCategory.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        request.Id = id;
        return (await mediator.Send(new UpdateTodoCategory.UseCase(request), cancellationToken))
            .ToApiResult();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        DeleteMode mode,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new DeleteTodoCategory.UseCase(new DeleteTodoCategory.Request(id, mode)),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> RestoreAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new RestoreTodoCategory.UseCase(new RestoreTodoCategory.Request(id)),
                cancellationToken))
            .ToApiResult();
}
