using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using FluentValidation;

namespace Application.Modules.Payments.Intents.Commands;

/// <summary>Voids an uncaptured payment.</summary>
public static class CancelPayment
{
    #region UseCase

    /// <summary>The command.</summary>
    /// <param name="PaymentId">The payment to void.</param>
    public sealed record UseCase(Guid PaymentId) : ICommand<PaymentResponseModel>;

    #endregion

    #region Validator

    /// <summary>Validates the command.</summary>
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        /// <summary>Creates the validator.</summary>
        public UseCaseValidator() => RuleFor(command => command.PaymentId).NotEmpty();
    }

    #endregion

    #region Handler

    /// <summary>Handles the command.</summary>
    public sealed class UseCaseHandler(
        IEventSourcedRepository<Payment, Guid> repository,
        IPaymentGatewayRegistry registry,
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

            Result<IPaymentGateway> resolved = registry.Resolve(payment.Provider);

            if (resolved.IsFailure)
                return Result<PaymentResponseModel>.Fail(resolved.Errors);

            if (payment.ProviderReference is not null)
            {
                Result<PaymentCancelResult> cancelled = await resolved.Value.CancelAsync(
                    new PaymentCancelRequest(payment.ProviderReference, $"cancel:{payment.Id:N}"),
                    cancellationToken);

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
