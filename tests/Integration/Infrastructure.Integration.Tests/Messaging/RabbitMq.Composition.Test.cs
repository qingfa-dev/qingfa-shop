using Application.Common.Caching;
using Application.Common.Messaging.Inbox;
using Application.Features.Todos.Events;

using Core.Common.Monad.Results;

using Infrastructure.Integrations.Messaging;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Wolverine;

namespace Infrastructure.Integration.Tests.Messaging;

/// <summary>Exercises the production `AddMessaging` composition against a real broker.</summary>
[Collection(RabbitMqCollection.Name)]
public sealed class RabbitMqCompositionTest(RabbitMqFixture fixture)
{
    [Fact]
    public async Task AddMessaging_ShouldDeliverThroughTheProductionComposition()
    {
        // Validate: the fixture cannot start a broker without a container engine.
        RabbitMqTestGuard.RequireContainer();

        // Arrange
        var cache = new RecordingCache();
        var inbox = new PassthroughInbox();
        string suffix = Guid.NewGuid().ToString("N");

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Messaging:Enabled"] = "true",
            ["Messaging:Transport"] = "RabbitMq",
            ["Messaging:ConnectionString"] = fixture.ConnectionString,
            ["Messaging:ExchangeName"] = $"test.{suffix}",
            ["Messaging:QueuePrefix"] = $"test.{suffix}",
            ["Messaging:PrefetchCount"] = "4",
            ["Messaging:MaxConcurrency"] = "2",
            ["Messaging:MaxAttempts"] = "3",
            ["Messaging:RetryDelaySeconds"] = "1",
        });
        builder.Services.AddSingleton<ICacheService>(cache);
        builder.Services.AddSingleton<IInboxStore>(inbox);
        builder.AddMessaging(builder.Configuration);

        await using WebApplication app = builder.Build();
        await app.StartAsync();
        using IServiceScope scope = app.Services.CreateScope();
        var bus = new WolverineEventBus(scope.ServiceProvider.GetRequiredService<IMessageBus>());
        var integration = new TodoCompletedIntegration
        {
            TodoId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CompletedAtUtc = DateTimeOffset.UtcNow,
        };

        // Act
        Result published = await bus.PublishAsync(integration);
        string key = await cache.Removed.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        published.IsSuccess.Should().BeTrue();
        key.Should().Be($"todo:{integration.TodoId}");
        inbox.Executions.Should().Be(1);
        await app.StopAsync();
    }

    private sealed class PassthroughInbox : IInboxStore
    {
        public int Executions { get; private set; }

        public async Task<Result> ExecuteOnceAsync(
            Guid messageId,
            string consumer,
            Func<CancellationToken, Task<Result>> handler,
            CancellationToken cancellationToken = default)
        {
            Executions++;
            return await handler(cancellationToken);
        }
    }

    private sealed class RecordingCache : ICacheService
    {
        public TaskCompletionSource<string> Removed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<Result<T>> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, ValueTask<T>> factory,
            CachingEntryOption? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
            where T : notnull
            => Result<T>.Success(await factory(cancellationToken));

        public ValueTask<Result> SetAsync<T>(
            string key,
            T value,
            CachingEntryOption? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
            where T : notnull
            => ValueTask.FromResult(Result.Ok());

        public ValueTask<Result> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Removed.TrySetResult(key);
            return ValueTask.FromResult(Result.Ok());
        }

        public ValueTask<Result> RemoveByTagAsync(
            IEnumerable<string> tags,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Result.Ok());

        public ValueTask<Result> RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Result.Ok());
    }
}
