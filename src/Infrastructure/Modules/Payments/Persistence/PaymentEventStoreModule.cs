using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Persistence;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Modules.Payments.Persistence;

/// <summary>Registers the Payments projection and stores.</summary>
public sealed class PaymentEventStoreModule : IEventStoreModule
{
    /// <inheritdoc />
    public void Configure(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IProjection, PaymentProjection>());
        services.TryAddScoped<IPaymentIdempotencyStore, PaymentIdempotencyStore>();
        services.TryAddScoped<IPaymentReferenceStore, PaymentReferenceStore>();
        services.TryAddScoped<IWebhookEventStore, WebhookEventStore>();
        services.TryAddScoped<IPaymentReadStore, PaymentReadStore>();
    }
}
