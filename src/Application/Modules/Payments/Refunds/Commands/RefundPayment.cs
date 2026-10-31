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

            Result<Payment> loaded = await repository.LoadAsync(command.PaymentId, cancellationToken);

            if (loaded.IsFailure)
                return Result<Response>.Fail(loaded.Errors);

            Payment payment = loaded.Value;

            Result<IPaymentGateway> resolved = registry.Resolve(payment.Provider);

            if (resolved.IsFailure)
                return Result<Response>.Fail(resolved.Errors);

            IPaymentGateway gateway = resolved.Value;

            // Validate: a provider without the capability may only refund the full remainder.
            if (command.Request.Amount.Amount < payment.CreditAllowed
                && !gateway.Capabilities.SupportsPartialRefund)
                return Result<Response>.Fail(PaymentsOutcome.Failure.PartialRefundUnsupported());

            if (payment.ProviderReference is null)
                return Result<Response>.Fail(PaymentsOutcome.Failure.ProviderReferenceMissing());

            Guid refundId = Guid.NewGuid();

            Result<Payment> requested = payment.RequestRefund(
                refundId,
                command.Request.Amount,
                command.Request.Reason,
                command.IdempotencyKey,
                clock.GetUtcNow());

            if (requested.IsFailure)
                return Result<Response>.Fail(requested.Errors);

            Result<PaymentRefundResult> refunded = await gateway.RefundAsync(
                new PaymentRefundRequest(
                    payment.ProviderReference,
                    refundId,
                    command.Request.Amount,
                    $"{PaymentConstant.IdempotencyPrefixes.Refund}{refundId:N}",
                    command.Request.Reason),
                cancellationToken);

            Result<Payment> completed = refunded.IsFailure || !refunded.Value.Succeeded
                ? requested.Value.FailRefund(
                    refundId,
                    refunded.IsFailure ? refunded.Errors[0].Code : refunded.Value.FailureCode ?? PaymentConstant.Reasons.RefundFailed,
                    refunded.IsFailure ? refunded.Errors[0].Message : null,
                    clock.GetUtcNow())
                : requested.Value.CompleteRefund(refundId, refunded.Value.ProviderReference, clock.GetUtcNow());

            if (completed.IsFailure)
                return Result<Response>.Fail(completed.Errors);

            Result saved = await repository.SaveAsync(completed.Value, cancellationToken);

            if (saved.IsFailure)
                return Result<Response>.Fail(saved.Errors);

            PaymentMetrics.RefundsSucceeded.Add(1);

            Refund refund = completed.Value.Refunds.Single(item => item.Id == refundId);

            return Result<Response>.Success(new Response(
                    refund.Id,
                    payment.Id,
                    refund.Status.ToWire(),
                    refund.Amount.Amount,
                    refund.Amount.Currency.Code ?? payment.Amount.Currency.Code ?? PaymentConstant.Defaults.Currency,
                    refund.ProviderReference))
                .WithStatus(ResultConstant.StatusCode.Created);
        }
    }

    #endregion
}
