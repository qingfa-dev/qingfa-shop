using System.Text.Json.Serialization;

using Application.BuildingBlocks.Messaging.Events;

namespace Application.Modules.Payments.Events;

/// <summary>Published when a payment settles so notifications and reporting can react.</summary>
public sealed record PaymentCapturedIntegration : IntegrationEvent
{
    /// <summary>The payment id.</summary>
    public Guid PaymentId { get; init; }

    /// <summary>The captured amount.</summary>
    public decimal Amount { get; init; }

    /// <summary>The ISO 4217 currency code.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>When the capture was applied.</summary>
    public DateTimeOffset CapturedAtUtc { get; init; }

    /// <inheritdoc />
    [JsonIgnore]
    public override string EventName => PaymentEventConstant.Captured;

    /// <inheritdoc />
    [JsonIgnore]
    public override int Version => PaymentEventConstant.Version;
}
