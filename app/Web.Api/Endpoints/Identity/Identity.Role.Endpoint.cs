using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Security.Authorization.Attributes;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.BuildingBlocks.Security.Identity.Roles.Commands;
using Application.BuildingBlocks.Security.Identity.Roles.Queries;

using Carter;

using Infrastructure.BuildingBlocks.Adapters.Serialization;

using MediatR;

namespace Web.Api.Endpoints.Identity;

/// <summary>Carter module for the admin-gated identity role surface.</summary>
public sealed class IdentityRoleEndpoint : ICarterModule
{
    private const string BasePath = "/api/identity/roles";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BasePath)
            .WithTags("Identity Roles")
            .RequireAuthorization();

        group.MapGet("/", SearchAsync)
            .WithName("SearchRoles")
            .WithSummary("List roles.")
            .HasPermission(PermissionContext.Identity.Roles.Read);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetRole")
            .WithSummary("Get one role.")
            .HasPermission(PermissionContext.Identity.Roles.Read);

        group.MapPost("/", CreateAsync)
            .WithName("CreateRole")
            .WithSummary("Create a role with an initial permission set.")
            .HasPermission(PermissionContext.Identity.Roles.Manage);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateRole")
            .WithSummary("Update a role.")
            .HasPermission(PermissionContext.Identity.Roles.Manage);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteRole")
            .WithSummary("Delete a role.")
            .HasPermission(PermissionContext.Identity.Roles.Manage);

        group.MapGet("/{id:guid}/permissions", GetPermissionsAsync)
            .WithName("ListIdentityRolePermissions")
            .WithSummary("List the permissions assigned to a role.")
            .HasPermission(PermissionContext.Identity.Roles.Read);

        group.MapPost("/{id:guid}/permissions", AssignPermissionAsync)
            .WithName("AssignPermissionToRole")
            .WithSummary("Assign a permission to a role.")
            .HasPermission(PermissionContext.Identity.Roles.Manage);

        group.MapDelete("/{id:guid}/permissions/{permission}", RemovePermissionAsync)
            .WithName("RemovePermissionFromRole")
            .WithSummary("Remove a permission from a role.")
            .HasPermission(PermissionContext.Identity.Roles.Manage);
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] QueryParameters parameters,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new SearchRoles.UseCase(parameters),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetRole.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> CreateAsync(
        CreateRole.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new CreateRole.UseCase(request), cancellationToken))
            .ToApiResult(role => $"{BasePath}/{role.Id}");

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateRole.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new UpdateRole.UseCase(id, request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> DeleteAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new DeleteRole.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetPermissionsAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetRolePermissions.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> AssignPermissionAsync(
        Guid id,
        AssignPermissionToRole.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new AssignPermissionToRole.UseCase(
                    new AssignPermissionToRole.Request
                    {
                        RoleId = id,
                        Permission = request.Permission,
                    }),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> RemovePermissionAsync(
        Guid id,
        string permission,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new RemovePermissionFromRole.UseCase(
                    new RemovePermissionFromRole.Request
                    {
                        RoleId = id,
                        Permission = permission,
                    }),
                cancellationToken))
            .ToApiResult();
}
