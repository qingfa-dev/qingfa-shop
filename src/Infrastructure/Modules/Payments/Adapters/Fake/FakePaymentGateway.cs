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
    /// <summary>The shared secret used by the fake signature scheme.</summary>
    public const string WebhookSecret = "fake-webhook-secret";

    /// <summary>The failure code reported for scripted declines.</summary>
    public const string DeclinedCode = "fake_declined";

    /// <summary>The prefix of the fake HMAC signature.</summary>
    public const string SignaturePrefix = "sha256=";

    /// <inheritdoc />
    public PaymentProvider Provider => PaymentProvider.Fake;

    /// <summary>Computes the signature header value for a webhook body.</summary>
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
        => Task.FromResult(Result<PaymentIntentResult>.Success(new PaymentIntentResult(
            providerReference, PaymentProviderIntentStatus.Captured, null, null, null, null)));

    /// <inheritdoc />
    public Task<Result<PaymentCaptureResult>> CaptureAsync(
        PaymentCaptureRequest request,
        CancellationToken cancellationToken)
        => Task.FromResult(Result<PaymentCaptureResult>.Success(
            new PaymentCaptureResult($"cap_{request.IdempotencyKey}", request.Amount, true, null)));

    /// <inheritdoc />
    public Task<Result<PaymentCancelResult>> CancelAsync(
        PaymentCancelRequest request,
        CancellationToken cancellationToken)
        => Task.FromResult(Result<PaymentCancelResult>.Success(new PaymentCancelResult(true, null)));

    /// <inheritdoc />
    public Task<Result<PaymentRefundResult>> RefundAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken)
        => Task.FromResult(Result<PaymentRefundResult>.Success(
            new PaymentRefundResult($"re_{request.RefundId:N}", request.Amount, true, null)));

    /// <inheritdoc />
    public Task<Result<Maybe<PaymentProviderEvent>>> ParseWebhookAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        if (!headers.TryGetValue(PaymentConstant.Headers.FakeSignature, out string? provided)
            || !IsValid(rawBody, provided))
            return Task.FromResult(Result<Maybe<PaymentProviderEvent>>.Fail(
                PaymentsOutcome.Failure.WebhookSignatureInvalid()));

        try
        {
            return Task.FromResult(Parse(rawBody));
        }
        catch (JsonException)
        {
            return Task.FromResult(Result<Maybe<PaymentProviderEvent>>.Fail(
                PaymentsOutcome.Failure.WebhookPayloadInvalid()));
        }
    }

    private static Result<Maybe<PaymentProviderEvent>> Parse(string rawBody)
    {
        using JsonDocument document = JsonDocument.Parse(rawBody);
        JsonElement root = document.RootElement;

        string type = root.GetProperty("type").GetString() ?? string.Empty;
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

        Money? amount = root.TryGetProperty("amount", out JsonElement amountElement)
            ? new Money(
                amountElement.GetDecimal(),
                new Currency(root.GetProperty("currency").GetString() ?? PaymentConstant.Defaults.Currency))
            : null;

        return Result<Maybe<PaymentProviderEvent>>.Success(Maybe<PaymentProviderEvent>.From(
            new PaymentProviderEvent(
                root.GetProperty("id").GetString()!,
                eventType.Value,
                root.GetProperty("reference").GetString()!,
                root.TryGetProperty("refund", out JsonElement refund) ? refund.GetString() : null,
                amount,
                DateTimeOffset.UtcNow)));
    }

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
