using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Security.Authorization.Attributes;
using Application.Common.Security.Authorization.Registry;
using Application.Features.Todos.Categories.Commands;
using Application.Features.Todos.Categories.Queries;

using Carter;

using MediatR;

using Web.Api.Endpoints.Results;

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
            .HasPermission(PermissionContext.Todo.Categories.Read);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetTodoCategory")
            .WithSummary("Get one todo category.")
            .HasPermission(PermissionContext.Todo.Categories.Read);

        group.MapPost("/", CreateAsync)
            .WithName("CreateTodoCategory")
            .WithSummary("Create a todo category.")
            .HasPermission(PermissionContext.Todo.Categories.Create);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateTodoCategory")
            .WithSummary("Update a todo category.")
            .HasPermission(PermissionContext.Todo.Categories.Update);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTodoCategory")
            .WithSummary("Delete a todo category, softly by default.")
            .HasPermission(PermissionContext.Todo.Categories.Delete);

        group.MapPost("/{id:guid}/restore", RestoreAsync)
            .WithName("RestoreTodoCategory")
            .WithSummary("Restore a soft-deleted todo category.")
            .HasPermission(PermissionContext.Todo.Categories.Update);
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
