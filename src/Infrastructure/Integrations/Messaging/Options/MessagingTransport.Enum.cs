namespace Infrastructure.Integrations.Messaging.Options;

/// <summary>Selects the transport Wolverine uses for integration events.</summary>
public enum MessagingTransport
{
    /// <summary>Wolverine's local, in-process transport; no broker is contacted.</summary>
    InMemory = 0,

    /// <summary>RabbitMQ via the stable amqp connection string.</summary>
    RabbitMq = 1,
}
