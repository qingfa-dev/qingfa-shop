using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Application.Modules.Payments.Ports;
using Application.Modules.Payments.Ports.Models;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.BuildingBlocks.Functional.Maybes;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.Modules.Payments.Adapters.Shared;

namespace Infrastructure.Modules.Payments.Adapters.PayPal;

/// <summary>PayPal Orders v2 adapter; refunds are not enabled in this slice.</summary>
public sealed class PayPalPaymentGateway(
    HttpClient client,
    PayPalTokenProvider tokens,
    PayPalOptions options) : IPaymentGateway
{
    /// <inheritdoc />
    public PaymentProvider Provider => PaymentProvider.PayPal;

    /// <inheritdoc />
    public PaymentGatewayCapabilities Capabilities { get; } = new()
    {
        SupportsManualCapture = true,
        SupportsWebhooks = true
    };

    /// <inheritdoc />
    public async Task<Result<PaymentIntentResult>> CreateIntentAsync(
        PaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = new HttpRequestMessage(HttpMethod.Post, "v2/checkout/orders")
        {
            Content = JsonContent.Create(new
            {
                intent = request.CaptureMode is PaymentCaptureMode.Manual
                    ? PayPalConstant.OrderIntent.Authorize
                    : PayPalConstant.OrderIntent.Capture,
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = request.Amount.Currency.Code,
                            value = MoneyConverter.ToDecimalString(request.Amount)
                        },
                        custom_id = request.ExternalReference
                    }
                }
            })
        };

        message.Headers.Authorization = new AuthenticationHeaderValue(PayPalConstant.Headers.AuthorizationScheme, await tokens.GetAsync(cancellationToken));
        message.Headers.Add(PayPalConstant.Headers.RequestId, request.IdempotencyKey);

        using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<PaymentIntentResult>.Fail(
                PaymentsOutcome.Failure.ProviderRequestRejected(PaymentProvider.PayPal));

        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));

        string id = body.RootElement.GetProperty("id").GetString()!;
        string? approve = FindApproveLink(body.RootElement);

        return Result<PaymentIntentResult>.Success(new PaymentIntentResult(
            id, PaymentProviderIntentStatus.RequiresAction, approve, null, null, null));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentIntentResult>> RetrieveAsync(
        string providerReference,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, $"v2/checkout/orders/{providerReference}");
        message.Headers.Authorization = new AuthenticationHeaderValue(PayPalConstant.Headers.AuthorizationScheme, await tokens.GetAsync(cancellationToken));

        using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<PaymentIntentResult>.Fail(
                PaymentsOutcome.Failure.ProviderRequestRejected(PaymentProvider.PayPal));

        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));

        string status = body.RootElement.GetProperty("status").GetString() ?? "UNKNOWN";

        PaymentProviderIntentStatus mapped = status switch
        {
            PayPalConstant.OrderStatus.Completed => PaymentProviderIntentStatus.Captured,
            PayPalConstant.OrderStatus.Approved => PaymentProviderIntentStatus.Authorized,
            PayPalConstant.OrderStatus.Created => PaymentProviderIntentStatus.RequiresAction,
            _ => PaymentProviderIntentStatus.Failed
        };

        return Result<PaymentIntentResult>.Success(new PaymentIntentResult(
            providerReference, mapped, null, null, null, null));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentCaptureResult>> CaptureAsync(
        PaymentCaptureRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"v2/checkout/orders/{request.ProviderReference}/capture");

        message.Headers.Authorization = new AuthenticationHeaderValue(PayPalConstant.Headers.AuthorizationScheme, await tokens.GetAsync(cancellationToken));
        message.Headers.Add(PayPalConstant.Headers.RequestId, request.IdempotencyKey);

        using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<PaymentCaptureResult>.Success(new PaymentCaptureResult(
                request.ProviderReference,
                request.Amount,
                false,
                PaymentConstant.Reasons.CaptureFailed));

        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));

        return Result<PaymentCaptureResult>.Success(new PaymentCaptureResult(
            request.ProviderReference,
            request.Amount,
            true,
            null,
            ReadCaptureId(body.RootElement, request.ProviderReference)));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentCancelResult>> CancelAsync(
        PaymentCancelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"v2/checkout/orders/{request.ProviderReference}/cancel");

        message.Headers.Authorization = new AuthenticationHeaderValue(PayPalConstant.Headers.AuthorizationScheme, await tokens.GetAsync(cancellationToken));

        using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);

        return Result<PaymentCancelResult>.Success(
            new PaymentCancelResult(response.IsSuccessStatusCode, null));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentRefundResult>> RefundAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate: PayPal refunds a capture, so a missing capture id cannot be refunded.
        if (string.IsNullOrWhiteSpace(request.CaptureReference))
            return Result<PaymentRefundResult>.Fail(
                PaymentsOutcome.Failure.ProviderReferenceMissing());

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"v2/payments/captures/{request.CaptureReference}/refund")
        {
            Content = JsonContent.Create(new
            {
                amount = new
                {
                    value = MoneyConverter.ToDecimalString(request.Amount),
                    currency_code = request.Amount.Currency.Code
                }
            })
        };

        message.Headers.Authorization = new AuthenticationHeaderValue(
            PayPalConstant.Headers.AuthorizationScheme,
            await tokens.GetAsync(cancellationToken));
        message.Headers.Add(PayPalConstant.Headers.RequestId, request.IdempotencyKey);

        using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<PaymentRefundResult>.Success(
                new PaymentRefundResult(
                    request.CaptureReference,
                    request.Amount,
                    false,
                    PaymentConstant.Reasons.RefundFailed));

        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));

        return Result<PaymentRefundResult>.Success(new PaymentRefundResult(
            ReadString(body.RootElement, "id"),
            request.Amount,
            true,
            null));
    }

    /// <inheritdoc />
    public async Task<Result<Maybe<PaymentProviderEvent>>> ParseWebhookAsync(
        ReadOnlyMemory<byte> rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(headers);

        using JsonDocument document = JsonDocument.Parse(rawBody);
        JsonElement root = document.RootElement;

        using var message = new HttpRequestMessage(HttpMethod.Post, "v1/notifications/verify-webhook-signature")
        {
            Content = JsonContent.Create(new
            {
                transmission_id = headers.GetValueOrDefault(PaymentConstant.Headers.PayPalTransmissionId),
                transmission_time = headers.GetValueOrDefault(PaymentConstant.Headers.PayPalTransmissionTime),
                cert_url = headers.GetValueOrDefault(PaymentConstant.Headers.PayPalCertUrl),
                auth_algo = headers.GetValueOrDefault(PaymentConstant.Headers.PayPalAuthAlgo),
                transmission_sig = headers.GetValueOrDefault(PaymentConstant.Headers.PayPalTransmissionSig),
                webhook_id = options.WebhookId,
                webhook_event = root
            })
        };

        message.Headers.Authorization = new AuthenticationHeaderValue(PayPalConstant.Headers.AuthorizationScheme, await tokens.GetAsync(cancellationToken));

        using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<Maybe<PaymentProviderEvent>>.Fail(
                PaymentsOutcome.Failure.WebhookSignatureInvalid());

        using JsonDocument verification = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));

        if (!verification.RootElement.TryGetProperty(
                PayPalConstant.Verification.StatusProperty, out JsonElement status)
            || status.GetString() != PayPalConstant.Verification.Success)
            return Result<Maybe<PaymentProviderEvent>>.Fail(
                PaymentsOutcome.Failure.WebhookSignatureInvalid());

        return Result<Maybe<PaymentProviderEvent>>.Success(Normalize(root));
    }

    /// <summary>Maps a PayPal event onto the canonical event.</summary>
    private static Maybe<PaymentProviderEvent> Normalize(JsonElement root)
    {
        string eventType = root.GetProperty("event_type").GetString() ?? string.Empty;
        string eventId = root.GetProperty("id").GetString() ?? string.Empty;

        if (!root.TryGetProperty("resource", out JsonElement resource))
            return Maybe<PaymentProviderEvent>.Empty();

        (PaymentProviderEventType Type, string Reference)? mapped = eventType switch
        {
            PayPalConstant.EventType.OrderApproved =>
                (PaymentProviderEventType.Authorized, ReadString(resource, "id")),
            PayPalConstant.EventType.CaptureCompleted =>
                (PaymentProviderEventType.Captured, ReadOrderId(resource)),
            PayPalConstant.EventType.CaptureDenied =>
                (PaymentProviderEventType.Failed, ReadOrderId(resource)),
            PayPalConstant.EventType.CaptureRefunded =>
                (PaymentProviderEventType.Refunded, ReadOrderId(resource)),
            PayPalConstant.EventType.DisputeCreated =>
                (PaymentProviderEventType.DisputeOpened, ReadDisputeOrderId(resource)),
            PayPalConstant.EventType.DisputeResolved =>
                (PaymentProviderEventType.DisputeLost, ReadDisputeOrderId(resource)),
            _ => null
        };

        if (mapped is null || string.IsNullOrEmpty(mapped.Value.Reference))
            return Maybe<PaymentProviderEvent>.Empty();

        return Maybe<PaymentProviderEvent>.From(new PaymentProviderEvent(
            eventId,
            mapped.Value.Type,
            mapped.Value.Reference,
            null,
            null,
            DateTimeOffset.UtcNow,
            CaptureReference: mapped.Value.Type is PaymentProviderEventType.Captured
                ? ReadString(resource, "id")
                : null,
            DisputeReference: mapped.Value.Type is PaymentProviderEventType.DisputeOpened
                or PaymentProviderEventType.DisputeLost
                ? ReadString(resource, "dispute_id")
                : null,
            DisputeReason: ReadString(resource, "reason")));
    }

    /// <summary>Reads a string property, tolerating absence.</summary>
    private static string ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out JsonElement value) ? value.GetString() ?? string.Empty : string.Empty;

    /// <summary>Reads the disputed order id from a dispute resource.</summary>
    private static string ReadDisputeOrderId(JsonElement resource)
    {
        if (!resource.TryGetProperty("disputed_transactions", out JsonElement transactions)
            || transactions.ValueKind is not JsonValueKind.Array)
            return string.Empty;

        foreach (JsonElement transaction in transactions.EnumerateArray())
        {
            string orderId = ReadString(transaction, "seller_transaction_id");

            if (!string.IsNullOrEmpty(orderId))
                return orderId;
        }

        return string.Empty;
    }

    /// <summary>Reads the first capture id from a captured order response.</summary>
    private static string? ReadCaptureId(JsonElement root, string orderId)
    {
        if (!root.TryGetProperty("purchase_units", out JsonElement units) || units.ValueKind is not JsonValueKind.Array)
            return null;

        foreach (JsonElement unit in units.EnumerateArray())
        {
            if (!unit.TryGetProperty("payments", out JsonElement payments)
                || !payments.TryGetProperty("captures", out JsonElement captures)
                || captures.ValueKind is not JsonValueKind.Array)
                continue;

            foreach (JsonElement capture in captures.EnumerateArray())
            {
                string id = ReadString(capture, "id");

                if (!string.IsNullOrEmpty(id))
                    return id;
            }
        }

        _ = orderId;
        return null;
    }

    /// <summary>Reads the related order id from a capture resource.</summary>
    private static string ReadOrderId(JsonElement resource)
    {
        if (!resource.TryGetProperty("supplementary_data", out JsonElement supplementary)
            || !supplementary.TryGetProperty("related_ids", out JsonElement related))
            return string.Empty;

        return ReadString(related, "order_id");
    }

    /// <summary>Finds the approval link in an order response.</summary>
    private static string? FindApproveLink(JsonElement root)
    {
        if (!root.TryGetProperty("links", out JsonElement links) || links.ValueKind is not JsonValueKind.Array)
            return null;

        foreach (JsonElement link in links.EnumerateArray())
        {
            if (ReadString(link, "rel") == "approve")
                return ReadString(link, "href");
        }

        return null;
    }
}
