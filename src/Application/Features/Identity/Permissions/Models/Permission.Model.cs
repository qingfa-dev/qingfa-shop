using Application.Common.Security.Authorization;

namespace Application.Features.Identity.Permissions.Models;

#region Parameters

/// <summary>Fields that describe a permission check.</summary>
public abstract record PermissionCheckParameter
{
    /// <summary>Gets the permission identifiers to check.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    /// <summary>Gets whether every or any permission must be held.</summary>
    public PermissionMatchMode Mode { get; init; } = PermissionMatchMode.All;
}

#endregion

#region Request

/// <summary>A request asking whether a user holds the supplied permissions.</summary>
public record CheckUserPermissionsRequestModel : PermissionCheckParameter;

#endregion

#region Response

/// <summary>One permission advertised by the catalog.</summary>
/// <param name="Identifier">The canonical permission identifier.</param>
/// <param name="Domain">The owning functional domain.</param>
/// <param name="Category">The category within the domain.</param>
/// <param name="Resource">The resource the permission addresses.</param>
/// <param name="Action">The action the permission allows.</param>
/// <param name="Description">The human-readable description.</param>
public sealed record PermissionItemModel(
    string Identifier,
    string Domain,
    string Category,
    string Resource,
    string Action,
    string Description);

/// <summary>A point-in-time view of the permission catalog.</summary>
/// <param name="Permissions">The permissions in the catalog.</param>
/// <param name="RefreshedOnUtc">The UTC moment the snapshot was built.</param>
public sealed record PermissionSnapshotModel(
    IReadOnlyList<PermissionItemModel> Permissions,
    DateTimeOffset RefreshedOnUtc);

/// <summary>The outcome of a permission check for one user.</summary>
public sealed record PermissionCheckModel : PermissionCheckParameter
{
    /// <summary>Gets the user whose permissions were checked.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets a value indicating whether the requirement was met.</summary>
    public bool Granted { get; init; }
}

#endregion
