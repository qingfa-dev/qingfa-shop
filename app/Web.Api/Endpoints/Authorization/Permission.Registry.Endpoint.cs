using Carter;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Microsoft.AspNetCore.Http.HttpResults;

using Application.Common.Security.Authorization;
using Application.Common.Security.Authorization.Attributes;
using Application.Common.Security.Authorization.Domain;
using Application.Common.Security.Authorization.Permissions.Services;
using Application.Common.Security.Authorization.Registry;

using Infrastructure.Security.Identity.Domain.Roles;

namespace Web.Api.Endpoints.Authorization;

/// <summary>Carter module that exposes the permission catalog and permission checks.</summary>
public sealed class PermissionRegistryEndpoint : ICarterModule
{
    private const string BasePath = "/api/authorization";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BasePath)
            .WithTags("Authorization");

        group.MapGet("/permissions", ListPermissionsAsync)
            .WithName("ListPermissions")
            .WithSummary("List permission metadata, optionally filtered by domain, category, or text.")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/permissions/{identifier}", GetPermissionAsync)
            .WithName("GetPermission")
            .WithSummary("Get one permission by canonical identifier.")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/domains", ListDomainsAsync)
            .WithName("ListPermissionDomains")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListPermissionCategories")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/roles", ListRolePermissionsAsync)
            .WithName("ListRolePermissions")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapPost("/permissions/refresh", RefreshAsync)
            .WithName("RefreshPermissions")
            .WithSummary("Rebuild the catalog from built-in metadata and persisted permission claims.")
            .HasPermission(PermissionContext.Identity.Permissions.Manage);

        group.MapGet("/users/{userId:guid}/permissions", GetUserPermissionsAsync)
            .WithName("GetUserPermissions")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/users/{userId:guid}/permissions/check", CheckUserPermissionsAsync)
            .WithName("CheckUserPermissions")
            .HasPermission(PermissionContext.Identity.Permissions.Read);

        group.MapGet("/roles/{roleId:guid}/permissions", GetRolePermissionsAsync)
            .WithName("GetRolePermissions")
            .HasPermission(PermissionContext.Identity.Permissions.Read);
    }

    internal static async Task<IResult> ListPermissionsAsync(
        IPermissionMetadataRegistry registry,
        string? domain,
        string? category,
        string? search,
        CancellationToken cancellationToken)
    {
        Result<PermissionMetadataSnapshot> refreshed = await registry.RefreshAsync(cancellationToken);
        if (refreshed.IsFailure)
            return Problem(refreshed.Errors);

        IEnumerable<PermissionMetadata> permissions = refreshed.Value.Permissions;

        if (!string.IsNullOrWhiteSpace(domain))
            permissions = permissions.Where(permission =>
                string.Equals(permission.Domain, domain, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(category))
            permissions = permissions.Where(permission =>
                string.Equals(permission.Category, category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
            permissions = permissions.Where(permission =>
                permission.Identifier.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                permission.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                permission.Description.Contains(search, StringComparison.OrdinalIgnoreCase));

        PermissionMetadata[] result = [.. permissions];
        return Results.Ok(new PermissionListResponse(
            result,
            result.Length,
            refreshed.Value.RefreshedAtUtc));
    }

    internal static async Task<IResult> GetPermissionAsync(
        string identifier,
        IPermissionMetadataRegistry registry,
        CancellationToken cancellationToken)
    {
        Result<PermissionMetadataSnapshot> refreshed = await registry.RefreshAsync(cancellationToken);
        if (refreshed.IsFailure)
            return Problem(refreshed.Errors);

        PermissionMetadata? permission = refreshed.Value.Find(identifier);
        return permission is null
            ? Results.NotFound(new { identifier, message = "Permission metadata was not found." })
            : Results.Ok(permission);
    }

    internal static async Task<IResult> ListDomainsAsync(
        IPermissionMetadataRegistry registry,
        CancellationToken cancellationToken)
    {
        Result<PermissionMetadataSnapshot> refreshed = await registry.RefreshAsync(cancellationToken);
        if (refreshed.IsFailure)
            return Problem(refreshed.Errors);

        DomainCount[] domains = [.. refreshed.Value.Permissions
            .GroupBy(permission => permission.Domain, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DomainCount(group.Key, group.Count()))];

        return Results.Ok(domains);
    }

    internal static async Task<IResult> ListCategoriesAsync(
        IPermissionMetadataRegistry registry,
        CancellationToken cancellationToken)
    {
        Result<PermissionMetadataSnapshot> refreshed = await registry.RefreshAsync(cancellationToken);
        if (refreshed.IsFailure)
            return Problem(refreshed.Errors);

        CategoryCount[] categories = [.. refreshed.Value.Permissions
            .GroupBy(permission => permission.Category, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CategoryCount(group.Key, group.Count()))];

        return Results.Ok(categories);
    }

    internal static IResult ListRolePermissionsAsync() =>
        Results.Ok(new[]
        {
            new RolePermissionSet(
                RoleConstant.Defaults.Admin,
                [.. PermissionContext.Admin.Select(permission => permission.Identifier)]),
            new RolePermissionSet(
                RoleConstant.Defaults.Manager,
                [.. PermissionContext.Manager.Select(permission => permission.Identifier)]),
            new RolePermissionSet(
                RoleConstant.Defaults.User,
                [.. PermissionContext.User.Select(permission => permission.Identifier)]),
        });

    internal static async Task<IResult> RefreshAsync(
        IPermissionMetadataRegistry registry,
        CancellationToken cancellationToken)
    {
        Result<PermissionMetadataSnapshot> refreshed = await registry.RefreshAsync(cancellationToken);
        if (refreshed.IsFailure)
            return Problem(refreshed.Errors);

        return Results.Ok(new PermissionRefreshResponse(
            refreshed.Value.Count,
            refreshed.Value.RefreshedAtUtc));
    }

    internal static async Task<IResult> GetUserPermissionsAsync(
        Guid userId,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        Result<HashSet<string>> permissions = await permissionService
            .GetEffectiveUserPermissionsAsync(userId, cancellationToken);

        return permissions.IsFailure
            ? Problem(permissions.Errors)
            : Results.Ok(new UserPermissionResponse(
                userId,
                [.. permissions.Value.Order(StringComparer.OrdinalIgnoreCase)]));
    }

    internal static async Task<IResult> CheckUserPermissionsAsync(
        Guid userId,
        string[] permissions,
        string? mode,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        if (permissions.Length == 0)
            return Results.Problem(
                title: "A permission check was requested without permissions.",
                statusCode: StatusCodes.Status400BadRequest);

        PermissionMatchMode matchMode = PermissionMatchMode.All;
        if (!string.IsNullOrWhiteSpace(mode) &&
            !Enum.TryParse(mode, ignoreCase: true, out matchMode))
        {
            return Results.Problem(
                title: $"Unknown permission match mode '{mode}'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        Result<bool> granted = matchMode == PermissionMatchMode.Any
            ? await permissionService.HasAnyPermissionsAsync(userId, permissions, cancellationToken)
            : await permissionService.HasAllPermissionsAsync(userId, permissions, cancellationToken);

        return granted.IsFailure
            ? Problem(granted.Errors)
            : Results.Ok(new PermissionCheckResponse(
                userId,
                matchMode,
                permissions,
                granted.Value));
    }

    internal static async Task<IResult> GetRolePermissionsAsync(
        Guid roleId,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        Result<HashSet<string>> permissions = await permissionService
            .GetRolePermissionsAsync(roleId, cancellationToken);

        return permissions.IsFailure
            ? Problem(permissions.Errors)
            : Results.Ok(new RolePermissionResponse(
                roleId,
                [.. permissions.Value.Order(StringComparer.OrdinalIgnoreCase)]));
    }

    private static ProblemHttpResult Problem(IEnumerable<Error> errors)
    {
        Error[] details = [.. errors];
        int status = details.Select(error => error.Status).FirstOrDefault(status => status is not null) ?? 500;

        return TypedResults.Problem(
            title: "Authorization request failed",
            detail: string.Join(
                "; ",
                details.Select(error => $"{error.Code}: {error.Message}")),
            statusCode: status);
    }

    private sealed record PermissionListResponse(
        IReadOnlyList<PermissionMetadata> Permissions,
        int Count,
        DateTimeOffset RefreshedAtUtc);

    private sealed record PermissionRefreshResponse(int Count, DateTimeOffset RefreshedAtUtc);

    private sealed record RolePermissionSet(string Role, IReadOnlyList<string> Permissions);

    private sealed record UserPermissionResponse(Guid UserId, IReadOnlyList<string> Permissions);

    private sealed record RolePermissionResponse(Guid RoleId, IReadOnlyList<string> Permissions);

    private sealed record PermissionCheckResponse(
        Guid UserId,
        PermissionMatchMode Mode,
        IReadOnlyList<string> Permissions,
        bool Granted);

    private sealed record DomainCount(string Domain, int Count);

    private sealed record CategoryCount(string Category, int Count);
}
