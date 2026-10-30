using Application.Common.Messaging.Bus;

using Core.Common.Monad.Metadata;
using Core.Common.Monad.Results;

using Infrastructure.Integrations.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace Infrastructure.Integration.Tests.Messaging;

/// <summary>Publishes and consumes through a real RabbitMQ broker.</summary>
[Collection(RabbitMqCollection.Name)]
public sealed class RabbitMqMessagingTest(RabbitMqFixture fixture) : IAsyncLifetime
{
    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        TestRabbitConsumer.Reset();
        RetryRabbitConsumer.Reset();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Publish_ShouldDeliverToTheConsumer()
    {
        // Validate: the fixture cannot start a broker without a container engine.
        RabbitMqTestGuard.RequireContainer();

        // Arrange
        string exchange = $"test.{Guid.NewGuid():N}";
        string queue = $"test.{Guid.NewGuid():N}";
        using IHost host = BuildHost(exchange, queue);
        await host.StartAsync();
        var bus = new WolverineEventBus(host.Services.GetRequiredService<IMessageBus>());
        var message = new TestRabbitEvent(Guid.NewGuid());

        // Act
        Result published = await bus.PublishAsync(message);

        // Assert
        published.IsSuccess.Should().BeTrue();
        Guid received = await TestRabbitConsumer.Received.Task.WaitAsync(TimeSpan.FromSeconds(15));
        received.Should().Be(message.Id);
        await host.StopAsync();
    }

    [Fact]
    public async Task ConsumerFailure_ShouldRetryUntilSuccess()
    {
        // Validate: the fixture cannot start a broker without a container engine.
        RabbitMqTestGuard.RequireContainer();

        // Arrange
        string exchange = $"test.{Guid.NewGuid():N}";
        string queue = $"test.{Guid.NewGuid():N}";
        using IHost host = BuildHost(exchange, queue);
        await host.StartAsync();
        var bus = new WolverineEventBus(host.Services.GetRequiredService<IMessageBus>());
        var message = new RetryRabbitEvent(Guid.NewGuid());

        // Act
        Result published = await bus.PublishAsync(message);

        // Assert
        published.IsSuccess.Should().BeTrue();
        Guid received = await RetryRabbitConsumer.Received.Task.WaitAsync(TimeSpan.FromSeconds(15));
        received.Should().Be(message.Id);
        RetryRabbitConsumer.Attempts.Should().BeGreaterThanOrEqualTo(3);
        await host.StopAsync();
    }

    private IHost BuildHost(string exchange, string queue)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.UseWolverine(options =>
        {
            var transport = options
                .UseRabbitMq(new Uri(fixture.ConnectionString))
                .AutoProvision();

            transport
                .BindExchange(exchange, Wolverine.RabbitMQ.ExchangeType.Topic)
                .ToQueue(queue, "test.event");

            options
                .PublishMessage<TestRabbitEvent>()
                .ToRabbitRoutingKey(
                    exchange,
                    "test.event",
                    configured => configured.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Topic);

            options
                .PublishMessage<RetryRabbitEvent>()
                .ToRabbitRoutingKey(
                    exchange,
                    "test.event",
                    configured => configured.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Topic);

            options.Discovery.IncludeAssembly(typeof(RabbitMqMessagingTest).Assembly);
            options.ListenToRabbitQueue(queue);
            options.OnException<Exception>().RetryWithCooldown(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(300));
        });

        return builder.Build();
    }
}

/// <summary>A message consumed by <see cref="TestRabbitConsumer"/>.</summary>
public sealed record TestRabbitEvent(Guid Id) : IIntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
    public string EventName => "test.event";
    public int Version => 1;
    public DefaultMetadata Metadata { get; init; } = [];
}

/// <summary>A message consumed by <see cref="RetryRabbitConsumer"/>.</summary>
public sealed record RetryRabbitEvent(Guid Id) : IIntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
    public string EventName => "test.event";
    public int Version => 1;
    public DefaultMetadata Metadata { get; init; } = [];
}

/// <summary>Records the delivered message.</summary>
public sealed class TestRabbitConsumer
{
    public static TaskCompletionSource<Guid> Received { get; private set; } = NewSource();

    public static void Reset() => Received = NewSource();

    public Task Handle(TestRabbitEvent message, CancellationToken cancellationToken)
    {
        Received.TrySetResult(message.Id);
        return Task.CompletedTask;
    }

    private static TaskCompletionSource<Guid> NewSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>Throws twice, then records the third delivery.</summary>
public sealed class RetryRabbitConsumer
{
    public static TaskCompletionSource<Guid> Received { get; private set; } = NewSource();

    public static int Attempts;

    public static void Reset()
    {
        Received = NewSource();
        Attempts = 0;
    }

    public Task Handle(RetryRabbitEvent message, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref Attempts) < 3)
            throw new InvalidOperationException("transient consumer failure");

        Received.TrySetResult(message.Id);
        return Task.CompletedTask;
    }

    private static TaskCompletionSource<Guid> NewSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
