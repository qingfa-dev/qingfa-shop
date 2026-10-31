using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.BuildingBlocks.Security.Authorization.Registry;

/// <summary>A module's contribution to the permission catalog.</summary>
public interface IPermissionCatalogContributor
{
    /// <summary>Gets the module permissions.</summary>
    IReadOnlyList<PermissionMetadata> Permissions { get; }

    /// <summary>Gets the module's administrator defaults.</summary>
    IReadOnlyList<PermissionMetadata> Admin { get; }

    /// <summary>Gets the module's manager defaults.</summary>
    IReadOnlyList<PermissionMetadata> Manager { get; }

    /// <summary>Gets the module's standard user defaults.</summary>
    IReadOnlyList<PermissionMetadata> User { get; }
}
