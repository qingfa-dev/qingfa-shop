using System.Diagnostics.CodeAnalysis;

using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Messaging.Serialization;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Refunds.Models;

using Core.BuildingBlocks.Domain.Events;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;
using Core.Modules.Payments.Refunds;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Projects payment events into the payments and refunds tables.</summary>
[ExcludeFromCodeCoverage]
public sealed class PaymentProjection(DbContext context, IDomainEventSerializer serializer) : IProjection
{
    /// <summary>The projection checkpoint name.</summary>
    public const string PaymentProjectionName = "payments";

    /// <inheritdoc />
    public string Name => PaymentProjectionName;

    /// <inheritdoc />
    public async Task<Result> ApplyAsync(StoredEvent storedEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storedEvent);

        IDomainEvent domainEvent = serializer.Deserialize(
            storedEvent.EventName, storedEvent.EventVersion, storedEvent.Payload);

        switch (domainEvent)
        {
            case PaymentInitiated initiated:
                context.Set<PaymentReadModel>().Add(ToRow(initiated, storedEvent.Version));
                break;

            case PaymentProcessing processing:
                await UpdateAsync(processing.PaymentId, row =>
                {
                    row.ProviderReference ??= processing.ProviderReference;
                    row.Status = PaymentStatus.Processing.ToWire();
                    row.UpdatedAt = processing.OccurredAtUtc.UtcDateTime;
                }, cancellationToken);
                break;

            case PaymentAuthorized authorized:
                await UpdateAsync(authorized.PaymentId, row =>
                {
                    row.ProviderReference = authorized.ProviderReference;
                    row.Status = PaymentStatus.Authorized.ToWire();
                    row.UpdatedAt = authorized.OccurredAtUtc.UtcDateTime;
                }, cancellationToken);
                break;

            case PaymentCaptured captured:
                await UpdateAsync(captured.PaymentId, row =>
                {
                    row.ProviderReference = captured.ProviderReference;
                    row.ProviderCaptureReference = captured.ProviderCaptureReference;
                    row.CapturedAmount = captured.TotalCapturedAmount.Amount;
                    row.Status = row.RefundedAmount > 0
                        ? (row.CapturedAmount > row.RefundedAmount
                            ? PaymentStatus.PartiallyRefunded.ToWire()
                            : PaymentStatus.Refunded.ToWire())
                        : PaymentStatus.Captured.ToWire();
                    row.ApprovalUrl = null;
                    row.FailureCode = null;
                    row.FailureMessage = null;
                    row.UpdatedAt = captured.OccurredAtUtc.UtcDateTime;
                }, cancellationToken);
                break;

            case PaymentCaptureFailed captureFailed:
                await UpdateAsync(captureFailed.PaymentId, row =>
                {
                    row.FailureCode = captureFailed.FailureCode;
                    row.FailureMessage = captureFailed.FailureMessage;
                    row.UpdatedAt = captureFailed.OccurredAtUtc.UtcDateTime;
                }, cancellationToken);
                break;

            case PaymentFailed failed:
                await UpdateAsync(failed.PaymentId, row =>
                {
                    row.Status = PaymentStatus.Failed.ToWire();
                    row.FailureCode = failed.FailureCode;
                    row.FailureMessage = failed.FailureMessage;
                    row.ApprovalUrl = null;
                    row.UpdatedAt = failed.OccurredAtUtc.UtcDateTime;
                }, cancellationToken);
                break;

            case PaymentCancelled cancelled:
                await UpdateAsync(cancelled.PaymentId, row =>
                {
                    row.Status = PaymentStatus.Cancelled.ToWire();
                    row.ApprovalUrl = null;
                    row.UpdatedAt = cancelled.OccurredAtUtc.UtcDateTime;
                }, cancellationToken);
                break;

            case PaymentExpired expired:
                await UpdateAsync(expired.PaymentId, row =>
                {
                    row.Status = PaymentStatus.Expired.ToWire();
                    row.ApprovalUrl = null;
                    row.UpdatedAt = expired.ExpiredAtUtc.UtcDateTime;
                }, cancellationToken);
                break;

            case RefundRequested requested:
                context.Set<RefundReadModel>().Add(new RefundReadModel
                {
                    Id = requested.RefundId,
                    PaymentId = requested.PaymentId,
                    Status = RefundStatus.Pending.ToWire(),
                    Amount = requested.Amount.Amount,
                    Currency = requested.Amount.Currency.Code ?? PaymentConstant.Defaults.Currency,
                    Reason = requested.Reason,
                    IdempotencyKey = requested.IdempotencyKey,
                    CreatedAt = requested.OccurredAtUtc.UtcDateTime,
                    UpdatedAt = requested.OccurredAtUtc.UtcDateTime
                });
                break;

            case RefundSucceeded succeeded:
                await UpdateRefundAsync(succeeded.RefundId, refund =>
                {
                    refund.Status = RefundStatus.Succeeded.ToWire();
                    refund.ProviderReference = succeeded.ProviderReference;
                    refund.UpdatedAt = succeeded.OccurredAtUtc.UtcDateTime;
                }, cancellationToken);
                await UpdateAsync(succeeded.PaymentId, row =>
                {
                    row.RefundedAmount += succeeded.Amount.Amount;
                    row.Status = row.CapturedAmount > row.RefundedAmount
                        ? PaymentStatus.PartiallyRefunded.ToWire()
                        : PaymentStatus.Refunded.ToWire();
                    row.UpdatedAt = succeeded.OccurredAtUtc.UtcDateTime;
                }, cancellationToken);
                break;

            case RefundFailed failed:
                await UpdateRefundAsync(failed.RefundId, refund =>
                {
                    refund.Status = RefundStatus.Failed.ToWire();
                    refund.FailureCode = failed.FailureCode;
                    refund.FailureMessage = failed.FailureMessage;
                    refund.UpdatedAt = failed.OccurredAtUtc.UtcDateTime;
                }, cancellationToken);
                break;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <summary>Maps the initiation event to its first projection row.</summary>
    private static PaymentReadModel ToRow(PaymentInitiated initiated, long version) => new()
    {
        Id = initiated.PaymentId,
        Provider = initiated.Provider.ToWire(),
        ProviderReference = initiated.ProviderReference,
        Status = PaymentStatus.Pending.ToWire(),
        Amount = initiated.Amount.Amount,
        Currency = initiated.Amount.Currency.Code ?? PaymentConstant.Defaults.Currency,
        ExternalReference = initiated.ExternalReference,
        IdempotencyKey = initiated.IdempotencyKey,
        IdempotencyFingerprint = initiated.IdempotencyFingerprint,
        CustomerId = initiated.CustomerId,
        ApprovalUrl = initiated.ApprovalUrl,
        ExpiresAt = initiated.ExpiresAt?.UtcDateTime,
        CreatedAt = initiated.CreatedAtUtc.UtcDateTime,
        UpdatedAt = initiated.CreatedAtUtc.UtcDateTime,
        Version = (int)version
    };

    /// <summary>Applies a change to an existing payment row.</summary>
    private async Task UpdateAsync(Guid id, Action<PaymentReadModel> change, CancellationToken cancellationToken)
    {
        PaymentReadModel? row = await context.Set<PaymentReadModel>().FindAsync([id], cancellationToken);

        if (row is not null)
            change(row);
    }

    /// <summary>Applies a change to an existing refund row.</summary>
    private async Task UpdateRefundAsync(Guid id, Action<RefundReadModel> change, CancellationToken cancellationToken)
    {
        RefundReadModel? row = await context.Set<RefundReadModel>().FindAsync([id], cancellationToken);

        if (row is not null)
            change(row);
    }
}
