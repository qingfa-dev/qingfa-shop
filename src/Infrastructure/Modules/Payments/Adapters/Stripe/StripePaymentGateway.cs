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

        PaymentIntent intent = await service.CaptureAsync(
            request.ProviderReference,
            new PaymentIntentCaptureOptions { AmountToCapture = minor.Value },
            new RequestOptions { IdempotencyKey = request.IdempotencyKey },
            cancellationToken);

        return Result<PaymentCaptureResult>.Success(
            new PaymentCaptureResult(intent.Id, request.Amount, true, null));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentCancelResult>> CancelAsync(
        PaymentCancelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        PaymentIntentService service = NewService();

        await service.CancelAsync(
            request.ProviderReference,
            new PaymentIntentCancelOptions(),
            new RequestOptions { IdempotencyKey = request.IdempotencyKey },
            cancellationToken);

        return Result<PaymentCancelResult>.Success(new PaymentCancelResult(true, null));
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

        Refund refund = await service.CreateAsync(
            new RefundCreateOptions { PaymentIntent = request.ProviderReference, Amount = minor.Value },
            new RequestOptions { IdempotencyKey = request.IdempotencyKey },
            cancellationToken);

        return Result<PaymentRefundResult>.Success(
            new PaymentRefundResult(refund.Id, request.Amount, true, null));
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
                "payment_intent.amount_capturable_updated" => PaymentProviderEventType.Authorized,
                "payment_intent.succeeded" => PaymentProviderEventType.Captured,
                "payment_intent.payment_failed" => PaymentProviderEventType.Failed,
                "payment_intent.canceled" => PaymentProviderEventType.Cancelled,
                "charge.refunded" => PaymentProviderEventType.Refunded,
                "charge.refund.updated" => PaymentProviderEventType.RefundFailed,
                _ => null
            };

            if (type is null)
                return Task.FromResult(Result<Maybe<PaymentProviderEvent>>.Success(
                    Maybe<PaymentProviderEvent>.Empty()));

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
            intent.Created.AddMinutes(30),
            failure);
    }
}
