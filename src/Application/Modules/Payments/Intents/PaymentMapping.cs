using Application.Modules.Payments.Intents.Models;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

namespace Application.Modules.Payments.Intents;

/// <summary>Maps domain payments to API models.</summary>
public static class PaymentMapping
{
    /// <summary>Maps a payment aggregate.</summary>
    /// <param name="payment">The aggregate.</param>
    /// <returns>The API view.</returns>
    public static PaymentResponseModel ToResponse(this Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return new PaymentResponseModel
        {
            Id = payment.Id,
            Provider = payment.Provider.ToWire(),
            Status = payment.Status.ToWire(),
            Amount = new PaymentAmountModel(payment.Amount.Amount, payment.Amount.Currency.Code ?? PaymentConstant.Defaults.Currency),
            CapturedAmount = new PaymentAmountModel(payment.CapturedAmount.Amount, payment.CapturedAmount.Currency.Code ?? PaymentConstant.Defaults.Currency),
            RefundedAmount = new PaymentAmountModel(payment.RefundedAmount.Amount, payment.RefundedAmount.Currency.Code ?? PaymentConstant.Defaults.Currency),
            ExternalReference = payment.ExternalReference,
            ApprovalUrl = payment.ApprovalUrl,
            ExpiresAt = payment.ExpiresAt,
            CreatedAt = payment.CreatedAt,
            FailureCode = payment.FailureCode,
            FailureMessage = payment.FailureMessage
        };
    }

    /// <summary>Maps a projected payment row.</summary>
    /// <param name="model">The projected row.</param>
    /// <returns>The API view.</returns>
    public static PaymentResponseModel ToResponse(this PaymentReadModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new PaymentResponseModel
        {
            Id = model.Id,
            Provider = model.Provider,
            Status = model.Status,
            Amount = new PaymentAmountModel(model.Amount, model.Currency),
            CapturedAmount = new PaymentAmountModel(model.CapturedAmount, model.Currency),
            RefundedAmount = new PaymentAmountModel(model.RefundedAmount, model.Currency),
            ExternalReference = model.ExternalReference,
            ApprovalUrl = model.ApprovalUrl,
            ExpiresAt = model.ExpiresAt,
            CreatedAt = model.CreatedAt,
            FailureCode = model.FailureCode,
            FailureMessage = model.FailureMessage
        };
    }
}
