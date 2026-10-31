using Application.Modules.Payments.Ports;

namespace Application.Unit.Tests.Modules.Payments.Fixtures;

/// <summary>A policy with fixed, predictable limits for the unit tests.</summary>
public sealed class StubPaymentPolicy : IPaymentPolicy
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, decimal> MaximumAmountsByCurrency { get; init; } =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["USD"] = 100000m };

    /// <inheritdoc />
    public TimeSpan HostedCheckoutIntentLifetime { get; init; } = TimeSpan.FromMinutes(30);

    /// <inheritdoc />
    public TimeSpan ProviderExpiryGracePeriod { get; init; } = TimeSpan.FromMinutes(2);
}
