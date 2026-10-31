using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;

namespace Application.Modules.Payments.Permissions;

/// <summary>Payments permission definitions.</summary>
public sealed class PaymentPermissionCatalog : IPermissionCatalogContributor
{
    /// <summary>Payment permissions.</summary>
    public static class Payments
    {
        /// <summary>Read payments.</summary>
        public static readonly PermissionMetadata Read =
            PermissionMetadata.Create("payment", "payments", "payment", "read");

        /// <summary>Capture and cancel payments.</summary>
        public static readonly PermissionMetadata Manage =
            PermissionMetadata.Create("payment", "payments", "payment", "manage");

        /// <summary>Refund payments.</summary>
        public static readonly PermissionMetadata Refund =
            PermissionMetadata.Create("payment", "payments", "payment", "refund");
    }

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Permissions { get; } = [Payments.Read, Payments.Manage, Payments.Refund];

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Admin { get; } = [Payments.Read, Payments.Manage, Payments.Refund];

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> Manager { get; } = [Payments.Read];

    /// <inheritdoc />
    public IReadOnlyList<PermissionMetadata> User { get; } = [];
}
