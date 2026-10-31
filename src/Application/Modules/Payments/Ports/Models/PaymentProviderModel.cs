using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;

namespace Application.Modules.Payments.Ports.Models;

/// <summary>The canonical status a provider reports for an intent.</summary>
public enum PaymentProviderIntentStatus
{
    RequiresAction,
    Processing,
    Authorized,
    Captured,
    Failed,
    Cancelled
}

/// <summary>The canonical event types the module acts on.</summary>
public enum PaymentProviderEventType
{
    Authorized,
    Captured,
    Failed,
    Cancelled,
    Expired,
    Refunded,
    RefundFailed,
    DisputeOpened,
    DisputeWon,
    DisputeLost
}

/// <summary>An intent-creation request.</summary>
public sealed record PaymentIntentRequest(
    Guid PaymentId,
    Money Amount,
    PaymentProvider Provider,
    Guid CustomerId,
    string ExternalReference,
    string? ReturnUrl,
    string? CancelUrl,
    PaymentCaptureMode CaptureMode,
    string IdempotencyKey,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>An intent-creation or retrieval result.</summary>
public sealed record PaymentIntentResult(
    string ProviderReference,
    PaymentProviderIntentStatus Status,
    string? ApprovalUrl,
    string? ClientSecret,
    DateTimeOffset? ExpiresAt,
    string? FailureCode);

/// <summary>A capture request.</summary>
public sealed record PaymentCaptureRequest(string ProviderReference, Money Amount, string IdempotencyKey);

/// <summary>A capture result.</summary>
public sealed record PaymentCaptureResult(
    string ProviderReference,
    Money CapturedAmount,
    bool Succeeded,
    string? FailureCode,
    string? CaptureReference = null);

/// <summary>A void/cancel request.</summary>
public sealed record PaymentCancelRequest(string ProviderReference, string IdempotencyKey);

/// <summary>A void/cancel result.</summary>
public sealed record PaymentCancelResult(bool Succeeded, string? FailureCode);

/// <summary>A refund request.</summary>
public sealed record PaymentRefundRequest(
    string ProviderReference,
    Guid RefundId,
    Money Amount,
    string IdempotencyKey,
    string? Reason,
    string? CaptureReference = null);

/// <summary>A refund result.</summary>
public sealed record PaymentRefundResult(string ProviderReference, Money Amount, bool Succeeded, string? FailureCode);

/// <summary>A normalized webhook event.</summary>
public sealed record PaymentProviderEvent(
    string EventId,
    PaymentProviderEventType Type,
    string ProviderReference,
    string? RefundReference,
    Money? Amount,
    DateTimeOffset OccurredAt,
    string? CaptureReference = null,
    string? DisputeReference = null,
    string? DisputeReason = null);
