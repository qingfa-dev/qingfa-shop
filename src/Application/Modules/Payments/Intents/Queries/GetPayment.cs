using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Application.BuildingBlocks.UseCases.Mediation.Queries;

namespace Application.Modules.Payments.Intents.Queries;

/// <summary>Reads one payment; a null customer means an admin read.</summary>
public static class GetPayment
{
    #region UseCase

    /// <summary>The query.</summary>
    /// <param name="PaymentId">The payment id.</param>
    /// <param name="CustomerId">The owning customer, or null for an admin read.</param>
    public sealed record UseCase(Guid PaymentId, Guid? CustomerId) : IQuery<PaymentResponseModel>;

    #endregion


    #region Handler

    /// <summary>Handles the query.</summary>
    public sealed class UseCaseHandler(
        IPaymentReadStore store,
        IEventSourcedRepository<Payment, Guid> repository,
        IPaymentPolicy policy,
        TimeProvider clock) : IQueryHandler<UseCase, PaymentResponseModel>
    {
        /// <inheritdoc />
        public async Task<Result<PaymentResponseModel>> Handle(UseCase query, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);

            return await ReadAsync(query, cancellationToken)
                .BindAsync((row, ct) => Visible(row, query), cancellationToken);
        }

        /// <summary>Reads the projection, falling back to the event stream while it catches up.</summary>
        private async Task<Result<PaymentReadModel>> ReadAsync(
            UseCase query,
            CancellationToken cancellationToken)
        {
            Result<PaymentReadModel> row = await store.FindAsync(query.PaymentId, cancellationToken);

            if (row.IsSuccess)
                return row;

            // Fallback: read the event stream while the projection is still catching up.
            Result<Payment> loaded = await repository.LoadAsync(query.PaymentId, cancellationToken);

            return loaded.Map(ToReadModel);
        }

        /// <summary>Enforces ownership and reports an overdue pending payment as expired.</summary>
        private Task<Result<PaymentResponseModel>> Visible(PaymentReadModel row, UseCase query)
        {
            // Enforce: another customer's payment is reported as absent, never forbidden.
            bool visible = query.CustomerId is null || row.CustomerId == query.CustomerId;

            return Task.FromResult(visible
                ? Result<PaymentResponseModel>.Success(ApplyExpiry(row.ToResponse(), row.ExpiresAt))
                : Result<PaymentResponseModel>.Fail(PaymentsOutcome.Failure.NotFound(query.PaymentId)));
        }

        /// <summary>Projects an aggregate into the read model shape.</summary>
        private static PaymentReadModel ToReadModel(Payment payment) => new()
        {
            Id = payment.Id,
            Provider = payment.Provider.ToWire(),
            ProviderReference = payment.ProviderReference,
            ProviderCaptureReference = payment.ProviderCaptureReference,
            Status = payment.Status.ToWire(),
            Amount = payment.Amount.Amount,
            Currency = payment.Amount.Currency.Code ?? PaymentConstant.Defaults.Currency,
            CapturedAmount = payment.CapturedAmount.Amount,
            RefundedAmount = payment.RefundedAmount.Amount,
            ExternalReference = payment.ExternalReference,
            IdempotencyKey = payment.IdempotencyKey,
            IdempotencyFingerprint = payment.IdempotencyFingerprint,
            CustomerId = payment.CustomerId,
            ApprovalUrl = payment.ApprovalUrl,
            ExpiresAt = payment.ExpiresAt?.UtcDateTime,
            CreatedAt = payment.CreatedAt.UtcDateTime,
            UpdatedAt = payment.UpdatedAt.UtcDateTime,
            FailureCode = payment.FailureCode,
            FailureMessage = payment.FailureMessage,
            Version = (int)payment.Version
        };

        /// <summary>Reports an overdue pending row as expired.</summary>
        private PaymentResponseModel ApplyExpiry(PaymentResponseModel response, DateTime? expiresAt)
        {
            bool expired = response.Status == PaymentStatus.Pending.ToWire()
                && expiresAt is { } due
                && clock.GetUtcNow() >= new DateTimeOffset(due, TimeSpan.Zero) + policy.ProviderExpiryGracePeriod;

            return expired
                ? response with { Status = PaymentStatus.Expired.ToWire(), ApprovalUrl = null }
                : response;
        }
    }

    #endregion
}
