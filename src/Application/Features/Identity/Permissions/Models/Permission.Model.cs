namespace Application.Features.Identity.Permissions.Models;

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
