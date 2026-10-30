using Application.Common.Querying;
using Application.Common.Security.Authorization.Attributes;
using Application.Common.Security.Authorization.Registry;
using Application.Features.Identity.Users.Commands;
using Application.Features.Identity.Users.Queries;

using Carter;

using Infrastructure.Integrations.Serialization;

using MediatR;

namespace Web.Api.Endpoints.Identity;

/// <summary>Carter module for the admin-gated identity user surface.</summary>
public sealed class IdentityUserEndpoint : ICarterModule
{
    private const string BasePath = "/api/identity/users";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BasePath)
            .WithTags("Identity Users")
            .RequireAuthorization();

        group.MapGet("/", SearchAsync)
            .WithName("SearchUsers")
            .WithSummary("List user accounts.")
            .HasPermission(PermissionContext.Identity.Users.Read);

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetUser")
            .WithSummary("Get one user account.")
            .HasPermission(PermissionContext.Identity.Users.Read);

        group.MapPost("/", CreateAsync)
            .WithName("CreateUser")
            .WithSummary("Create a local user account.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateUser")
            .WithSummary("Update a user account.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteUser")
            .WithSummary("Delete a user account.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapPost("/{id:guid}/activate", ActivateAsync)
            .WithName("ActivateUser")
            .WithSummary("Activate a user account.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapPost("/{id:guid}/deactivate", DeactivateAsync)
            .WithName("DeactivateUser")
            .WithSummary("Deactivate a user account.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapPost("/{id:guid}/lock", LockAsync)
            .WithName("LockUser")
            .WithSummary("Lock a user account.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapPost("/{id:guid}/unlock", UnlockAsync)
            .WithName("UnlockUser")
            .WithSummary("Unlock a user account.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapPost("/{id:guid}/reset-password", ResetPasswordAsync)
            .WithName("ResetUserPassword")
            .WithSummary("Reset a user password without the current password.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapPost("/{id:guid}/confirm-email", ConfirmEmailAsync)
            .WithName("ConfirmUserEmail")
            .WithSummary("Mark a user email address as confirmed.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapPost("/{id:guid}/revoke-sessions", RevokeSessionsAsync)
            .WithName("RevokeUserSessions")
            .WithSummary("Revoke every active session for a user.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapGet("/{id:guid}/roles", GetRolesAsync)
            .WithName("GetUserRoles")
            .WithSummary("List the roles assigned to a user.")
            .HasPermission(PermissionContext.Identity.Users.Read);

        group.MapPost("/{id:guid}/roles/{roleId:guid}", AssignRoleAsync)
            .WithName("AssignRoleToUser")
            .WithSummary("Assign a role to a user.")
            .HasPermission(PermissionContext.Identity.Users.Manage);

        group.MapDelete("/{id:guid}/roles/{roleId:guid}", RemoveRoleAsync)
            .WithName("RemoveRoleFromUser")
            .WithSummary("Remove a role from a user.")
            .HasPermission(PermissionContext.Identity.Users.Manage);
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] QueryParameters parameters,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new SearchUsers.UseCase(parameters),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetUser.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> CreateAsync(
        CreateUser.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new CreateUser.UseCase(request), cancellationToken))
            .ToApiResult(user => $"{BasePath}/{user.Id}");

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateUser.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new UpdateUser.UseCase(id, request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> DeleteAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new DeleteUser.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> ActivateAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new ActivateUser.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> DeactivateAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new DeactivateUser.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> LockAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new LockUser.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> UnlockAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new UnlockUser.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> ResetPasswordAsync(
        Guid id,
        ResetUserPassword.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(new ResetUserPassword.UseCase(id, request), cancellationToken))
            .ToApiResult();

    private static async Task<IResult> ConfirmEmailAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new ConfirmUserEmail.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> RevokeSessionsAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new RevokeUserSessions.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> GetRolesAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new GetUserRoles.UseCase(id),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> AssignRoleAsync(
        Guid id,
        Guid roleId,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new AssignRoleToUser.UseCase(new AssignRoleToUser.Request { UserId = id, RoleId = roleId }),
                cancellationToken))
            .ToApiResult();

    private static async Task<IResult> RemoveRoleAsync(
        Guid id,
        Guid roleId,
        IMediator mediator,
        CancellationToken cancellationToken)
        => (await mediator.Send(
                new RemoveRoleFromUser.UseCase(new RemoveRoleFromUser.Request { UserId = id, RoleId = roleId }),
                cancellationToken))
            .ToApiResult();
}
