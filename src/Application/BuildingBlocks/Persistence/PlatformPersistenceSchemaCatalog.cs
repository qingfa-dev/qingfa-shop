namespace Application.BuildingBlocks.Persistence;

/// <summary>Platform-owned schemas and tables.</summary>
public sealed class PlatformPersistenceSchemaCatalog
    : IPersistenceSchemaCatalog, IPersistenceSchemaCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<string> Schemas { get; } =
    [
        PersistenceConstant.Schema.Identity,
        PersistenceConstant.Schema.Messaging
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> Tables { get; } =
    [
        "refresh_tokens",
        "passkey_data",
        "passkeys",
        "user_tokens",
        "user_logins",
        "user_roles",
        "user_claims",
        "role_claims",
        "roles",
        "users"
    ];
}
