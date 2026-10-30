using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Common.Security.Authorization.Domain;
using Application.Common.Security.Authorization.Registry;
using Application.Features.Identity.Permissions.Models;

namespace Application.Features.Identity.Permissions.Mappings;

/// <summary>Maps permission metadata into the identity permission slice models.</summary>
public static class PermissionMapper
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 200;

    /// <summary>Maps one catalog entry into its application projection.</summary>
    /// <param name="permission">The catalog metadata.</param>
    /// <returns>The permission projection.</returns>
    public static PermissionItemModel ToModel(this PermissionMetadata permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        return new PermissionItemModel(
            permission.Identifier,
            permission.Domain,
            permission.Category,
            permission.Resource,
            permission.Action,
            permission.Description);
    }

    /// <summary>Maps a metadata snapshot into the application snapshot model.</summary>
    /// <param name="snapshot">The catalog snapshot.</param>
    /// <returns>The permission snapshot projection.</returns>
    public static PermissionSnapshotModel ToModel(this PermissionMetadataSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new PermissionSnapshotModel(
            [.. snapshot.Permissions.Select(ToModel)],
            snapshot.RefreshedAtUtc);
    }

    /// <summary>Pages a filtered snapshot using the shared query parameters.</summary>
    /// <param name="snapshot">The filtered catalog snapshot.</param>
    /// <param name="parameters">The paging inputs.</param>
    /// <returns>One page of permission projections.</returns>
    public static QueryPage<PermissionItemModel> ToPage(
        this PermissionSnapshotModel snapshot,
        QueryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(parameters);

        int page = parameters.Page is > 0 ? parameters.Page.Value : 1;
        int pageSize = parameters.PageSize is > 0
            ? Math.Min(parameters.PageSize.Value, MaxPageSize)
            : DefaultPageSize;

        // Validate: clamp the skip so a very large page number cannot overflow Int32.
        int skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);

        IReadOnlyList<PermissionItemModel> items = [.. snapshot.Permissions
            .Skip(skip)
            .Take(pageSize)];

        return new QueryPage<PermissionItemModel>(
            items,
            page,
            pageSize,
            snapshot.Permissions.Count);
    }
}
