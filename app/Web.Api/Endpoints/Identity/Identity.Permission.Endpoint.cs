using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Security.Authorization;
using Application.BuildingBlocks.Security.Authorization.Attributes;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.BuildingBlocks.Security.Identity.Permissions.Commands;
using Application.BuildingBlocks.Security.Identity.Permissions.Queries;

using Carter;

using Infrastructure.BuildingBlocks.Adapters.Serialization;

using MediatR;

namespace Web.Api.Endpoints.Identity;

/// <summary>Carter module for the admin-gated identity permission catalog.</summary>
public sealed class IdentityPermissionEndpoint : ICarterModule
{
    private const string BasePath = "/api/identity/permissions";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BasePath)
            .WithTags("Identity Permissions")
            .RequireAuthorization();

        // Map: literal segments are declared before the identifier catch-all for clarity.
        group.MapGet("/", SearchAsync)
            .WithName("SearchPermissions")
            .WithSummary("List permission metadata, optionally filtered by text.")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/domains", ListDomainsAsync)
            .WithName("ListPermissionDomains")
            .WithSummary("List the distinct permission domains.")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListPermissionCategories")
            .WithSummary("List the distinct permission categories.")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/users/{userId:guid}", GetUserPermissionsAsync)
            .WithName("GetUserPermissions")
            .WithSummary("List the effective permissions held by a user.")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/users/{userId:guid}/check", CheckUserPermissionsAsync)
            .WithName("CheckUserPermissions")
            .WithSummary("Check whether a user holds the requested permissions.")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/roles/{roleId:guid}", GetRolePermissionsAsync)
            .WithName("ListPermissionRolePermissions")
            .WithSummary("List the permissions assigned to a role.")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/{identifier}", GetAsync)
            .WithName("GetPermission")
            .WithSummary("Get one permission by canonical identifier.")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshPermissions")
            .WithSummary("Rebuild the catalog from built-in metadata and persisted permission claims.")
            .HasPermission(PermissionContext.Identity.Permissions.Manage);
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] QueryParameters parameters,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new SearchPermissions.UseCase(parameters),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> ListDomainsAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new ListPermissionDomains.UseCase(),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> ListCategoriesAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new ListPermissionCategories.UseCase(),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetUserPermissionsAsync(
        Guid userId,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetUserPermissions.UseCase(userId),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> CheckUserPermissionsAsync(
        Guid userId,
        string[]? permissions,
        PermissionMatchMode? mode,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new CheckUserPermissions.UseCase(
                    userId,
                    new CheckUserPermissions.Request
                    {
                        Permissions = permissions ?? [],
                        Mode = mode ?? PermissionMatchMode.All,
                    }),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetRolePermissionsAsync(
        Guid roleId,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetRolePermissions.UseCase(roleId),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetAsync(
        string identifier,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetPermission.UseCase(identifier),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> RefreshAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new RefreshPermissionCatalog.UseCase(),
                cancellationToken))
            .ToApiResult();
}
