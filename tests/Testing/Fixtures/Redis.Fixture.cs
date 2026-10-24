using Testcontainers.Redis;

using Xunit;

namespace Testing;

/// <summary>Boots one real Redis container for a test collection.</summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private const string Image = "redis:7-alpine";
    private const string UnavailableMessage =
        "The Redis container is not running; guard with ContainerRuntime.IsAvailable first.";

    private RedisContainer? _container;

    /// <summary>Endpoint for the running container (host:port).</summary>
    /// <exception cref="InvalidOperationException">The container never started.</exception>
    public string ConnectionString => _container is null
        ? throw new InvalidOperationException(UnavailableMessage)
        : _container.GetConnectionString();

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        // Validate: with no engine the collection still initialises, so tests can
        // skip through ContainerRuntime.SkipReason instead of failing in the fixture.
        if (!ContainerRuntime.IsAvailable)
        {
            return;
        }

        ContainerRuntime.ConfigureProcessEnvironment();
        _container = new RedisBuilder(Image).Build();
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