using System.Text.Json.Serialization;

using Application.BuildingBlocks.Messaging.Events;

namespace Application.Modules.Payments.Events;

/// <summary>Published when a payment fails so operators can be alerted.</summary>
public sealed record PaymentFailedIntegration : IntegrationEvent
{
    /// <summary>The payment id.</summary>
    public Guid PaymentId { get; init; }

    /// <summary>The provider failure code.</summary>
    public string FailureCode { get; init; } = string.Empty;

    /// <summary>When the failure occurred.</summary>
    public DateTimeOffset FailedAtUtc { get; init; }

    /// <inheritdoc />
    [JsonIgnore]
    public override string EventName => PaymentEventConstant.Failed;

    /// <inheritdoc />
    [JsonIgnore]
    public override int Version => PaymentEventConstant.Version;
}
