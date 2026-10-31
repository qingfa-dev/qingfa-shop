using Application.Modules.Todos.Events;

using Infrastructure.BuildingBlocks.Adapters.Messaging;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Options;
using Infrastructure.BuildingBlocks.Adapters.Messaging.Serialization;

using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Infrastructure.Modules.Todos.Integrations.Messaging;

/// <summary>Contributes the Todo exchange, queues, handlers, and event types.</summary>
public sealed class TodoMessagingModule : IMessagingModule
{
    private const string TodoCompletedEventName = TodoIntegrationConstant.Events.Completed;
    private const string TodoReminderChangedEventName = TodoIntegrationConstant.Events.ReminderChanged;
    private const int EventVersion = 1;

    /// <inheritdoc />
    public void RegisterEventTypes(EventTypeRegistry eventTypes)
    {
        ArgumentNullException.ThrowIfNull(eventTypes);

        eventTypes.Register<TodoCompletedIntegration>(TodoCompletedEventName, EventVersion);
        eventTypes.Register<TodoReminderChangedIntegration>(TodoReminderChangedEventName, EventVersion);
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

        string todoQueueName =
            $"{setting.QueuePrefix}.{TodoCompletedEventName}.{nameof(Handlers.TodoCompletedIntegrationHandler)}";
        string reminderQueueName =
            $"{setting.QueuePrefix}.{TodoReminderChangedEventName}.{nameof(Handlers.TodoReminderChangedIntegrationHandler)}";

        transport
            .BindExchange(setting.ExchangeName, Wolverine.RabbitMQ.ExchangeType.Topic)
            .ToQueue(todoQueueName, TodoCompletedEventName);
        transport
            .BindExchange(setting.ExchangeName, Wolverine.RabbitMQ.ExchangeType.Topic)
            .ToQueue(reminderQueueName, TodoReminderChangedEventName);

        // Publish: one topic exchange; the routing key is the stable event name.
        options
            .PublishMessage<TodoCompletedIntegration>()
            .ToRabbitRoutingKey(
                setting.ExchangeName,
                TodoCompletedEventName,
                exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Topic);
        options
            .PublishMessage<TodoReminderChangedIntegration>()
            .ToRabbitRoutingKey(
                setting.ExchangeName,
                TodoReminderChangedEventName,
                exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Topic);

        // Listen: one queue per consumer, tuned by prefetch and concurrency settings.
        options
            .ListenToRabbitQueue(todoQueueName)
            .PreFetchCount((ushort)setting.PrefetchCount);
        options
            .ListenToRabbitQueue(reminderQueueName)
            .PreFetchCount((ushort)setting.PrefetchCount);
    }
}
