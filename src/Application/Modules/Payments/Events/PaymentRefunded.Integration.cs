using System.Text.Json.Serialization;

using Application.BuildingBlocks.Messaging.Events;

namespace Application.Modules.Payments.Events;

/// <summary>Published when a refund settles so receipts and reporting can react.</summary>
public sealed record PaymentRefundedIntegration : IntegrationEvent
{
    /// <summary>The payment id.</summary>
    public Guid PaymentId { get; init; }

    /// <summary>The refund id.</summary>
    public Guid RefundId { get; init; }

    /// <summary>The refunded amount.</summary>
    public decimal Amount { get; init; }

    /// <summary>The ISO 4217 currency code.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>When the refund was applied.</summary>
    public DateTimeOffset RefundedAtUtc { get; init; }

    /// <inheritdoc />
    [JsonIgnore]
    public override string EventName => PaymentEventConstant.Refunded;

    /// <inheritdoc />
    [JsonIgnore]
    public override int Version => PaymentEventConstant.Version;
}
