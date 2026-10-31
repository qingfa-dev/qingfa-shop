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


    #region Handler

    /// <summary>Handles the command.</summary>
    public sealed class UseCaseHandler(
        IEventSourcedRepository<Payment, Guid> repository,
        IPaymentIdempotencyStore idempotency,
        IPaymentReferenceStore references,
        IPaymentGatewayRegistry registry,
        IPaymentPolicy policy,
        IUserContext userContext,
        TimeProvider clock) : ICommandHandler<UseCase, PaymentResponseModel>
    {
        /// <inheritdoc />
        public async Task<Result<PaymentResponseModel>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            Result<State> created = await Result<State>.Success(new State())
                .BindAsync((state, _) => Task.FromResult(Resolve(command, state)), cancellationToken)
                .BindAsync((state, ct) => ReserveAsync(state, command, ct), cancellationToken)
                .BindAsync((state, ct) => state.Reservation.IsNew
                    ? CreateAsync(state, command, ct)
                    : ReplayAsync(state, ct), cancellationToken)
                .BindAsync((state, ct) => PersistAsync(state, command, ct), cancellationToken);

            if (created.IsFailure)
                return created.Map(_ => new PaymentResponseModel());

            Result<PaymentResponseModel> response = Map(created.Value);

            return created.Value.WasCreated
                ? response
                : response.WithMetadata(PaymentConstant.Metadata.IdempotencyReplay, true);
        }

        /// <summary>Resolves the customer, the currency ceiling, and the request fingerprint.</summary>
        private Result<State> Resolve(UseCase command, State state)
        {
            if (!Guid.TryParse(userContext.UserId, out Guid customerId))
                return Result<State>.Fail(PaymentsOutcome.Failure.CustomerRequired());

            string currencyCode = command.Request.Amount.Currency.Code ?? string.Empty;

            if (!policy.MaximumAmountsByCurrency.TryGetValue(currencyCode, out decimal maximum))
                return Result<State>.Fail(PaymentsOutcome.Failure.CurrencyUnsupported(currencyCode));

            // Validate: the configured per-currency ceiling applies before any provider call.
            if (command.Request.Amount.Amount > maximum)
                return Result<State>.Fail(
                    PaymentsOutcome.Failure.AmountExceedsLimit(command.Request.Amount.Amount, maximum));

            return Result<State>.Success(state with
            {
                CustomerId = customerId,
                Maximum = maximum,
                Fingerprint = Fingerprint(command.Request)
            });
        }

        /// <summary>Reserves the idempotency key synchronously.</summary>
        private async Task<Result<State>> ReserveAsync(
            State state,
            UseCase command,
            CancellationToken cancellationToken)
        {
            Result<IdempotencyReservation> reservation = await idempotency.ReserveAsync(
                state.CustomerId, command.IdempotencyKey, state.Fingerprint, cancellationToken);

            return reservation.Map(reserved => state with { Reservation = reserved });
        }

        /// <summary>Returns the stored payment for a replay.</summary>
        private async Task<Result<State>> ReplayAsync(State state, CancellationToken cancellationToken)
        {
            if (!state.Reservation.FingerprintMatches)
                return Result<State>.Fail(PaymentsOutcome.Failure.IdempotencyConflict());

            Result<Payment> replay = await repository.LoadAsync(
                state.Reservation.PaymentId, cancellationToken);

            return replay.Map(payment => state with { Payment = payment, WasCreated = false });
        }

        /// <summary>Creates the provider intent and the aggregate that records it.</summary>
        private async Task<Result<Payment>> CreateWithGatewayAsync(
            IPaymentGateway gateway,
            State state,
            UseCase command,
            CancellationToken cancellationToken)
        {
            Result<PaymentIntentResult> intent = await PaymentMetrics.TrackAsync(
                gateway.Provider,
                PaymentConstant.Operations.Create,
                () => gateway.CreateIntentAsync(
                    new PaymentIntentRequest(
                        state.Reservation.PaymentId,
                        command.Request.Amount,
                        gateway.Provider,
                        state.CustomerId,
                        command.Request.ExternalReference,
                        command.Request.ReturnUrl,
                        command.Request.CancelUrl,
                        command.Request.CaptureMode,
                        $"{PaymentConstant.IdempotencyPrefixes.Create}{command.IdempotencyKey}",
                        new Dictionary<string, string>()),
                    cancellationToken));

            return await intent
                .BindAsync(
                    (value, _) => Task.FromResult(Initiate(gateway, state, command, value)),
                    cancellationToken);
        }

        /// <summary>Records the initiation event, failing the payment when the provider declined.</summary>
        private Result<Payment> Initiate(
            IPaymentGateway gateway,
            State state,
            UseCase command,
            PaymentIntentResult value)
            => PaymentFactory
                .Initiate(
                    id: state.Reservation.PaymentId,
                    provider: gateway.Provider,
                    providerReference: value.ProviderReference,
                    amount: command.Request.Amount,
                    captureMode: command.Request.CaptureMode,
                    externalReference: command.Request.ExternalReference,
                    idempotencyKey: command.IdempotencyKey,
                    idempotencyFingerprint: state.Fingerprint,
                    customerId: state.CustomerId,
                    approvalUrl: value.ApprovalUrl,
                    // Fallback: providers that do not state an expiry use the configured lifetime.
                    expiresAt: value.ExpiresAt ?? clock.GetUtcNow().Add(policy.HostedCheckoutIntentLifetime),
                    nowUtc: clock.GetUtcNow(),
                    maxAmount: state.Maximum)
                .Bind(payment => value.Status is PaymentProviderIntentStatus.Failed
                    ? payment.Fail(
                        value.FailureCode ?? PaymentConstant.Reasons.ProviderFailed,
                        null,
                        clock.GetUtcNow())
                    : Result<Payment>.Success(payment));

        /// <summary>Creates the aggregate for a new payment.</summary>
        private async Task<Result<State>> CreateAsync(
            State state,
            UseCase command,
            CancellationToken cancellationToken)
        {
            Result<Payment> created = await registry
                .Resolve(command.Request.Provider)
                .BindAsync(
                    (gateway, ct) => CreateWithGatewayAsync(gateway, state, command, ct),
                    cancellationToken);

            if (created.IsFailure)
            {
                // Fallback: release the reservation so a retry can create the payment.
                await Release(state, command, cancellationToken);
                return Result<State>.Fail(created.Errors);
            }

            return created.Map(payment => state with { Payment = payment, WasCreated = true });
        }

        /// <summary>Persists a newly created payment and records its provider reference.</summary>
        private async Task<Result<State>> PersistAsync(
            State state,
            UseCase command,
            CancellationToken cancellationToken)
        {
            if (!state.WasCreated)
                return Result<State>.Success(state);

            Result saved = await repository.SaveAsync(state.Payment!, cancellationToken);

            if (saved.IsSuccess)
            {
                await RecordReference(state.Payment!, cancellationToken);
                return Result<State>.Success(state);
            }

            // Fallback: release the reservation so a retry can create the payment.
            await Release(state, command, cancellationToken);

            return Result<State>.Fail(saved.Errors);
        }

        /// <summary>Records the provider reference so callbacks resolve before the projection catches up.</summary>
        private Task RecordReference(Payment payment, CancellationToken cancellationToken)
            => payment.ProviderReference is null
                ? Task.CompletedTask
                : references.RecordAsync(
                    payment.Provider,
                    payment.ProviderReference,
                    payment.Id,
                    cancellationToken);

        /// <summary>Releases the reservation so a retry can create the payment.</summary>
        private Task Release(State state, UseCase command, CancellationToken cancellationToken)
            => idempotency.ReleaseAsync(
                state.CustomerId,
                command.IdempotencyKey,
                state.Reservation.PaymentId,
                cancellationToken);

        /// <summary>Maps the stored payment to the response.</summary>
        private static Result<PaymentResponseModel> Map(State state)
        {
            Payment payment = state.Payment
                ?? throw new InvalidOperationException("The payment was not created or replayed.");

            Result<PaymentResponseModel> response = Result<PaymentResponseModel>.Success(payment.ToResponse());

            if (!state.WasCreated)
                return response.WithMetadata(PaymentConstant.Metadata.IdempotencyReplay, true);

            PaymentMetrics.Initiated.Add(
                1,
                new KeyValuePair<string, object?>(PaymentMetrics.ProviderTag, payment.Provider.ToWire()));

            return response.WithStatus(ResultConstant.StatusCode.Created);
        }

        /// <summary>The request context threaded through the creation flow.</summary>
        private sealed record State
        {
            public Guid CustomerId { get; init; }

            public decimal Maximum { get; init; }

            public string Fingerprint { get; init; } = string.Empty;

            public IdempotencyReservation Reservation { get; init; } = null!;

            public Payment? Payment { get; init; }

            public bool WasCreated { get; init; }
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
