using Core.BuildingBlocks.Domain.Events;
using Core.BuildingBlocks.Domain.EventSourcing;
using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments.Refunds;

namespace Core.Modules.Payments.Intents;

/// <summary>An event-sourced payment intent and its capture/refund state.</summary>
public sealed class Payment : EventSourcedAggregate<Guid>
{
    private readonly List<Refund> _refunds = [];

    internal Payment() { }

    /// <summary>The provider that settles this payment.</summary>
    public PaymentProvider Provider { get; private set; }

    /// <summary>The provider-side intent or order identifier.</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>The immutable requested amount.</summary>
    public Money Amount { get; private set; }

    /// <summary>Automatic or manual capture.</summary>
    public PaymentCaptureMode CaptureMode { get; private set; }

    /// <summary>The caller's cart or order key.</summary>
    public string ExternalReference { get; private set; } = string.Empty;

    /// <summary>The caller's idempotency key.</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>The fingerprint of the request that created the payment.</summary>
    public string IdempotencyFingerprint { get; private set; } = string.Empty;

    /// <summary>The owning customer.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>The current lifecycle state.</summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>Funds held by the provider.</summary>
    public Money AuthorizedAmount { get; private set; }

    /// <summary>Funds captured so far.</summary>
    public Money CapturedAmount { get; private set; }

    /// <summary>Funds refunded so far.</summary>
    public Money RefundedAmount { get; private set; }

    /// <summary>The provider failure code, when failed.</summary>
    public string? FailureCode { get; private set; }

    /// <summary>The provider-safe failure message.</summary>
    public string? FailureMessage { get; private set; }

    /// <summary>The hosted-checkout redirect target while the customer must act.</summary>
    public string? ApprovalUrl { get; private set; }

    /// <summary>When the intent stops being payable.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>When the payment was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the payment last changed.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>The refunds issued against this payment.</summary>
    public IReadOnlyList<Refund> Refunds => _refunds;

    /// <summary>Builds an unpersisted payment before its initiation event is validated.</summary>
    internal static Payment Draft(
        Guid id,
        PaymentProvider provider,
        string? providerReference,
        Money amount,
        PaymentCaptureMode captureMode,
        string? externalReference,
        string? idempotencyKey,
        string? idempotencyFingerprint,
        Guid customerId,
        string? approvalUrl,
        DateTimeOffset? expiresAt,
        DateTimeOffset nowUtc) => new()
        {
            Id = id,
            Provider = provider,
            ProviderReference = providerReference,
            Amount = amount,
            CaptureMode = captureMode,
            ExternalReference = externalReference?.Trim() ?? string.Empty,
            IdempotencyKey = idempotencyKey?.Trim() ?? string.Empty,
            IdempotencyFingerprint = idempotencyFingerprint ?? string.Empty,
            CustomerId = customerId,
            ApprovalUrl = approvalUrl,
            ExpiresAt = expiresAt,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            Status = PaymentStatus.Pending,
            AuthorizedAmount = new Money(0, amount.Currency),
            CapturedAmount = new Money(0, amount.Currency),
            RefundedAmount = new Money(0, amount.Currency)
        };

    /// <summary>The remaining amount that can still be refunded; pending refunds reserve credit until they settle.</summary>
    public decimal CreditAllowed => CapturedAmount.Amount
        - RefundedAmount.Amount
        - _refunds.Where(refund => refund.Status is RefundStatus.Pending)
            .Sum(refund => refund.Amount.Amount);

    /// <summary>The remaining amount that can still be captured.</summary>
    public decimal RemainingCapturable => Math.Max(0m, Amount.Amount - CapturedAmount.Amount);

    /// <summary>Whether the payment can accept a capture right now.</summary>
    public bool CanCapture => Status is PaymentStatus.Pending
        or PaymentStatus.Processing
        or PaymentStatus.Authorized
        or PaymentStatus.Expired
        or PaymentStatus.Failed;

    /// <summary>Reports whether an unpaid intent is past its expiry, including the grace period.</summary>
    /// <param name="nowUtc">The current instant.</param>
    /// <param name="grace">Extra time allowed before expiry is enforced.</param>
    /// <returns>True when the intent can no longer be paid.</returns>
    public bool IsExpired(DateTimeOffset nowUtc, TimeSpan grace)
        => ExpiresAt is { } expiresAt
            && Status is PaymentStatus.Pending
            && nowUtc >= expiresAt + grace;

    /// <summary>Whether the payment can accept a refund right now.</summary>
    public bool CanRefund => (Status is PaymentStatus.Captured or PaymentStatus.PartiallyRefunded)
        && CreditAllowed > 0;

    /// <summary>Applies a provider observation using the aggregate's transition rules.</summary>
    public Result<Payment> ApplyObservation(
        PaymentObservation observation,
        string providerReference,
        Money? amount,
        string? failureCode,
        DateTimeOffset nowUtc) => observation switch
        {
            PaymentObservation.Authorized => Authorize(providerReference, amount ?? Amount, nowUtc),
            PaymentObservation.Captured => Capture(amount ?? Amount, providerReference, nowUtc),
            PaymentObservation.Failed => Fail(failureCode ?? PaymentConstant.Reasons.ProviderFailed, null, nowUtc),
            PaymentObservation.Cancelled => Cancel(PaymentConstant.Reasons.ProviderCancelled, nowUtc),
            PaymentObservation.Expired => ApplyProviderExpiry(nowUtc),
            _ => Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "observed"))
        };

    /// <summary>Marks the payment as processing.</summary>
    public Result<Payment> MarkProcessing(string? providerReference, DateTimeOffset nowUtc)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "processed"));

        return RaiseEvent(new PaymentProcessing(Id, providerReference, nowUtc));
    }

    /// <summary>Records an authorization for manual capture.</summary>
    public Result<Payment> Authorize(string providerReference, Money authorized, DateTimeOffset nowUtc)
    {
        if (CaptureMode is not PaymentCaptureMode.Manual)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "authorized"));

        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "authorized"));

        if (authorized.Amount <= 0 || authorized.Amount > Amount.Amount)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.AmountInvalid("authorized amount is out of range"));

        if (authorized.Currency != Amount.Currency)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.CurrencyMismatch(
                Amount.Currency.Code ?? string.Empty, authorized.Currency.Code ?? string.Empty));

        return RaiseEvent(new PaymentAuthorized(Id, providerReference, authorized, nowUtc));
    }

    /// <summary>Captures part or all of the payment.</summary>
    public Result<Payment> Capture(Money captured, string providerReference, DateTimeOffset nowUtc)
    {
        // Validate: A capture is only legal while the payment is in a capturable state.
        if (!CanCapture)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "captured"));

        bool lateSettlement = Status is PaymentStatus.Expired or PaymentStatus.Failed;

        // Enforce: Manual capture needs a prior authorization except for late settlement.
        if (CaptureMode is PaymentCaptureMode.Manual && Status is not PaymentStatus.Authorized && !lateSettlement)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "captured before authorization"));

        // Validate: reference, amount, currency and remaining balance in one place.
        Result validation = PaymentValidator.ValidateCapture(this, captured, providerReference);

        if (validation.IsFailure)
            return Result<Payment>.Fail(validation.Errors);

        // Enforce: Automatic capture and late settlement must take the full remainder.
        if (captured.Amount < RemainingCapturable && Status is not PaymentStatus.Authorized)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.AmountInvalid(
                "partial capture requires an authorization"));

        string reason = Status switch
        {
            PaymentStatus.Expired => PaymentConstant.Reasons.LateSettlement,
            PaymentStatus.Failed => PaymentConstant.Reasons.SettledAfterFailure,
            _ => PaymentConstant.Reasons.InitialCapture
        };

        return RaiseEvent(new PaymentCaptured(
            Id,
            providerReference,
            captured,
            new Money(CapturedAmount.Amount + captured.Amount, Amount.Currency),
            reason,
            nowUtc));
    }

    /// <summary>Records a rejected capture without moving the payment.</summary>
    public Result<Payment> CaptureFailed(Money attempted, string code, string? message, DateTimeOffset nowUtc)
    {
        if (Status is not PaymentStatus.Authorized)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "marked as a failed capture"));

        return RaiseEvent(new PaymentCaptureFailed(
            Id,
            attempted,
            PaymentValidator.NormalizeFailureCode(code) ?? PaymentConstant.Reasons.CaptureFailed,
            message,
            nowUtc));
    }

    /// <summary>Marks the payment failed.</summary>
    public Result<Payment> Fail(string code, string? message, DateTimeOffset nowUtc)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "failed"));

        return RaiseEvent(new PaymentFailed(
            Id,
            PaymentValidator.NormalizeFailureCode(code) ?? PaymentConstant.Reasons.ProviderFailed,
            message,
            nowUtc));
    }

    /// <summary>Voids the payment before any capture.</summary>
    public Result<Payment> Cancel(string reason, DateTimeOffset nowUtc)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing or PaymentStatus.Authorized))
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "cancelled"));

        if (CapturedAmount.Amount > 0)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "cancelled after capture"));

        return RaiseEvent(new PaymentCancelled(Id, reason, nowUtc));
    }

    /// <summary>Expires an unpaid intent once its expiry instant has passed.</summary>
    public Result<Payment> Expire(DateTimeOffset nowUtc)
    {
        // Validate: Only a pending intent can expire.
        if (Status is not PaymentStatus.Pending)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "expired"));

        // Enforce: The expiry instant must exist and have passed; grace periods belong to Application.
        if (ExpiresAt is null || nowUtc < ExpiresAt)
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "expired before its expiry instant"));

        return RaiseEvent(new PaymentExpired(Id, nowUtc));
    }

    /// <inheritdoc />
    protected override void When(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case PaymentInitiated initiated:
                Id = initiated.PaymentId;
                Provider = initiated.Provider;
                ProviderReference = initiated.ProviderReference;
                Amount = initiated.Amount;
                CaptureMode = initiated.CaptureMode;
                ExternalReference = initiated.ExternalReference;
                IdempotencyKey = initiated.IdempotencyKey;
                IdempotencyFingerprint = initiated.IdempotencyFingerprint;
                CustomerId = initiated.CustomerId;
                ApprovalUrl = initiated.ApprovalUrl;
                ExpiresAt = initiated.ExpiresAt;
                CreatedAt = initiated.CreatedAtUtc;
                UpdatedAt = initiated.CreatedAtUtc;
                Status = PaymentStatus.Pending;
                AuthorizedAmount = new Money(0, initiated.Amount.Currency);
                CapturedAmount = new Money(0, initiated.Amount.Currency);
                RefundedAmount = new Money(0, initiated.Amount.Currency);
                break;

            case PaymentProcessing processing:
                ProviderReference ??= processing.ProviderReference;
                Status = PaymentStatus.Processing;
                UpdatedAt = processing.OccurredAtUtc;
                break;

            case PaymentAuthorized authorized:
                ProviderReference = authorized.ProviderReference;
                AuthorizedAmount = authorized.AuthorizedAmount;
                Status = PaymentStatus.Authorized;
                UpdatedAt = authorized.OccurredAtUtc;
                break;

            case PaymentCaptured captured:
                ProviderReference = captured.ProviderReference;
                CapturedAmount = captured.TotalCapturedAmount;
                // Enforce: A partial capture preserves the outstanding authorization amount.
                if (CapturedAmount.Amount >= Amount.Amount)
                    AuthorizedAmount = Amount;
                Status = RefundedAmount.Amount > 0
                    ? (CapturedAmount.Amount > RefundedAmount.Amount ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded)
                    : (CapturedAmount.Amount < Amount.Amount ? PaymentStatus.Authorized : PaymentStatus.Captured);
                ApprovalUrl = null;
                UpdatedAt = captured.OccurredAtUtc;
                break;

            case PaymentCaptureFailed captureFailed:
                FailureCode = captureFailed.FailureCode;
                FailureMessage = captureFailed.FailureMessage;
                UpdatedAt = captureFailed.OccurredAtUtc;
                break;

            case PaymentFailed failed:
                FailureCode = failed.FailureCode;
                FailureMessage = failed.FailureMessage;
                Status = PaymentStatus.Failed;
                ApprovalUrl = null;
                UpdatedAt = failed.OccurredAtUtc;
                break;

            case PaymentCancelled cancelled:
                Status = PaymentStatus.Cancelled;
                ApprovalUrl = null;
                UpdatedAt = cancelled.OccurredAtUtc;
                break;

            case PaymentExpired expired:
                Status = PaymentStatus.Expired;
                ApprovalUrl = null;
                UpdatedAt = expired.ExpiredAtUtc;
                break;

            case RefundRequested requested:
                _refunds.Add(Refund.Request(requested));
                UpdatedAt = requested.OccurredAtUtc;
                break;

            case RefundSucceeded succeeded:
                ApplyRefundSucceeded(succeeded);
                break;

            case RefundFailed failed:
                Refund? refund = _refunds.FirstOrDefault(item => item.Id == failed.RefundId);
                refund?.MarkFailed(failed.FailureCode, failed.FailureMessage);
                UpdatedAt = failed.OccurredAtUtc;
                break;
        }
    }

    /// <summary>Applies an event through the single mutation path.</summary>
    internal Result<Payment> RaiseEvent(IDomainEvent domainEvent)
    {
        Raise(domainEvent);
        return Result<Payment>.Success(this);
    }

    /// <summary>Applies a provider-reported expiry even while the payment is processing.</summary>
    private Result<Payment> ApplyProviderExpiry(DateTimeOffset nowUtc)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            return Result<Payment>.Fail(PaymentsOutcome.Failure.InvalidState(Status, "expired by the provider"));

        return RaiseEvent(new PaymentExpired(Id, nowUtc));
    }

    private void ApplyRefundSucceeded(RefundSucceeded succeeded)
    {
        Refund? refund = _refunds.FirstOrDefault(item => item.Id == succeeded.RefundId);
        refund?.MarkSucceeded(succeeded.ProviderReference);
        RefundedAmount = new Money(RefundedAmount.Amount + succeeded.Amount.Amount, Amount.Currency);
        Status = CapturedAmount.Amount > RefundedAmount.Amount
            ? PaymentStatus.PartiallyRefunded
            : PaymentStatus.Refunded;
        UpdatedAt = succeeded.OccurredAtUtc;
    }
}