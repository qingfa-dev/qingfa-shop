using Application.Modules.Payments.Ports;

using Core.Modules.Payments.Intents;

namespace Application.Unit.Tests.Modules.Payments.Fixtures;

/// <summary>A policy with fixed, predictable limits for the unit tests.</summary>
public sealed class StubPaymentPolicy : IPaymentPolicy
{
    /// <inheritdoc />
    public decimal MaxAmount { get; init; } = PaymentConstant.Defaults.MaxAmount;

    /// <inheritdoc />
    public TimeSpan IntentLifetime { get; init; } = TimeSpan.FromMinutes(30);

    /// <inheritdoc />
    public TimeSpan ExpiryGrace { get; init; } = TimeSpan.FromMinutes(5);
}
