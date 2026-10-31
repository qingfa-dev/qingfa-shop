using System.Text;

using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Intents;
using Application.Modules.Payments.Intents.Commands;
using Application.Modules.Payments.Intents.Models;
using Application.Modules.Payments.Persistence;
using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.BuildingBlocks.Functional.Maybes;
using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using MediatR;

namespace Application.Modules.Payments.Webhooks.Commands;

/// <summary>Verifies, deduplicates, and applies a provider webhook.</summary>
public static class ProcessPaymentWebhook
{
    #region UseCase

    /// <summary>The command carrying the raw delivery.</summary>
    /// <param name="Provider">The provider the delivery came from.</param>
    /// <param name="RawBody">The exact request bytes; signatures are verified over them.</param>
    /// <param name="Headers">The request headers, needed for signature verification.</param>
    public sealed record UseCase(
        PaymentProvider Provider,
        ReadOnlyMemory<byte> RawBody,
        IReadOnlyDictionary<string, string> Headers) : ICommand<bool>, INonTransactionalCommand;

    #endregion


    #region Handler

    /// <summary>Handles the command.</summary>
    public sealed class UseCaseHandler(
        IPaymentGatewayRegistry registry,
        IWebhookEventStore webhookStore,
        IPaymentReferenceStore references,
        IEventSourcedRepository<Payment, Guid> repository,
        IMediator mediator) : ICommandHandler<UseCase, bool>
    {
        /// <inheritdoc />
        public async Task<Result<bool>> Handle(UseCase command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (command.RawBody.IsEmpty)
                return Result<bool>.Fail(PaymentsOutcome.Failure.WebhookPayloadInvalid());

            Result<IPaymentGateway> resolved = registry.Resolve(command.Provider);

            if (resolved.IsFailure)
                return Result<bool>.Fail(resolved.Errors);

            Result<Maybe<PaymentProviderEvent>> parsed = await PaymentMetrics.TrackAsync(
                command.Provider,
                PaymentConstant.Operations.Webhook,
                () => resolved.Value.ParseWebhookAsync(
                    command.RawBody, command.Headers, cancellationToken));

            if (parsed.IsFailure)
                return Result<bool>.Fail(parsed.Errors);

            // Handle: an unsupported event type is acknowledged, never retried.
            if (parsed.Value.HasNoValue)
                return Result<bool>.Success(false);

            PaymentMetrics.WebhooksReceived.Add(1);

            PaymentProviderEvent providerEvent = parsed.Value.Value;
            string providerName = command.Provider.ToWire();

            Result<bool> recorded = await webhookStore.TryRecordAsync(
                providerName,
                providerEvent.EventId,
                providerEvent.Type.ToString(),
                Encoding.UTF8.GetString(command.RawBody.Span),
                cancellationToken);

            if (recorded.IsFailure)
                return Result<bool>.Fail(recorded.Errors);

            if (!recorded.Value)
            {
                PaymentMetrics.WebhooksDuplicate.Add(1);
                return Result<bool>.Success(false);
            }

            // Handle: a mismatched amount is audited, acknowledged, and never settled;
            // retrying the provider would not change the payload.
            if (await IsAmountMismatchAsync(command.Provider, providerEvent, cancellationToken))
            {
                PaymentMetrics.WebhooksMismatch.Add(1);

                await webhookStore.MarkProcessedAsync(
                    providerName,
                    providerEvent.EventId,
                    PaymentConstant.Outcomes.AmountMismatch,
                    cancellationToken);

                return Result<bool>.Success(false);
            }

            Result<PaymentResponseModel> settled = await mediator.Send(
                new SettlePayment.UseCase(
                    command.Provider,
                    providerEvent.ProviderReference,
                    providerEvent.Type,
                    providerEvent.Amount,
                    null,
                    providerEvent.EventId),
                cancellationToken);

            await webhookStore.MarkProcessedAsync(
                providerName,
                providerEvent.EventId,
                settled.IsFailure ? PaymentConstant.Outcomes.Failed : PaymentConstant.Outcomes.Settled,
                cancellationToken);

            return settled.IsFailure
                ? Result<bool>.Fail(settled.Errors)
                : Result<bool>.Success(true);
        }

        /// <summary>Checks the observed amount against the payment it targets.</summary>
        private async Task<bool> IsAmountMismatchAsync(
            PaymentProvider provider,
            PaymentProviderEvent providerEvent,
            CancellationToken cancellationToken)
        {
            if (providerEvent.Amount is not { } observed)
                return false;

            Result<Guid> reference = await references.FindAsync(
                provider, providerEvent.ProviderReference, cancellationToken);

            if (reference.IsFailure)
                return false;

            Result<Payment> loaded = await repository.LoadAsync(reference.Value, cancellationToken);

            if (loaded.IsFailure)
                return false;

            Payment payment = loaded.Value;

            return observed.Currency != payment.Amount.Currency
                || observed.Amount > payment.Amount.Amount;
        }
    }

    #endregion
}
