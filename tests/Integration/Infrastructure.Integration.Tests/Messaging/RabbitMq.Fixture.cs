using Testcontainers.RabbitMq;

namespace Infrastructure.Integration.Tests.Messaging;

/// <summary>Boots a real RabbitMQ broker for the transport tests.</summary>
public sealed class RabbitMqFixture : IAsyncLifetime
{
#pragma warning disable TC001 // RabbitMqBuilder obsolete constructor
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4-management").Build();
#pragma warning restore TC001

    /// <summary>The AMQP connection string.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
    }
}

/// <summary>Collection for the real-broker messaging tests.</summary>
[CollectionDefinition(Name)]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
{
    /// <summary>Collection name.</summary>
    public const string Name = "rabbitmq";
}
