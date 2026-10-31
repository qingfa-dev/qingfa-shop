using Application.BuildingBlocks.Security.Authorization.Domain;

namespace Application.BuildingBlocks.Security.Authorization.Registry;

/// <summary>Platform permission catalog backed by the Identity baseline.</summary>
public sealed class PlatformPermissionCatalog : IPermissionCatalog, IPermissionCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> All => PermissionContext.All;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Permissions => PermissionContext.All;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Admin => PermissionContext.Admin;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Manager => PermissionContext.Manager;

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> User => PermissionContext.User;
}
