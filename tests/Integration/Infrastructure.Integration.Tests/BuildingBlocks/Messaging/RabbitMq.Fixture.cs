using Testing;

using Testcontainers.RabbitMq;

namespace Infrastructure.Integration.Tests.BuildingBlocks.Messaging;

/// <summary>Boots a real RabbitMQ broker for the transport tests.</summary>
/// <remarks>
/// The image is pinned to the same tag as <c>app/Infrastructure/RabbitMQ/Dockerfile</c>
/// so the test broker and the Compose broker cannot drift. A missing container
/// engine leaves the fixture unstarted; the specs call <see cref="ContainerRuntime.SkipReason"/>
/// and skip instead of failing before they run.
/// </remarks>
public sealed class RabbitMqFixture : IAsyncLifetime
{
    private const string Image = "rabbitmq:4.3-management-alpine";
    private const string UnavailableMessage =
        "The RabbitMQ container is not running; guard with ContainerRuntime.IsAvailable first.";

    private RabbitMqContainer? _container;

    /// <summary>The AMQP connection string.</summary>
    /// <exception cref="InvalidOperationException">The container never started.</exception>
    public string ConnectionString => _container is null
        ? throw new InvalidOperationException(UnavailableMessage)
        : _container.GetConnectionString();

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        // Validate: with no engine the collection still initialises, so dependent
        // tests can skip via ContainerRuntime.SkipReason instead of failing here.
        if (!ContainerRuntime.IsAvailable)
        {
            return;
        }

        ContainerRuntime.ConfigureProcessEnvironment();

#pragma warning disable TC001 // RabbitMqBuilder obsolete constructor
        _container = new RabbitMqBuilder(Image).Build();
#pragma warning restore TC001

        await _container.StartAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>Collection for the real-broker messaging tests.</summary>
[CollectionDefinition(Name)]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
{
    /// <summary>Collection name.</summary>
    public const string Name = "rabbitmq";
}

/// <summary>Skips a broker spec when no container engine is available.</summary>
internal static class RabbitMqTestGuard
{
    /// <summary>Skips the current test with the shared container-runtime reason.</summary>
    public static void RequireContainer()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }
    }
}
