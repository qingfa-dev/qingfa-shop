using System.Diagnostics.CodeAnalysis;

using Application.Modules.Payments.Events;

using Core.Modules.Payments.Intents;

using Infrastructure.BuildingBlocks.Adapters.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Options;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;

using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Infrastructure.Modules.Payments.Integrations.Messaging;

/// <summary>Contributes the payment exchange, queue, and handler topology.</summary>
/// <remarks>
/// The exchange and queue wiring only runs against a live broker, so the RabbitMQ topology is covered
/// by the broker suite (<c>make test.infra.broker</c>) rather than the unit-coverage gate.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class PaymentMessagingModule : IMessagingModule
{
    /// <inheritdoc />
    public void RegisterEventTypes(EventTypeRegistry eventTypes)
    {
        ArgumentNullException.ThrowIfNull(eventTypes);

        eventTypes.Register<PaymentCapturedIntegration>(PaymentEventConstant.Captured, PaymentEventConstant.Version);
        eventTypes.Register<PaymentRefundedIntegration>(PaymentEventConstant.Refunded, PaymentEventConstant.Version);
        eventTypes.Register<PaymentFailedIntegration>(PaymentEventConstant.Failed, PaymentEventConstant.Version);
    }

    /// <inheritdoc />
    public void Configure(
        WolverineOptions options,
        MessagingSetting setting,
        EventTypeRegistry eventTypes,
        RabbitMqTransportExpression? transport)
    {
        // Skip: the in-memory transport has no exchange or queue topology to bind.
        if (transport is null)
            return;

        Publish(options, setting, PaymentEventConstant.Captured);
        Publish(options, setting, PaymentEventConstant.Refunded);
        Publish(options, setting, PaymentEventConstant.Failed);

        string queueName = $"{setting.QueuePrefix}.{PaymentEventConstant.Captured}";
        string refundQueueName = $"{setting.QueuePrefix}.{PaymentEventConstant.Refunded}";
        string failedQueueName = $"{setting.QueuePrefix}.{PaymentEventConstant.Failed}";

        transport
            .BindExchange(setting.ExchangeName, Wolverine.RabbitMQ.ExchangeType.Topic)
            .ToQueue(queueName, PaymentEventConstant.Captured);
        transport
            .BindExchange(setting.ExchangeName, Wolverine.RabbitMQ.ExchangeType.Topic)
            .ToQueue(refundQueueName, PaymentEventConstant.Refunded);
        transport
            .BindExchange(setting.ExchangeName, Wolverine.RabbitMQ.ExchangeType.Topic)
            .ToQueue(failedQueueName, PaymentEventConstant.Failed);

        options.ListenToRabbitQueue(queueName).PreFetchCount((ushort)setting.PrefetchCount);
        options.ListenToRabbitQueue(refundQueueName).PreFetchCount((ushort)setting.PrefetchCount);
        options.ListenToRabbitQueue(failedQueueName).PreFetchCount((ushort)setting.PrefetchCount);
    }

    /// <summary>Routes one payment event to the topic exchange by its stable name.</summary>
    private static void Publish(WolverineOptions options, MessagingSetting setting, string eventName)
    {
        switch (eventName)
        {
            case PaymentEventConstant.Captured:
                options.PublishMessage<PaymentCapturedIntegration>()
                    .ToRabbitRoutingKey(
                        setting.ExchangeName,
                        eventName,
                        exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Topic);
                break;

            case PaymentEventConstant.Refunded:
                options.PublishMessage<PaymentRefundedIntegration>()
                    .ToRabbitRoutingKey(
                        setting.ExchangeName,
                        eventName,
                        exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Topic);
                break;

            default:
                options.PublishMessage<PaymentFailedIntegration>()
                    .ToRabbitRoutingKey(
                        setting.ExchangeName,
                        eventName,
                        exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Topic);
                break;
        }
    }
}
