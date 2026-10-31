using Microsoft.AspNetCore.Authorization;

using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.BuildingBlocks.Security.Authorization.Requirements;

/// <summary>
/// Requires the current user to hold one or more permissions, either through a role or directly.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>Creates a requirement for a single permission.</summary>
    /// <param name="permission">The required permission identifier.</param>
    public PermissionRequirement(string permission)
        : this([permission], PermissionMatchMode.All)
    {
    }

    /// <summary>Creates a requirement for multiple permissions.</summary>
    /// <param name="permissions">The required permission identifiers.</param>
    /// <param name="matchMode">Whether every permission or at least one is required.</param>
    /// <exception cref="ArgumentNullException"><paramref name="permissions"/> is <c>null</c>.</exception>
    public PermissionRequirement(
        IEnumerable<string> permissions,
        PermissionMatchMode matchMode = PermissionMatchMode.All)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        Permissions = [.. permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(PermissionMetadataConstant.NormalizeIdentifier)
            .Distinct(StringComparer.Ordinal)];

        MatchMode = matchMode;
    }

    /// <summary>Gets the required permission identifiers.</summary>
    public IReadOnlyList<string> Permissions { get; }

    /// <summary>Gets how <see cref="Permissions"/> combine.</summary>
    public PermissionMatchMode MatchMode { get; }

    /// <summary>Gets the first required permission, for diagnostics and single-permission call sites.</summary>
    public string Permission => Permissions.Count > 0 ? Permissions[0] : string.Empty;
}
