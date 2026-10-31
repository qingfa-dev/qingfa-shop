using Application.BuildingBlocks.Persistence;

namespace Application.Modules.Payments.Persistence;

/// <summary>Payments schema and tables.</summary>
public sealed class PaymentPersistenceSchemaCatalog
    : IPersistenceSchemaCatalog, IPersistenceSchemaCatalogContributor
{
    /// <summary>The Payments schema name.</summary>
    public const string SchemaName = "payment";

    /// <inheritdoc />
    public IReadOnlyList<string> Schemas { get; } = [SchemaName];

    /// <inheritdoc />
    public IReadOnlyList<string> Tables { get; } =
    [
        "payments",
        "payment_refunds",
        "payment_webhook_events",
        "payment_idempotency_keys",
        "payment_references",
        "payment_disputes"
    ];
}
