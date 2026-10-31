using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Policies;

namespace Application.BuildingBlocks.Security.Authorization.Attributes;

/// <summary>
/// Declarative permission gate. Decorate endpoints or controllers with
/// <c>[HasPermission(PermissionContext.Identity.Users.Manage.Identifier)]</c>.
/// </summary>
/// <remarks>
/// Multiple identifiers require every permission by default:
/// <c>[HasPermission(permissionA, permissionB)]</c>. Use
/// <c>[HasPermission(PermissionMatchMode.Any, permissionA, permissionB)]</c> when any
/// one permission is sufficient.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class HasPermissionAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute
{
    /// <summary>Creates a requirement that the caller holds every supplied permission.</summary>
    /// <param name="permissions">The required permission identifiers.</param>
    public HasPermissionAttribute(params string[] permissions)
        : this(PermissionMatchMode.All, permissions)
    {
    }

    /// <summary>Creates a requirement with an explicit match mode.</summary>
    /// <param name="matchMode">Whether every permission or at least one is required.</param>
    /// <param name="permissions">The required permission identifiers.</param>
    /// <exception cref="ArgumentException">No permission identifier was supplied.</exception>
    public HasPermissionAttribute(
        PermissionMatchMode matchMode,
        params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        Permissions = [.. permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(PermissionMetadataConstant.NormalizeIdentifier)
            .Distinct(StringComparer.Ordinal)];

        if (Permissions.Count == 0)
            throw new ArgumentException("At least one permission identifier is required.", nameof(permissions));

        MatchMode = matchMode;
        Policy = PermissionPolicyParser.Compose(Permissions, matchMode);
    }

    /// <summary>Gets the required permission identifiers.</summary>
    public IReadOnlyList<string> Permissions { get; }

    /// <summary>Gets how <see cref="Permissions"/> combine.</summary>
    public PermissionMatchMode MatchMode { get; }

    /// <summary>Gets the first required permission, for diagnostics and single-permission call sites.</summary>
    public string Permission => Permissions[0];
}
