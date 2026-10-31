using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;

namespace Application.Modules.PermissionCatalog;

/// <summary>Merges platform and module permission contributions.</summary>
public sealed class ApplicationPermissionCatalog : IPermissionCatalog
{
    private readonly IReadOnlyList<IPermissionCatalogContributor> _contributors;

    /// <summary>Creates the catalog from the registered contributions.</summary>
    /// <param name="contributors">The platform and module contributions.</param>
    public ApplicationPermissionCatalog(IEnumerable<IPermissionCatalogContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);
        _contributors = [.. contributors];

        All = Merge(contributor => contributor.Permissions);
        Admin = Merge(contributor => contributor.Admin);
        Manager = Merge(contributor => contributor.Manager);
        User = Merge(contributor => contributor.User);
    }

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> All { get; }

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Admin { get; }

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Manager { get; }

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> User { get; }

    private IReadOnlyList<PermissionMetadata> Merge(
        Func<IPermissionCatalogContributor, IReadOnlyList<PermissionMetadata>> selector) =>
        [.. _contributors
            .SelectMany(selector)
            .DistinctBy(permission => permission.Identifier, StringComparer.OrdinalIgnoreCase)];
}
