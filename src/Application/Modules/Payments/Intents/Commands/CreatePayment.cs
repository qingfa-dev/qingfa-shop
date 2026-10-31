using System.Security.Cryptography;
using System.Text;

using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using FluentValidation;

namespace Application.Modules.Payments.Intents.Commands;

/// <summary>Creates a payment intent and the provider-side intent.</summary>
public static class CreatePayment
{
    #region Request

    /// <summary>The request body.</summary>
    /// <param name="Amount">The amount to charge.</param>
    /// <param name="Provider">The provider to charge through.</param>
    /// <param name="ExternalReference">The caller's cart or order key.</param>
    /// <param name="CaptureMode">Automatic or manual capture.</param>
    /// <param name="ReturnUrl">Where the customer returns after paying.</param>
    /// <param name="CancelUrl">Where the customer returns after cancelling.</param>
    public sealed record Request(
        Money Amount,
        PaymentProvider Provider,
        string ExternalReference,
        PaymentCaptureMode CaptureMode,
        string? ReturnUrl,
        string? CancelUrl);

    #endregion

    #region UseCase

    /// <summary>The command.</summary>
    /// <param name="Request">The request body.</param>
    /// <param name="IdempotencyKey">The caller's idempotency key.</param>
    public sealed record UseCase(Request Request, string IdempotencyKey) : ICommand<PaymentResponseModel>;

    #endregion

    #region Validator

    /// <summary>Validates the command.</summary>
    public sealed class UseCaseValidator : AbstractValidator<UseCase>
    {
        /// <summary>Creates the validator.</summary>
        public UseCaseValidator()
        {
            RuleFor(command => command.IdempotencyKey)
                .MaximumLength(PaymentConstant.Constraints.IdempotencyKeyMaxLength)
                .WithErrorCode(PaymentsOutcome.Codes.IdempotencyKeyTooLong);

            RuleFor(command => command.Request.ExternalReference)
                .NotEmpty()
                .WithErrorCode(PaymentsOutcome.Codes.ReferenceRequired)
                .MaximumLength(PaymentConstant.Constraints.ReferenceMaxLength)
                .WithErrorCode(PaymentsOutcome.Codes.ReferenceTooLong);

            RuleFor(command => command.Request.Amount.Amount)
                .GreaterThan(0)
                .WithErrorCode(PaymentsOutcome.Codes.AmountInvalid);
        }
    }

    #endregion

    #region Handler

    /// <summary>Handles the command.</summary>
    public sealed class UseCaseHandler(
        IEventSourcedRepository<Payment, Guid> repository,
        IPaymentIdempotencyStore idempotency,
        IPaymentReferenceStore references,
        IPaymentGatewayRegistry registry,
        IUserContext userContext,
        TimeProvider clock) : ICommandHandler<UseCase, PaymentResponseModel>
    {
        /// <inheritdoc />
        public async Task<Result<PaymentResponseModel>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (!Guid.TryParse(userContext.UserId, out Guid customerId))
                return Result<PaymentResponseModel>.Fail(PaymentsOutcome.Failure.CustomerRequired());

            string fingerprint = Fingerprint(command.Request);

            // Enforce: reserve the key synchronously so a replay never creates a second
            // provider intent while the projection is still catching up.
            Result<IdempotencyReservation> reservation = await idempotency.ReserveAsync(
                customerId, command.IdempotencyKey, fingerprint, cancellationToken);

            if (reservation.IsFailure)
                return Result<PaymentResponseModel>.Fail(reservation.Errors);

            if (!reservation.Value.IsNew)
            {
                if (!reservation.Value.FingerprintMatches)
                    return Result<PaymentResponseModel>.Fail(PaymentsOutcome.Failure.IdempotencyConflict());

                Result<Payment> replay = await repository.LoadAsync(reservation.Value.PaymentId, cancellationToken);

                return replay.IsFailure
                    ? Result<PaymentResponseModel>.Fail(replay.Errors)
                    : Result<PaymentResponseModel>.Success(replay.Value.ToResponse())
                        .WithMetadata("IdempotentReplay", true);
            }

            Result<Payment> created = await registry.Resolve(command.Request.Provider)
                .BindAsync(
                    (gateway, ct) => CreateWithGatewayAsync(
                        gateway,
                        command.Request,
                        customerId,
                        command.IdempotencyKey,
                        fingerprint,
                        reservation.Value.PaymentId,
                        ct),
                    cancellationToken);

            if (created.IsFailure)
            {
                // Fallback: release the reservation so a retry can create the payment.
                await idempotency.ReleaseAsync(
                    customerId, command.IdempotencyKey, reservation.Value.PaymentId, cancellationToken);

                return Result<PaymentResponseModel>.Fail(created.Errors);
            }

            Result saved = await repository.SaveAsync(created.Value, cancellationToken);

            if (saved.IsFailure)
            {
                await idempotency.ReleaseAsync(
                    customerId, command.IdempotencyKey, reservation.Value.PaymentId, cancellationToken);

                return Result<PaymentResponseModel>.Fail(saved.Errors);
            }

            // Enforce: provider callbacks must resolve before the projection catches up.
            if (created.Value.ProviderReference is not null)
            {
                await references.RecordAsync(
                    created.Value.Provider,
                    created.Value.ProviderReference,
                    created.Value.Id,
                    cancellationToken);
            }

            PaymentMetrics.Initiated.Add(
                1,
                new KeyValuePair<string, object?>(PaymentMetrics.ProviderTag, created.Value.Provider.ToWire()));

            return Result<PaymentResponseModel>.Success(created.Value.ToResponse())
                .WithStatus(ResultConstant.StatusCode.Created);
        }

        /// <summary>Creates the provider intent and the aggregate that records it.</summary>
        private async Task<Result<Payment>> CreateWithGatewayAsync(
            IPaymentGateway gateway,
            Request request,
            Guid customerId,
            string idempotencyKey,
            string fingerprint,
            Guid id,
            CancellationToken cancellationToken)
        {
            Result<PaymentIntentResult> intent = await gateway.CreateIntentAsync(
                new PaymentIntentRequest(
                    id,
                    request.Amount,
                    gateway.Provider,
                    customerId,
                    request.ExternalReference,
                    request.ReturnUrl,
                    request.CancelUrl,
                    request.CaptureMode,
                    idempotencyKey,
                    new Dictionary<string, string>()),
                cancellationToken);

            if (intent.IsFailure)
                return Result<Payment>.Fail(intent.Errors);

            PaymentIntentResult value = intent.Value;

            return PaymentFactory.Initiate(
                    id: id,
                    provider: gateway.Provider,
                    providerReference: value.ProviderReference,
                    amount: request.Amount,
                    captureMode: request.CaptureMode,
                    externalReference: request.ExternalReference,
                    idempotencyKey: idempotencyKey,
                    idempotencyFingerprint: fingerprint,
                    customerId: customerId,
                    approvalUrl: value.ApprovalUrl,
                    expiresAt: value.ExpiresAt,
                    nowUtc: clock.GetUtcNow())
                .Bind(payment => value.Status is PaymentProviderIntentStatus.Failed
                    ? payment.Fail(value.FailureCode ?? PaymentConstant.Reasons.ProviderFailed, null, clock.GetUtcNow())
                    : Result<Payment>.Success(payment));
        }

        /// <summary>Computes the stable request fingerprint used to detect key reuse.</summary>
        private static string Fingerprint(Request request)
        {
            string raw = string.Join(
                '|',
                request.Provider,
                request.Amount.Amount.ToString("0.####"),
                request.Amount.Currency.Code,
                request.ExternalReference,
                request.CaptureMode,
                request.ReturnUrl,
                request.CancelUrl);

            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        }
    }

    #endregion
}
