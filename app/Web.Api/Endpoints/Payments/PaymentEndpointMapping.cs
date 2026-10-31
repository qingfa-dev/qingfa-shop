using Application.Modules.Payments.Intents.Commands;
using Application.Modules.Payments.Intents.Models;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.BuildingBlocks.Functional.Results;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Web.Api.Endpoints.Payments.Models;

namespace Web.Api.Endpoints.Payments;

/// <summary>Maps endpoint bodies onto application requests.</summary>
internal static class PaymentEndpointMapping
{
    /// <summary>Converts a transport amount to the domain value object.</summary>
    /// <param name="model">The transport amount.</param>
    /// <returns>The domain amount, or a stable validation failure.</returns>
    internal static Result<Money> ToMoney(this PaymentAmountModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return Currency.Create(model.Currency)
            .Map(currency => new Money(model.Amount, currency));
    }

    /// <summary>Converts the create body to an application request.</summary>
    /// <param name="body">The transport body.</param>
    /// <returns>The application request, or a stable validation failure.</returns>
    internal static Result<CreatePayment.Request> ToRequest(this CreatePaymentBody body)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (!Enum.TryParse(body.Provider, ignoreCase: true, out PaymentProvider provider))
            return Result<CreatePayment.Request>.Fail(
                PaymentsOutcome.Failure.ProviderUnknown(body.Provider));

        if (!Enum.TryParse(body.CaptureMode, ignoreCase: true, out PaymentCaptureMode captureMode))
            return Result<CreatePayment.Request>.Fail(
                PaymentsOutcome.Failure.CaptureModeInvalid(body.CaptureMode));

        return body.Amount.ToMoney()
            .Map(amount => new CreatePayment.Request(
                amount,
                provider,
                body.ExternalReference,
                captureMode,
                body.ReturnUrl,
                body.CancelUrl));
    }
}
