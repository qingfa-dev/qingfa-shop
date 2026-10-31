using System.Text;

using Application.Modules.Payments.Webhooks.Commands;

using Carter;

using Core.BuildingBlocks.Functional.Errors;
using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.BuildingBlocks.Adapters.Serialization;
using Infrastructure.Modules.Payments;

using MediatR;

using Microsoft.Extensions.Options;

namespace Web.Api.Endpoints.Payments;

/// <summary>Carter module for provider webhooks; anonymous, signature-verified.</summary>
public sealed class PaymentWebhookEndpoint : ICarterModule
{
    private const string BasePath = "/api/payments/webhooks";

    /// <inheritdoc />
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost($"{BasePath}/{{provider}}", HandleAsync)
            .WithTags("Payments Webhooks")
            .WithName("PaymentWebhook")
            .WithSummary("Receive a signed provider webhook.")
            .AllowAnonymous();
    }

    /// <summary>Accepts a raw delivery and applies it through the module.</summary>
    private static async Task<IResult> HandleAsync(
        string provider,
        HttpRequest request,
        IOptions<PaymentsOptions> options,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        int maxBytes = options.Value.Limits.WebhookMaxBytes;

        if (!Enum.TryParse(provider, ignoreCase: true, out PaymentProvider parsed))
            return Result.Fail(PaymentsOutcome.Failure.WebhookProviderUnsupported(provider)).ToApiResult();

        if (request.ContentType is null
            || !request.ContentType.StartsWith(PaymentConstant.ContentTypes.Json, StringComparison.OrdinalIgnoreCase))
            return Result.Fail(PaymentsOutcome.Failure.WebhookContentTypeUnsupported()).ToApiResult();

        if (request.ContentLength is > 0 && request.ContentLength > maxBytes)
            return Result.Fail(PaymentsOutcome.Failure.WebhookPayloadTooLarge()).ToApiResult();

        // Handle: read the raw body; signature verification needs the exact bytes.
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        string rawBody = await reader.ReadToEndAsync(cancellationToken);

        if (rawBody.Length > maxBytes)
            return Result.Fail(PaymentsOutcome.Failure.WebhookPayloadTooLarge()).ToApiResult();

        IReadOnlyDictionary<string, string> headers = request.Headers
            .ToDictionary(header => header.Key, header => header.Value.ToString());

        Result<bool> result = await mediator.Send(
            new ProcessPaymentWebhook.UseCase(parsed, rawBody, headers),
            cancellationToken);

        if (result.IsFailure)
        {
            Error error = result.Errors[0];

            // Handle: an unknown reference is acknowledged; the provider cannot fix it by retrying
            // and a non-2xx would only produce a retry storm.
            if (error.Code == PaymentsOutcome.Codes.NotFound)
                return Result.Success().ToApiResult();

            return Result.Fail(error).ToApiResult();
        }

        return Result.Success().ToApiResult();
    }
}
