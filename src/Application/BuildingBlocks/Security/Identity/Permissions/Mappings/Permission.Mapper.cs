using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.BuildingBlocks.Security.Identity.Permissions.Models;

namespace Application.BuildingBlocks.Security.Identity.Permissions.Mappings;

/// <summary>Maps permission metadata into the identity permission slice models.</summary>
public static class PermissionMapper
{
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
    /// <returns>One page of permission projections, or a paging failure.</returns>
    public static Result<QueryPage<PermissionItemModel>> ToPage(
        this PermissionSnapshotModel snapshot,
        QueryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(parameters);

        int page = parameters.Page ?? 1;
        int pageSize = parameters.PageSize ?? QueryConstant.Limits.Page.DefaultSize;

        if (page < 1)
            return Result<QueryPage<PermissionItemModel>>.Fail(QueryOutcome.Failure.PageInvalid);

        if (pageSize < 1 || pageSize > QueryConstant.Limits.Page.MaxSize)
        {
            return Result<QueryPage<PermissionItemModel>>.Fail(
                QueryOutcome.Failure.PageSizeInvalid(QueryConstant.Limits.Page.MaxSize));
        }

        int skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);

        IReadOnlyList<PermissionItemModel> items = [.. snapshot.Permissions
            .Skip(skip)
            .Take(pageSize)];

        return Result<QueryPage<PermissionItemModel>>.Ok(new QueryPage<PermissionItemModel>(
            items,
            page,
            pageSize,
            snapshot.Permissions.Count));
    }
}
