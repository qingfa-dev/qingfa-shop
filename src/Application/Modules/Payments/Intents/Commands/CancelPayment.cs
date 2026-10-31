using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

namespace Application.Modules.Payments.Intents.Commands;

/// <summary>Voids an uncaptured payment.</summary>
public static class CancelPayment
{
    #region UseCase

    /// <summary>The command.</summary>
    /// <param name="PaymentId">The payment to void.</param>
    public sealed record UseCase(Guid PaymentId) : ICommand<PaymentResponseModel>;

    #endregion


    #region Handler

    /// <summary>Handles the command.</summary>
    public sealed class UseCaseHandler(
        IEventSourcedRepository<Payment, Guid> repository,
        IPaymentGatewayRegistry registry,
        IPaymentPolicy policy,
        TimeProvider clock) : ICommandHandler<UseCase, PaymentResponseModel>
    {
        /// <inheritdoc />
        public async Task<Result<PaymentResponseModel>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            Result<Payment> loaded = await repository.LoadAsync(command.PaymentId, cancellationToken);

            if (loaded.IsFailure)
                return Result<PaymentResponseModel>.Fail(loaded.Errors);

            Payment payment = loaded.Value;

            // Handle: an overdue intent is expired rather than cancelled.
            if (payment.IsExpired(clock.GetUtcNow(), policy.ExpiryGrace))
            {
                Result<Payment> expiring = payment.Expire(clock.GetUtcNow());

                if (expiring.IsFailure)
                    return Result<PaymentResponseModel>.Fail(expiring.Errors);

                Result expired = await repository.SaveAsync(expiring.Value, cancellationToken);

                return expired.IsFailure
                    ? Result<PaymentResponseModel>.Fail(expired.Errors)
                    : Result<PaymentResponseModel>.Success(expiring.Value.ToResponse());
            }

            Result<IPaymentGateway> resolved = registry.Resolve(payment.Provider);

            if (resolved.IsFailure)
                return Result<PaymentResponseModel>.Fail(resolved.Errors);

            if (payment.ProviderReference is not null)
            {
                Result<PaymentCancelResult> cancelled = await PaymentMetrics.TrackAsync(
                    payment.Provider,
                    PaymentConstant.Operations.Cancel,
                    () => resolved.Value.CancelAsync(
                        new PaymentCancelRequest(
                            payment.ProviderReference,
                            $"{PaymentConstant.IdempotencyPrefixes.Cancel}{payment.Id:N}"),
                        cancellationToken));

                if (cancelled.IsFailure)
                    return Result<PaymentResponseModel>.Fail(cancelled.Errors);
            }

            Result<Payment> transition = payment.Cancel(
                PaymentConstant.Reasons.CustomerCancelled, clock.GetUtcNow());

            if (transition.IsFailure)
                return Result<PaymentResponseModel>.Fail(transition.Errors);

            Result saved = await repository.SaveAsync(transition.Value, cancellationToken);

            return saved.IsFailure
                ? Result<PaymentResponseModel>.Fail(saved.Errors)
                : Result<PaymentResponseModel>.Success(transition.Value.ToResponse());
        }
    }

    #endregion
}
