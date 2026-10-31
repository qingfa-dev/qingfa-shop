using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;
using Core.Modules.Payments.Refunds;

namespace Application.Modules.Payments.Refunds.Commands;

/// <summary>Refunds captured funds.</summary>
public static class RefundPayment
{
    #region Request

    /// <summary>The request body.</summary>
    /// <param name="Amount">The amount to refund.</param>
    /// <param name="Reason">Why the funds are being returned.</param>
    public sealed record Request(Money Amount, string Reason);

    #endregion

    #region Response

    /// <summary>The refund view.</summary>
    /// <param name="Id">The refund id.</param>
    /// <param name="PaymentId">The payment id.</param>
    /// <param name="Status">The refund status.</param>
    /// <param name="Amount">The refunded amount.</param>
    /// <param name="Currency">The currency code.</param>
    /// <param name="ProviderReference">The provider refund reference.</param>
    public sealed record Response(
        Guid Id,
        Guid PaymentId,
        string Status,
        decimal Amount,
        string Currency,
        string? ProviderReference);

    #endregion

    #region UseCase

    /// <summary>The command.</summary>
    /// <param name="PaymentId">The payment to refund.</param>
    /// <param name="Request">The request body.</param>
    /// <param name="IdempotencyKey">The caller's idempotency key.</param>
    public sealed record UseCase(Guid PaymentId, Request Request, string IdempotencyKey) : ICommand<Response>;

    #endregion


    #region Handler

    /// <summary>Handles the command.</summary>
    public sealed class UseCaseHandler(
        IEventSourcedRepository<Payment, Guid> repository,
        IPaymentGatewayRegistry registry,
        TimeProvider clock) : ICommandHandler<UseCase, Response>
    {
        /// <inheritdoc />
        public async Task<Result<Response>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            Guid refundId = Guid.NewGuid();

            Result<Payment> refunded = await repository
                .LoadAsync(command.PaymentId, cancellationToken)
                .BindAsync((payment, ct) => RefundAsync(payment, command, refundId, ct), cancellationToken)
                .BindAsync((payment, ct) => SaveAsync(payment, ct), cancellationToken);

            return Created(refunded
                .Map(payment => ToResponse(payment.Refunds.Single(item => item.Id == refundId), payment.Id)));
        }

        /// <summary>Requests the refund and applies the provider outcome to the aggregate.</summary>
        private async Task<Result<Payment>> RefundAsync(
            Payment payment,
            UseCase command,
            Guid refundId,
            CancellationToken cancellationToken)
            => await registry.Resolve(payment.Provider)
                .BindAsync(
                    (gateway, ct) => RefundAtProviderAsync(payment, gateway, command, refundId, ct),
                    cancellationToken);

        /// <summary>Calls the provider and completes or fails the refund accordingly.</summary>
        private async Task<Result<Payment>> RefundAtProviderAsync(
            Payment payment,
            IPaymentGateway gateway,
            UseCase command,
            Guid refundId,
            CancellationToken cancellationToken)
        {
            // Validate: a provider without the capability may only refund the full remainder.
            if (command.Request.Amount.Amount < payment.CreditAllowed
                && !gateway.Capabilities.SupportsPartialRefund)
                return Result<Payment>.Fail(PaymentsOutcome.Failure.PartialRefundUnsupported());

            if (payment.ProviderReference is null)
                return Result<Payment>.Fail(PaymentsOutcome.Failure.ProviderReferenceMissing());

            return await payment
                .RequestRefund(
                    refundId,
                    command.Request.Amount,
                    command.Request.Reason,
                    command.IdempotencyKey,
                    clock.GetUtcNow())
                .BindAsync(
                    (requested, ct) => CallProviderAsync(requested, gateway, command, refundId, ct),
                    cancellationToken);
        }

        /// <summary>Refunds at the provider, then completes or fails the recorded refund.</summary>
        private async Task<Result<Payment>> CallProviderAsync(
            Payment payment,
            IPaymentGateway gateway,
            UseCase command,
            Guid refundId,
            CancellationToken cancellationToken)
        {
            Result<PaymentRefundResult> refunded = await PaymentMetrics.TrackAsync(
                payment.Provider,
                PaymentConstant.Operations.Refund,
                () => gateway.RefundAsync(
                    new PaymentRefundRequest(
                        payment.ProviderReference!,
                        refundId,
                        command.Request.Amount,
                        $"{PaymentConstant.IdempotencyPrefixes.Refund}{refundId:N}",
                        command.Request.Reason,
                        payment.ProviderCaptureReference),
                    cancellationToken));

            return refunded.IsSuccess && refunded.Value.Succeeded
                ? payment.CompleteRefund(refundId, refunded.Value.ProviderReference, clock.GetUtcNow())
                : payment.FailRefund(
                    refundId,
                    refunded.IsFailure
                        ? refunded.Errors[0].Code
                        : refunded.Value.FailureCode ?? PaymentConstant.Reasons.RefundFailed,
                    refunded.IsFailure ? refunded.Errors[0].Message : null,
                    clock.GetUtcNow());
        }

        /// <summary>Persists the aggregate and reports the refund metric.</summary>
        private async Task<Result<Payment>> SaveAsync(Payment payment, CancellationToken cancellationToken)
        {
            Result saved = await repository.SaveAsync(payment, cancellationToken);

            saved.Tap(() => PaymentMetrics.RefundsSucceeded.Add(1));

            return saved.Map(() => payment);
        }

        /// <summary>Applies the created status to a successful result only.</summary>
        private static Result<Response> Created(Result<Response> result)
            => result.IsSuccess
                ? result.WithStatus(ResultConstant.StatusCode.Created)
                : result;

        /// <summary>Maps the refund entity onto the response.</summary>
        private static Response ToResponse(Refund refund, Guid paymentId) => new(
            refund.Id,
            paymentId,
            refund.Status.ToWire(),
            refund.Amount.Amount,
            refund.Amount.Currency.Code ?? PaymentConstant.Defaults.Currency,
            refund.ProviderReference);
    }

    #endregion
}
