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

            Result<Payment> cancelled = await repository
                .LoadAsync(command.PaymentId, cancellationToken)
                .BindAsync((payment, ct) => CancelAsync(payment, ct), cancellationToken);

            return cancelled.Map(payment => payment.ToResponse());
        }

        /// <summary>Expires an overdue intent, otherwise voids it at the provider and locally.</summary>
        private async Task<Result<Payment>> CancelAsync(Payment payment, CancellationToken cancellationToken)
        {
            // Handle: an overdue intent is expired rather than cancelled.
            if (payment.IsExpired(clock.GetUtcNow(), policy.ProviderExpiryGracePeriod))
                return await payment
                    .Expire(clock.GetUtcNow())
                    .BindAsync((expired, ct) => SaveAsync(expired, ct), cancellationToken);

            return await registry
                .Resolve(payment.Provider)
                .BindAsync((gateway, ct) => VoidAtProviderAsync(payment, gateway, ct), cancellationToken)
                .BindAsync(
                    (current, _) => Task.FromResult(current.Cancel(
                        PaymentConstant.Reasons.CustomerCancelled,
                        clock.GetUtcNow())),
                    cancellationToken)
                .BindAsync((cancelled, ct) => SaveAsync(cancelled, ct), cancellationToken);
        }

        /// <summary>Voids the provider authorization when one exists.</summary>
        private async Task<Result<Payment>> VoidAtProviderAsync(
            Payment payment,
            IPaymentGateway gateway,
            CancellationToken cancellationToken)
        {
            if (payment.ProviderReference is null)
                return Result<Payment>.Success(payment);

            Result<PaymentCancelResult> cancelled = await PaymentMetrics.TrackAsync(
                payment.Provider,
                PaymentConstant.Operations.Cancel,
                () => gateway.CancelAsync(
                    new PaymentCancelRequest(
                        payment.ProviderReference,
                        $"{PaymentConstant.IdempotencyPrefixes.Cancel}{payment.Id:N}"),
                    cancellationToken));

            return cancelled.Map(_ => payment);
        }

        /// <summary>Persists the aggregate and returns it.</summary>
        private async Task<Result<Payment>> SaveAsync(Payment payment, CancellationToken cancellationToken)
        {
            Result saved = await repository.SaveAsync(payment, cancellationToken);

            return saved.Map(() => payment);
        }
    }

    #endregion
}
