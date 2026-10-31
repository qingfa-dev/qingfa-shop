using Application.BuildingBlocks.Persistence;

namespace Application.Modules.Persistence;

/// <summary>Merges platform and module persistence schemas and tables.</summary>
public sealed class ApplicationPersistenceSchemaCatalog : IPersistenceSchemaCatalog
{
    private readonly IReadOnlyList<IPersistenceSchemaCatalogContributor> _catalogs;

    /// <summary>Creates the catalog from the registered contributions.</summary>
    /// <param name="catalogs">The platform and module schema contributions.</param>
    public ApplicationPersistenceSchemaCatalog(
        IEnumerable<IPersistenceSchemaCatalogContributor> catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        _catalogs = [.. catalogs];

        Schemas = [.. _catalogs
            .SelectMany(catalog => catalog.Schemas)
            .Distinct(StringComparer.Ordinal)];
        Tables = [.. _catalogs
            .SelectMany(catalog => catalog.Tables)
            .Distinct(StringComparer.Ordinal)];
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Schemas { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Tables { get; }
}
