using Microsoft.AspNetCore.Builder;

using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.BuildingBlocks.Security.Authorization.Attributes;

/// <summary>Fluent authorization helpers for minimal-API endpoints.</summary>
public static class HasPermissionExtensions
{
    /// <summary>
    /// Requires the caller to hold every supplied permission.
    /// </summary>
    /// <param name="builder">The endpoint builder.</param>
    /// <param name="permissions">The required permission identifiers.</param>
    /// <returns>The original builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static RouteHandlerBuilder HasPermission(
        this RouteHandlerBuilder builder,
        params string[] permissions) =>
        builder.HasPermission(PermissionMatchMode.All, permissions);

    /// <summary>
    /// Requires the caller to hold permissions according to <paramref name="matchMode"/>.
    /// </summary>
    /// <param name="builder">The endpoint builder.</param>
    /// <param name="matchMode">Whether every permission or at least one is required.</param>
    /// <param name="permissions">The required permission identifiers.</param>
    /// <returns>The original builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static RouteHandlerBuilder HasPermission(
        this RouteHandlerBuilder builder,
        PermissionMatchMode matchMode,
        params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(new HasPermissionAttribute(matchMode, permissions));
    }

    /// <summary>
    /// Requires the caller to hold every supplied permission.
    /// </summary>
    /// <param name="builder">The endpoint builder.</param>
    /// <param name="permissions">The permission metadata value objects.</param>
    /// <returns>The original builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static RouteHandlerBuilder HasPermission(
        this RouteHandlerBuilder builder,
        params PermissionMetadata[] permissions) =>
        builder.HasPermission(PermissionMatchMode.All, permissions);

    /// <summary>
    /// Requires the caller to hold permissions according to <paramref name="matchMode"/>.
    /// </summary>
    /// <param name="builder">The endpoint builder.</param>
    /// <param name="matchMode">Whether every permission or at least one is required.</param>
    /// <param name="permissions">The permission metadata value objects.</param>
    /// <returns>The original builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">A permission value is <c>null</c>.</exception>
    public static RouteHandlerBuilder HasPermission(
        this RouteHandlerBuilder builder,
        PermissionMatchMode matchMode,
        params PermissionMetadata[] permissions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(permissions);

        string[] identifiers = [.. permissions.Select(permission =>
            permission?.Identifier
            ?? throw new ArgumentNullException(nameof(permissions), "A permission value is required."))];

        return builder.HasPermission(matchMode, identifiers);
    }
}
