using System.Globalization;
using System.Text;

using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.BuildingBlocks.Functional.Maybes;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.Modules.Payments.Adapters.Shared;

using Stripe;

namespace Infrastructure.Modules.Payments.Adapters.Stripe;

/// <summary>Stripe adapter built on Stripe.net.</summary>
public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;
    private readonly IStripeClient _client;

    /// <summary>Creates the adapter from configuration.</summary>
    /// <param name="options">The Stripe credentials.</param>
    public StripePaymentGateway(StripeOptions options)
        : this(options, new StripeClient(options.SecretKey))
    {
    }

    /// <summary>Creates the adapter with an explicit client; used by tests.</summary>
    /// <param name="options">The Stripe credentials.</param>
    /// <param name="client">The Stripe client.</param>
    public StripePaymentGateway(StripeOptions options, IStripeClient client)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(client);

        _options = options;
        _client = client;
    }

    /// <inheritdoc />
    public PaymentProvider Provider => PaymentProvider.Stripe;

    /// <inheritdoc />
    public PaymentGatewayCapabilities Capabilities { get; } = PaymentGatewayCapabilities.Full;

    /// <inheritdoc />
    public async Task<Result<PaymentIntentResult>> CreateIntentAsync(
        PaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<long> minor = MoneyConverter.ToMinorUnits(request.Amount);

        if (minor.IsFailure)
            return Result<PaymentIntentResult>.Fail(minor.Errors);

        PaymentIntentService service = NewService();

        try
        {
            PaymentIntent intent = await service.CreateAsync(
                new PaymentIntentCreateOptions
                {
                    Amount = minor.Value,
                    Currency = request.Amount.Currency.Code!.ToLowerInvariant(),
                    CaptureMethod = request.CaptureMode is PaymentCaptureMode.Manual
                        ? StripeConstant.CaptureMethod.Manual
                        : StripeConstant.CaptureMethod.Automatic,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                    Metadata = new Dictionary<string, string>(request.Metadata)
                    {
                        [StripeConstant.Metadata.PaymentId] = request.PaymentId.ToString("N"),
                        [StripeConstant.Metadata.ExternalReference] = request.ExternalReference
                    }
                },
                new RequestOptions { IdempotencyKey = request.IdempotencyKey },
                cancellationToken);

            return Result<PaymentIntentResult>.Success(Map(intent));
        }
        catch (StripeException exception)
        {
            return Result<PaymentIntentResult>.Fail(ToFailure(exception));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaymentIntentResult>> RetrieveAsync(
        string providerReference,
        CancellationToken cancellationToken)
    {
        PaymentIntentService service = NewService();
        PaymentIntent intent = await service.GetAsync(providerReference, cancellationToken: cancellationToken);

        return Result<PaymentIntentResult>.Success(Map(intent));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentCaptureResult>> CaptureAsync(
        PaymentCaptureRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<long> minor = MoneyConverter.ToMinorUnits(request.Amount);

        if (minor.IsFailure)
            return Result<PaymentCaptureResult>.Fail(minor.Errors);

        PaymentIntentService service = NewService();

        try
        {
            PaymentIntent intent = await service.CaptureAsync(
                request.ProviderReference,
                new PaymentIntentCaptureOptions { AmountToCapture = minor.Value },
                new RequestOptions { IdempotencyKey = request.IdempotencyKey },
                cancellationToken);

            return Result<PaymentCaptureResult>.Success(
                new PaymentCaptureResult(intent.Id, request.Amount, true, null));
        }
        catch (StripeException exception)
        {
            return Result<PaymentCaptureResult>.Fail(ToFailure(exception));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaymentCancelResult>> CancelAsync(
        PaymentCancelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        PaymentIntentService service = NewService();

        try
        {
            await service.CancelAsync(
                request.ProviderReference,
                new PaymentIntentCancelOptions(),
                new RequestOptions { IdempotencyKey = request.IdempotencyKey },
                cancellationToken);

            return Result<PaymentCancelResult>.Success(new PaymentCancelResult(true, null));
        }
        catch (StripeException exception)
        {
            return Result<PaymentCancelResult>.Fail(ToFailure(exception));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaymentRefundResult>> RefundAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<long> minor = MoneyConverter.ToMinorUnits(request.Amount);

        if (minor.IsFailure)
            return Result<PaymentRefundResult>.Fail(minor.Errors);

        var service = new RefundService(_client);

        try
        {
            Refund refund = await service.CreateAsync(
                new RefundCreateOptions { PaymentIntent = request.ProviderReference, Amount = minor.Value },
                new RequestOptions { IdempotencyKey = request.IdempotencyKey },
                cancellationToken);

            return Result<PaymentRefundResult>.Success(
                new PaymentRefundResult(refund.Id, request.Amount, true, null));
        }
        catch (StripeException exception)
        {
            return Result<PaymentRefundResult>.Fail(ToFailure(exception));
        }
    }

    /// <inheritdoc />
    public Task<Result<Maybe<PaymentProviderEvent>>> ParseWebhookAsync(
        ReadOnlyMemory<byte> rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(headers);

        // Handle: Stripe signs the exact payload; the library verifies the decoded JSON text.
        string payload = Encoding.UTF8.GetString(rawBody.Span);

        if (!headers.TryGetValue(PaymentConstant.Headers.StripeSignature, out string? signature))
            return Task.FromResult(Result<Maybe<PaymentProviderEvent>>.Fail(
                PaymentsOutcome.Failure.WebhookSignatureInvalid()));

        try
        {
            Event stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _options.WebhookSecret,
                _options.WebhookToleranceSeconds,
                throwOnApiVersionMismatch: false);

            PaymentProviderEventType? type = stripeEvent.Type switch
            {
                StripeConstant.EventType.AmountCapturable => PaymentProviderEventType.Authorized,
                StripeConstant.EventType.Succeeded => PaymentProviderEventType.Captured,
                StripeConstant.EventType.PaymentFailed => PaymentProviderEventType.Failed,
                StripeConstant.EventType.Canceled => PaymentProviderEventType.Cancelled,
                StripeConstant.EventType.ChargeRefunded => PaymentProviderEventType.Refunded,
                StripeConstant.EventType.ChargeRefundUpdated => PaymentProviderEventType.RefundFailed,
                _ => null
            };

            if (type is null)
                return Task.FromResult(Result<Maybe<PaymentProviderEvent>>.Success(
                    Maybe<PaymentProviderEvent>.Empty()));

            if (stripeEvent.Data.Object is Dispute dispute)
            {
                return Task.FromResult(Result<Maybe<PaymentProviderEvent>>.Success(
                    Maybe<PaymentProviderEvent>.From(new PaymentProviderEvent(
                        stripeEvent.Id,
                        type.Value,
                        ReadDisputeIntent(dispute),
                        null,
                        dispute.Amount > 0
                            ? MoneyConverter.FromMinorUnits(dispute.Amount, new Currency(dispute.Currency))
                            : null,
                        stripeEvent.Created,
                        DisputeReference: dispute.Id,
                        DisputeReason: dispute.Reason))));
            }

            var intent = stripeEvent.Data.Object as PaymentIntent;

            return Task.FromResult(Result<Maybe<PaymentProviderEvent>>.Success(
                Maybe<PaymentProviderEvent>.From(new PaymentProviderEvent(
                    stripeEvent.Id,
                    type.Value,
                    intent?.Id ?? string.Empty,
                    null,
                    intent is null
                        ? null
                        : MoneyConverter.FromMinorUnits(intent.Amount, new Currency(intent.Currency)),
                    stripeEvent.Created))));
        }
        catch (StripeException)
        {
            return Task.FromResult(Result<Maybe<PaymentProviderEvent>>.Fail(
                PaymentsOutcome.Failure.WebhookSignatureInvalid()));
        }
    }

    /// <summary>Maps a Stripe failure onto the canonical outcome.</summary>
    private static Error ToFailure(StripeException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // Boundary: rejected credentials are a configuration fault, not a transient provider error.
        return exception.HttpStatusCode is System.Net.HttpStatusCode.Unauthorized
            or System.Net.HttpStatusCode.Forbidden
            ? PaymentsOutcome.Failure.ProviderConfigurationInvalid(PaymentProvider.Stripe)
            : PaymentsOutcome.Failure.ProviderRequestRejected(PaymentProvider.Stripe);
    }

    /// <summary>Reads the disputed payment intent from a dispute payload.</summary>
    private static string ReadDisputeIntent(Dispute dispute)
        => dispute.PaymentIntentId ?? dispute.PaymentIntent?.Id ?? string.Empty;

    /// <summary>Creates an intent service bound to the configured key.</summary>
    private PaymentIntentService NewService() => new(_client);

    /// <summary>Maps a Stripe intent onto the canonical result.</summary>
    private static PaymentIntentResult Map(PaymentIntent intent)
    {
        PaymentProviderIntentStatus status = intent.Status switch
        {
            StripeConstant.IntentStatus.RequiresAction
                or StripeConstant.IntentStatus.RequiresConfirmation
                or StripeConstant.IntentStatus.RequiresPaymentMethod =>
                PaymentProviderIntentStatus.RequiresAction,
            StripeConstant.IntentStatus.Processing => PaymentProviderIntentStatus.Processing,
            StripeConstant.IntentStatus.RequiresCapture => PaymentProviderIntentStatus.Authorized,
            StripeConstant.IntentStatus.Succeeded => PaymentProviderIntentStatus.Captured,
            StripeConstant.IntentStatus.Canceled => PaymentProviderIntentStatus.Cancelled,
            _ => PaymentProviderIntentStatus.Failed
        };

        string? failure = intent.LastPaymentError?.Code
            ?? intent.LastPaymentError?.DeclineCode;

        return new PaymentIntentResult(
            intent.Id,
            status,
            null,
            intent.ClientSecret,
            intent.Created.AddMinutes(StripeConstant.IntentLifetimeMinutes),
            failure);
    }
}
