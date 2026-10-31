using Core.Modules.Payments;

namespace Application.Modules.Payments.Ports;

/// <summary>Resolves the enabled gateway for a provider.</summary>
public interface IPaymentGatewayRegistry
{
    /// <summary>Resolves an enabled gateway, or a stable failure when disabled.</summary>
    Result<IPaymentGateway> Resolve(PaymentProvider provider);
}
