using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using FluentValidation;

using MediatR;

namespace Application.Modules.Payments.Intents.Commands;

/// <summary>Refreshes a payment after the customer returns from the provider.</summary>
public static class ConfirmPayment
{
    #region UseCase

    /// <summary>The command.</summary>
    /// <param name="PaymentId">The payment to refresh.</param>
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
        IPaymentReadStore store,
        IPaymentGatewayRegistry registry,
        IMediator mediator) : ICommandHandler<UseCase, PaymentResponseModel>
    {
        /// <inheritdoc />
        public async Task<Result<PaymentResponseModel>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            PaymentReadModel? row = await store.FindAsync(command.PaymentId, cancellationToken);

            if (row is null)
                return Result<PaymentResponseModel>.Fail(PaymentsOutcome.Failure.NotFound(command.PaymentId));

            if (row.ProviderReference is null)
                return Result<PaymentResponseModel>.Fail(PaymentsOutcome.Failure.InvalidState(
                    PaymentStatusExtension.FromWire(row.Status), "confirm"));

            Result<IPaymentGateway> resolved = registry.Resolve(PaymentProviderExtension.FromWire(row.Provider));

            if (resolved.IsFailure)
                return Result<PaymentResponseModel>.Fail(resolved.Errors);

            Result<PaymentIntentResult> intent = await resolved.Value.RetrieveAsync(
                row.ProviderReference, cancellationToken);

            if (intent.IsFailure)
                return Result<PaymentResponseModel>.Fail(intent.Errors);

            PaymentProviderEventType observed = intent.Value.Status switch
            {
                PaymentProviderIntentStatus.Captured => PaymentProviderEventType.Captured,
                PaymentProviderIntentStatus.Authorized => PaymentProviderEventType.Authorized,
                PaymentProviderIntentStatus.Cancelled => PaymentProviderEventType.Cancelled,
                _ => PaymentProviderEventType.Failed
            };

            return await mediator.Send(
                new SettlePayment.UseCase(
                    PaymentProviderExtension.FromWire(row.Provider),
                    row.ProviderReference,
                    observed,
                    null,
                    intent.Value.FailureCode,
                    $"confirm:{row.Id:N}"),
                cancellationToken);
        }
    }

    #endregion
}
