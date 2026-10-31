using System.Text.Json.Serialization;

using Core.BuildingBlocks.Domain.Events;
using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.BuildingBlocks.Modeling.Metadata;

namespace Core.Modules.Payments.Refunds;

/// <summary>Raised when a refund is requested.</summary>
public sealed record RefundRequested(
    Guid RefundId,
    Guid PaymentId,
    Money Amount,
    string Reason,
    string IdempotencyKey,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when the provider confirms a refund.</summary>
public sealed record RefundSucceeded(
    Guid RefundId,
    Guid PaymentId,
    Money Amount,
    string ProviderReference,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when the provider rejects a refund.</summary>
public sealed record RefundFailed(
    Guid RefundId,
    Guid PaymentId,
    string FailureCode,
    string? FailureMessage,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);