using System.Text.Json.Serialization;

using Core.BuildingBlocks.Domain.Events;
using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.BuildingBlocks.Modeling.Metadata;

namespace Core.Modules.Payments.Intents;

/// <summary>Raised when a payment intent is created.</summary>
public sealed record PaymentInitiated(
    Guid PaymentId,
    PaymentProvider Provider,
    string? ProviderReference,
    Money Amount,
    PaymentCaptureMode CaptureMode,
    string ExternalReference,
    string IdempotencyKey,
    string IdempotencyFingerprint,
    Guid CustomerId,
    string? ApprovalUrl,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when the provider starts processing the payment.</summary>
public sealed record PaymentProcessing(
    Guid PaymentId,
    string? ProviderReference,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when funds are authorized but not captured.</summary>
public sealed record PaymentAuthorized(
    Guid PaymentId,
    string ProviderReference,
    Money AuthorizedAmount,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when funds are captured, fully or partially.</summary>
public sealed record PaymentCaptured(
    Guid PaymentId,
    string ProviderReference,
    string? ProviderCaptureReference,
    Money CapturedAmount,
    Money TotalCapturedAmount,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a capture attempt is rejected by the provider.</summary>
public sealed record PaymentCaptureFailed(
    Guid PaymentId,
    Money AttemptedAmount,
    string FailureCode,
    string? FailureMessage,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when the payment fails.</summary>
public sealed record PaymentFailed(
    Guid PaymentId,
    string FailureCode,
    string? FailureMessage,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when a payment is voided before capture.</summary>
public sealed record PaymentCancelled(
    Guid PaymentId,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);

/// <summary>Raised when an intent expires unpaid.</summary>
public sealed record PaymentExpired(
    Guid PaymentId,
    DateTimeOffset ExpiredAtUtc,
    [property: JsonIgnore] DefaultMetadata? metadata = null) : DomainEvent(metadata: metadata);