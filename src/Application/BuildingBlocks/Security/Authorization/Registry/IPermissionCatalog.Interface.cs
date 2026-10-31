using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.BuildingBlocks.Security.Authorization.Registry;

/// <summary>Complete permission catalog and default role assignments.</summary>
public interface IPermissionCatalog
{
    /// <summary>Gets every known permission.</summary>
    IReadOnlyList<PermissionMetadata> All { get; }

    /// <summary>Gets the administrator defaults.</summary>
    IReadOnlyList<PermissionMetadata> Admin { get; }

    /// <summary>Gets the manager defaults.</summary>
    IReadOnlyList<PermissionMetadata> Manager { get; }

    /// <summary>Gets the standard user defaults.</summary>
    IReadOnlyList<PermissionMetadata> User { get; }
}
