using Application.BuildingBlocks.EventSourcing;
using Application.Modules.Payments.Persistence;

using Core.BuildingBlocks.Domain.EventSourcing;
using Core.Modules.Payments.Intents;

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

        services.TryAddScoped<IAggregateFactory<Payment>, PaymentAggregateFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IProjection, PaymentProjection>());
        services.TryAddScoped<IPaymentIdempotencyStore, PaymentIdempotencyStore>();
        services.TryAddScoped<IPaymentReferenceStore, PaymentReferenceStore>();
        services.TryAddScoped<IWebhookEventStore, WebhookEventStore>();
        services.TryAddScoped<IPaymentReadStore, PaymentReadStore>();
    }
}
