using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Security.Authorization;
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Permissions.Services;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Permissions.Mappings;
using Application.BuildingBlocks.Security.Identity.Permissions.Models;
using Application.BuildingBlocks.Security.Identity.Permissions.Outcome;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Authorization.Permissions.Services;

/// <summary>Admin-facing catalog built on the authorization metadata registry and permission service.</summary>
/// <remarks>
/// The metadata registry owns the built-in and runtime permission catalog; the permission
/// service owns effective user and role permission resolution. This adapter projects both
/// into the identity application models without leaking authorization internals.
/// </remarks>
public sealed class PermissionCatalogService(
    IPermissionMetadataRegistry registry,
    IPermissionService permissionService) : IPermissionCatalogService
{
    /// <inheritdoc />
    public async Task<Result<PermissionSnapshotModel>> SearchAsync(
        QueryParameters parameters,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        // Validate: the catalog is in-memory, so only search and paging inputs are meaningful.
        Result supported = ValidateSearchParameters(parameters);
        if (supported.IsFailure)
            return Result<PermissionSnapshotModel>.Fail(supported.Errors, supported.StatusCode);

        Result<PermissionMetadataSnapshot> refreshed = await registry
            .RefreshAsync(ct)
            .ConfigureAwait(false);

        if (refreshed.IsFailure)
            return Result<PermissionSnapshotModel>.Fail(refreshed.Errors, refreshed.StatusCode);

        IEnumerable<PermissionMetadata> permissions = refreshed.Value.Permissions;

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            string search = parameters.Search.Trim();
            permissions = permissions.Where(permission => Matches(permission, search));
        }

        return Result<PermissionSnapshotModel>.Ok(new PermissionSnapshotModel(
            [.. permissions.Select(permission => permission.ToModel())],
            refreshed.Value.RefreshedAtUtc));
    }

    /// <inheritdoc />
    public async Task<Result<PermissionItemModel>> GetAsync(
        string identifier,
        CancellationToken ct = default)
    {
        Result<PermissionMetadataSnapshot> refreshed = await registry
            .RefreshAsync(ct)
            .ConfigureAwait(false);

        if (refreshed.IsFailure)
            return Result<PermissionItemModel>.Fail(refreshed.Errors, refreshed.StatusCode);

        PermissionMetadata? permission = refreshed.Value.Find(identifier);

        return permission is null
            ? Result<PermissionItemModel>.Fail(PermissionCatalogOutcome.Failure.NotFound(identifier))
            : Result<PermissionItemModel>.Ok(permission.ToModel());
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> ListDomainsAsync(CancellationToken ct = default)
    {
        Result<PermissionMetadataSnapshot> refreshed = await registry
            .RefreshAsync(ct)
            .ConfigureAwait(false);

        return refreshed.IsFailure
            ? Result<IReadOnlyList<string>>.Fail(refreshed.Errors, refreshed.StatusCode)
            : Result<IReadOnlyList<string>>.Ok([.. refreshed.Value.Permissions
                .Select(permission => permission.Domain)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)]);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> ListCategoriesAsync(CancellationToken ct = default)
    {
        Result<PermissionMetadataSnapshot> refreshed = await registry
            .RefreshAsync(ct)
            .ConfigureAwait(false);

        return refreshed.IsFailure
            ? Result<IReadOnlyList<string>>.Fail(refreshed.Errors, refreshed.StatusCode)
            : Result<IReadOnlyList<string>>.Ok([.. refreshed.Value.Permissions
                .Select(permission => permission.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)]);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        Result<HashSet<string>> permissions = await permissionService
            .GetEffectiveUserPermissionsAsync(userId, ct)
            .ConfigureAwait(false);

        return permissions.Map(values => (IReadOnlyList<string>)[
            .. values.Order(StringComparer.OrdinalIgnoreCase)]);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> GetRolePermissionsAsync(
        Guid roleId,
        CancellationToken ct = default)
    {
        Result<HashSet<string>> permissions = await permissionService
            .GetRolePermissionsAsync(roleId, ct)
            .ConfigureAwait(false);

        return permissions.Map(values => (IReadOnlyList<string>)[
            .. values.Order(StringComparer.OrdinalIgnoreCase)]);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> CheckUserPermissionsAsync(
        Guid userId,
        IReadOnlyList<string> permissions,
        PermissionMatchMode mode,
        CancellationToken ct = default)
    {
        // Validate: an empty requirement would make ALL checks vacuously true.
        if (permissions is null || permissions.Count == 0)
            return Result<bool>.Fail(PermissionCatalogOutcome.Failure.PermissionsRequired);

        return mode == PermissionMatchMode.Any
            ? await permissionService
                .HasAnyPermissionsAsync(userId, permissions, ct)
                .ConfigureAwait(false)
            : await permissionService
                .HasAllPermissionsAsync(userId, permissions, ct)
                .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<PermissionSnapshotModel>> RefreshAsync(CancellationToken ct = default)
    {
        Result<PermissionMetadataSnapshot> refreshed = await registry
            .RefreshAsync(ct)
            .ConfigureAwait(false);

        return refreshed.IsFailure
            ? Result<PermissionSnapshotModel>.Fail(refreshed.Errors, refreshed.StatusCode)
            : Result<PermissionSnapshotModel>.Ok(refreshed.Value.ToModel());
    }

    private static bool Matches(PermissionMetadata permission, string search) =>
        permission.Identifier.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        permission.Domain.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        permission.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        permission.Resource.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        permission.Action.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        permission.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        permission.Description.Contains(search, StringComparison.OrdinalIgnoreCase);

    private static Result ValidateSearchParameters(QueryParameters parameters)
    {
        if (parameters.Filter is not null)
            return UnsupportedSearch(nameof(parameters.Filter));

        if (parameters.OrderBy is not null)
            return UnsupportedSearch(nameof(parameters.OrderBy));

        if (parameters.SearchFields is not null)
            return UnsupportedSearch(nameof(parameters.SearchFields));

        if (parameters.SearchMode is not null)
            return UnsupportedSearch(nameof(parameters.SearchMode));

        if (parameters.SearchTermLogic is not null)
            return UnsupportedSearch(nameof(parameters.SearchTermLogic));

        if (parameters.SearchCaseInsensitive is not null)
            return UnsupportedSearch(nameof(parameters.SearchCaseInsensitive));

        if (parameters.Select is not null)
            return UnsupportedSearch(nameof(parameters.Select));

        if (parameters.GroupBy is not null)
            return UnsupportedSearch(nameof(parameters.GroupBy));

        if (parameters.Aggregate is not null)
            return UnsupportedSearch(nameof(parameters.Aggregate));

        return Result.Ok();
    }

    private static Result UnsupportedSearch(string parameter) => Result.Fail(
        PermissionCatalogOutcome.Failure.UnsupportedSearch(parameter));
}
