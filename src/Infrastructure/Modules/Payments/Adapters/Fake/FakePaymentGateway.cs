using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.BuildingBlocks.Functional.Maybes;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

namespace Infrastructure.Modules.Payments.Adapters.Fake;

/// <summary>Deterministic in-memory gateway for development and tests.</summary>
public sealed class FakePaymentGateway : IPaymentGateway, ISupportsPartialRefund, ISupportsManualCapture
{
    /// <summary>The shared secret mixed into the fake signature digest.</summary>
    public const string WebhookSecret = "fake-webhook-secret";

    /// <summary>The failure code reported for scripted declines.</summary>
    public const string DeclinedCode = "fake_declined";

    /// <summary>The prefix of the fake SHA-256 digest signature.</summary>
    public const string SignaturePrefix = "sha256=";

    /// <inheritdoc />
    public PaymentProvider Provider => PaymentProvider.Fake;

    /// <summary>
    /// Computes the fake signature header value: a SHA-256 digest over
    /// <c>secret:body</c>, hex-encoded and prefixed with <c>sha256=</c>.
    /// </summary>
    public static string Sign(string rawBody)
    {
        ArgumentNullException.ThrowIfNull(rawBody);

        return SignaturePrefix + Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes($"{WebhookSecret}:{rawBody}")));
    }

    /// <inheritdoc />
    public Task<Result<PaymentIntentResult>> CreateIntentAsync(
        PaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string reference = $"fake_{request.PaymentId:N}";

        if (request.ExternalReference.StartsWith("decline:", StringComparison.Ordinal))
            return Task.FromResult(Result<PaymentIntentResult>.Success(new PaymentIntentResult(
                reference, PaymentProviderIntentStatus.Failed, null, null, null, FakePaymentGateway.DeclinedCode)));

        if (request.ExternalReference.StartsWith("timeout:", StringComparison.Ordinal))
            return Task.FromResult(Result<PaymentIntentResult>.Fail(
                PaymentsOutcome.Failure.ProviderUnavailable(PaymentProvider.Fake)));

        PaymentProviderIntentStatus status = request.ExternalReference.StartsWith("declined-async:", StringComparison.Ordinal)
            ? PaymentProviderIntentStatus.Processing
            : PaymentProviderIntentStatus.RequiresAction;

        return Task.FromResult(Result<PaymentIntentResult>.Success(new PaymentIntentResult(
            reference,
            status,
            $"https://fake.pay/{reference}",
            "fake_client_secret",
            DateTimeOffset.UtcNow.AddMinutes(30),
            null)));
    }

    /// <inheritdoc />
    public Task<Result<PaymentIntentResult>> RetrieveAsync(string providerReference, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(providerReference);

        return Task.FromResult(Result<PaymentIntentResult>.Success(new PaymentIntentResult(
            providerReference, PaymentProviderIntentStatus.Captured, null, null, null, null)));
    }

    /// <inheritdoc />
    public Task<Result<PaymentCaptureResult>> CaptureAsync(
        PaymentCaptureRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(Result<PaymentCaptureResult>.Success(
            new PaymentCaptureResult($"cap_{request.IdempotencyKey}", request.Amount, true, null)));
    }

    /// <inheritdoc />
    public Task<Result<PaymentCancelResult>> CancelAsync(
        PaymentCancelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(Result<PaymentCancelResult>.Success(new PaymentCancelResult(true, null)));
    }

    /// <inheritdoc />
    public Task<Result<PaymentRefundResult>> RefundAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(Result<PaymentRefundResult>.Success(
            new PaymentRefundResult($"re_{request.RefundId:N}", request.Amount, true, null)));
    }

    /// <inheritdoc />
    public Task<Result<Maybe<PaymentProviderEvent>>> ParseWebhookAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        ArgumentNullException.ThrowIfNull(headers);

        if (!headers.TryGetValue(PaymentConstant.Headers.FakeSignature, out string? provided)
            || !IsValid(rawBody, provided))
            return Task.FromResult(Result<Maybe<PaymentProviderEvent>>.Fail(
                PaymentsOutcome.Failure.WebhookSignatureInvalid()));

        return Task.FromResult(Parse(rawBody));
    }

    private static Result<Maybe<PaymentProviderEvent>> Parse(string rawBody)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            return InvalidPayload();
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return InvalidPayload();

            if (!TryGetRequiredString(root, "type", out string type)
                || !TryGetRequiredString(root, "id", out string eventId)
                || !TryGetRequiredString(root, "reference", out string providerReference))
                return InvalidPayload();

            PaymentProviderEventType? eventType = type switch
            {
                "captured" => PaymentProviderEventType.Captured,
                "authorized" => PaymentProviderEventType.Authorized,
                "failed" => PaymentProviderEventType.Failed,
                "cancelled" => PaymentProviderEventType.Cancelled,
                "expired" => PaymentProviderEventType.Expired,
                "refunded" => PaymentProviderEventType.Refunded,
                "refund_failed" => PaymentProviderEventType.RefundFailed,
                _ => null
            };

            if (eventType is null)
                return Result<Maybe<PaymentProviderEvent>>.Success(Maybe<PaymentProviderEvent>.Empty());

            if (!TryGetOptionalAmount(root, out Money? amount)
                || !TryGetOptionalString(root, "refund", out string? refundReference))
                return InvalidPayload();

            return Result<Maybe<PaymentProviderEvent>>.Success(Maybe<PaymentProviderEvent>.From(
                new PaymentProviderEvent(
                    eventId,
                    eventType.Value,
                    providerReference,
                    refundReference,
                    amount,
                    DateTimeOffset.UtcNow)));
        }
    }

    private static bool TryGetRequiredString(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;

        if (!root.TryGetProperty(propertyName, out JsonElement element)
            || element.ValueKind != JsonValueKind.String)
            return false;

        string? text = element.GetString();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        value = text;
        return true;
    }

    private static bool TryGetOptionalString(JsonElement root, string propertyName, out string? value)
    {
        value = null;

        if (!root.TryGetProperty(propertyName, out JsonElement element)
            || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return true;

        if (element.ValueKind != JsonValueKind.String)
            return false;

        value = element.GetString();
        return true;
    }

    private static bool TryGetOptionalAmount(JsonElement root, out Money? amount)
    {
        amount = null;

        if (!root.TryGetProperty("amount", out JsonElement amountElement))
            return true;

        if (amountElement.ValueKind != JsonValueKind.Number
            || !amountElement.TryGetDecimal(out decimal value))
            return false;

        string currency = PaymentConstant.Defaults.Currency;
        if (root.TryGetProperty("currency", out JsonElement currencyElement))
        {
            if (currencyElement.ValueKind != JsonValueKind.String)
                return false;

            string? currencyText = currencyElement.GetString();
            if (string.IsNullOrWhiteSpace(currencyText))
                return false;

            currency = currencyText;
        }

        amount = new Money(value, new Currency(currency));
        return true;
    }

    private static Result<Maybe<PaymentProviderEvent>> InvalidPayload()
        => Result<Maybe<PaymentProviderEvent>>.Fail(
            PaymentsOutcome.Failure.WebhookPayloadInvalid());

    private static bool IsValid(string rawBody, string? provided)
    {
        if (string.IsNullOrEmpty(provided) || !provided.StartsWith(SignaturePrefix, StringComparison.Ordinal))
            return false;

        string expected = Sign(rawBody)[SignaturePrefix.Length..];

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided[SignaturePrefix.Length..].ToLowerInvariant()));
    }
}
