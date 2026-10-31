using System.Text.Json.Serialization;

using Core.BuildingBlocks.Domain.Events;
using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.BuildingBlocks.Modeling.Metadata;

namespace Core.Modules.Payments.Disputes;

/// <summary>Raised when a provider opens a dispute against a captured payment.</summary>
public sealed record DisputeOpened(
    Guid PaymentId,
    Guid DisputeId,
    string ProviderDisputeReference,
    Money Amount,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a provider resolves a dispute.</summary>
public sealed record DisputeResolved(
    Guid PaymentId,
    Guid DisputeId,
    DisputeOutcome Outcome,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);
