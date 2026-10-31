using Application.Modules.Payments.Ports;

using Core.Modules.Payments;

namespace Infrastructure.Modules.Payments.Adapters.Fake;

/// <summary>Resolves gateways from the registered adapters.</summary>
public sealed class PaymentGatewayRegistry : IPaymentGatewayRegistry
{
    private readonly IReadOnlyDictionary<PaymentProvider, IPaymentGateway> _gateways;

    /// <summary>Creates the registry from the registered adapters.</summary>
    public PaymentGatewayRegistry(IEnumerable<IPaymentGateway> gateways)
    {
        ArgumentNullException.ThrowIfNull(gateways);
        _gateways = gateways.ToDictionary(gateway => gateway.Provider);
    }

    /// <inheritdoc />
    public Result<IPaymentGateway> Resolve(PaymentProvider provider)
        => _gateways.TryGetValue(provider, out IPaymentGateway? gateway)
            ? Result<IPaymentGateway>.Success(gateway)
            : Result<IPaymentGateway>.Fail(PaymentsOutcome.Failure.ProviderDisabled(provider));
}
