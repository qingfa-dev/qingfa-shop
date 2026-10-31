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

            return await registry.Resolve(command.Provider)
                .BindAsync((gateway, ct) => ParseAsync(gateway, command, ct), cancellationToken)
                .BindAsync((parsed, ct) => AcceptAsync(parsed, command, ct), cancellationToken);
        }

        /// <summary>Verifies the signature and normalizes the delivery.</summary>
        private static async Task<Result<Maybe<PaymentProviderEvent>>> ParseAsync(
            IPaymentGateway gateway,
            UseCase command,
            CancellationToken cancellationToken)
            => await PaymentMetrics.TrackAsync(
                command.Provider,
                PaymentConstant.Operations.Webhook,
                () => gateway.ParseWebhookAsync(command.RawBody, command.Headers, cancellationToken));

        /// <summary>Records, deduplicates, and settles an accepted delivery.</summary>
        private async Task<Result<bool>> AcceptAsync(
            Maybe<PaymentProviderEvent> parsed,
            UseCase command,
            CancellationToken cancellationToken)
        {
            // Handle: an unsupported event type is acknowledged, never retried.
            if (parsed.HasNoValue)
                return Result<bool>.Success(false);

            PaymentMetrics.WebhooksReceived.Add(1);

            PaymentProviderEvent providerEvent = parsed.Value;
            string providerName = command.Provider.ToWire();

            return await webhookStore
                .TryRecordAsync(
                    providerName,
                    providerEvent.EventId,
                    providerEvent.Type.ToString(),
                    Encoding.UTF8.GetString(command.RawBody.Span),
                    cancellationToken)
                .BindAsync((recorded, ct) => recorded
                    ? SettleAsync(providerEvent, command, providerName, ct)
                    : Task.FromResult(Duplicate()), cancellationToken);
        }

        /// <summary>Settles an accepted delivery, auditing an amount mismatch first.</summary>
        private async Task<Result<bool>> SettleAsync(
            PaymentProviderEvent providerEvent,
            UseCase command,
            string providerName,
            CancellationToken cancellationToken)
        {
            // Handle: a mismatched amount is audited, acknowledged, and never settled;
            // retrying the provider would not change the payload.
            if (await IsAmountMismatchAsync(command.Provider, providerEvent, cancellationToken))
            {
                PaymentMetrics.WebhooksMismatch.Add(1);

                await webhookStore.MarkProcessedAsync(
                    providerName,
                    providerEvent.EventId,
                    PaymentsOutcome.Codes.WebhookAmountMismatch,
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

            return settled.Map(_ => true);
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

        /// <summary>Reports a deduplicated delivery.</summary>
        private static Result<bool> Duplicate()
        {
            PaymentMetrics.WebhooksDuplicate.Add(1);
            return Result<bool>.Success(false);
        }
    }

    #endregion
}
