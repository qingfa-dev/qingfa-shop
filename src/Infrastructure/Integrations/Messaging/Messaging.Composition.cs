using Application.Common.Messaging.Bus;
using Application.Common.Messaging.Events;
using Application.Common.Validation.FluentValidation;
using Application.Features.Todos.Events;

using Core.Common.Domain.Events;

using FluentValidation;

using Infrastructure.Integrations.Messaging.Options;
using Infrastructure.Integrations.Messaging.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using RabbitMQ.Client;

using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace Infrastructure.Integrations.Messaging;

/// <summary>Composes Wolverine, the messaging settings, and the event-bus adapter.</summary>
/// <remarks>
/// Reliability stays with the outbox: Wolverine is configured without its durable outbox or
/// transactional middleware, so a second reliability mechanism never competes with ours.
/// </remarks>
public static class MessagingComposition
{
    private const string TodoCompletedEventName = "todo.completed";
    private const int EventVersion = 1;

    /// <summary>Adds the messaging pipeline to the host.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configuration">Configuration holding the <c>Messaging</c> section.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configuration"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The RabbitMQ transport is selected but no connection string is configured.
    /// </exception>
    public static WebApplicationBuilder AddMessaging(
        this WebApplicationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.AddSingleton<IValidator<MessagingSetting>, MessagingSettingValidator>();
        builder.Services
            .AddOptions<MessagingSetting>()
            .Bind(configuration.GetSection(MessagingSetting.SectionName))
            .ValidateFluentValidation()
            .ValidateOnStart();

        RegisterEventTypes(builder.Services);

        MessagingSetting setting = configuration
            .GetSection(MessagingSetting.SectionName)
            .Get<MessagingSetting>() ?? new MessagingSetting();

        // Skip: a disabled pipeline keeps the outbox's no-op bus, so nothing reaches a broker.
        if (!setting.Enabled)
        {
            return builder;
        }

        string? connectionString = ResolveConnectionString(configuration);

        // Fail: a broker transport without an address cannot start, and falling back silently
        // would hide the misconfiguration until the first publish.
        if (setting.Transport == MessagingTransport.RabbitMq &&
            string.IsNullOrWhiteSpace(connectionString))
        {
            // Boundary: RabbitMQ was selected without an address; fail before serving traffic.
            throw new InvalidOperationException(
                "Messaging:Transport is RabbitMq but no connection string was found in " +
                "'ConnectionStrings:messaging', 'ConnectionStrings:rabbitmq', or 'Messaging:ConnectionString'.");
        }

        builder.UseWolverine(options => ConfigureWolverine(options, setting, connectionString));

        // Register: this concrete registration is intentionally last so it wins over the
        // outbox's TryAdd no-op registration. The adapter is scoped because Wolverine's
        // IMessageBus is scoped to the current message/DI scope.
        builder.Services.AddScoped<IEventBus, WolverineEventBus>();

        if (setting.Transport == MessagingTransport.RabbitMq && connectionString is not null)
        {
            // Reuse: the health-check package does not dispose the connection it is given,
            // so DI owns a single long-lived connection instead of opening one per probe.
            builder.Services.TryAddSingleton<IConnection>(_ =>
                new ConnectionFactory
                {
                    Uri = new Uri(connectionString),
                }.CreateConnectionAsync().GetAwaiter().GetResult());

            builder.Services.AddHealthChecks()
                .AddRabbitMQ(
                    provider => Task.FromResult(provider.GetRequiredService<IConnection>()),
                    tags: ["ready", "messaging"]);
        }

        return builder;
    }

    /// <summary>Resolves the bus once so a broken Wolverine topology fails at startup.</summary>
    /// <param name="app">The host application.</param>
    /// <returns>The same application, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <c>null</c>.</exception>
    public static WebApplication UseMessaging(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        MessagingSetting setting = app.Configuration
            .GetSection(MessagingSetting.SectionName)
            .Get<MessagingSetting>() ?? new MessagingSetting();

        // Skip: with messaging disabled there is no Wolverine bus to resolve.
        if (!setting.Enabled)
        {
            return app;
        }

        // Initialize: resolving the bus validates the composition before the first request.
        using IServiceScope scope = app.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        return app;
    }

    /// <summary>Replaces the persistence registry with one that also knows integration events.</summary>
    /// <param name="services">The host's service collection.</param>
    private static void RegisterEventTypes(IServiceCollection services)
    {
        // Replace: the persistence registration only scans Core and therefore cannot resolve
        // integration payloads when a consumer materialises them.
        services.RemoveAll<EventTypeRegistry>();
        services.AddSingleton<EventTypeRegistry>(_ =>
        {
            var registry = new EventTypeRegistry(
                typeof(IDomainEvent).Assembly,
                typeof(MessagingComposition).Assembly);

            registry.Register<TodoCompletedIntegration>(TodoCompletedEventName, EventVersion);

            return registry;
        });
    }

    /// <summary>Configures Wolverine discovery, retries, and the selected transport.</summary>
    /// <param name="options">The Wolverine options.</param>
    /// <param name="setting">The bound messaging settings.</param>
    /// <param name="connectionString">The resolved broker address, when one exists.</param>
    private static void ConfigureWolverine(
        WolverineOptions options,
        MessagingSetting setting,
        string? connectionString)
    {
        // Discover: events live in Application and consumers live in Infrastructure.
        options.Discovery.IncludeAssembly(typeof(IntegrationEvent).Assembly);
        options.Discovery.IncludeAssembly(typeof(MessagingComposition).Assembly);

        // Retry: exponential backoff keeps a failing consumer from hammering the broker.
        options.OnException<Exception>().RetryWithCooldown(RetryDelays(setting));

        // Skip: the local transport never contacts a broker.
        if (setting.Transport != MessagingTransport.RabbitMq ||
            string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var transport = options
            .UseRabbitMq(new Uri(connectionString))
            .AutoProvision()
            .ConfigureChannelCreation(channel =>
            {
                // Confirm: the outbox marks a row processed only after the broker acknowledges it.
                channel.PublisherConfirmationsEnabled = true;
                channel.PublisherConfirmationTrackingEnabled = true;
                channel.ConsumerDispatchConcurrency = (ushort)Math.Clamp(
                    setting.MaxConcurrency,
                    1,
                    ushort.MaxValue);
            });

        string queueName =
            $"{setting.QueuePrefix}.{TodoCompletedEventName}.{nameof(Handlers.TodoCompletedIntegrationHandler)}";
        transport
            .BindExchange(setting.ExchangeName, Wolverine.RabbitMQ.ExchangeType.Topic)
            .ToQueue(queueName, TodoCompletedEventName);

        // Publish: one topic exchange; the routing key is the stable event name.
        options
            .PublishMessage<TodoCompletedIntegration>()
            .ToRabbitRoutingKey(
                setting.ExchangeName,
                TodoCompletedEventName,
                exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Topic);

        // Listen: one queue per consumer, tuned by prefetch and concurrency settings.
        options
            .ListenToRabbitQueue(queueName)
            .PreFetchCount((ushort)setting.PrefetchCount);
    }

    /// <summary>Resolves the broker connection string from the supported configuration keys.</summary>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The first configured value, or <c>null</c> when none is present.</returns>
    private static string? ResolveConnectionString(IConfiguration configuration)
    {
        // Fallback: 'rabbitmq' is the Aspire resource name injected by the AppHost.
        string? connectionString = configuration.GetConnectionString("messaging");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration.GetConnectionString("rabbitmq");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        return configuration[$"{MessagingSetting.SectionName}:ConnectionString"];
    }

    /// <summary>Builds the exponential retry schedule from the settings.</summary>
    /// <param name="setting">The bound messaging settings.</param>
    /// <returns>One delay per delivery attempt, starting at the configured base delay.</returns>
    private static TimeSpan[] RetryDelays(MessagingSetting setting)
    {
        int attempts = Math.Clamp(
            setting.MaxAttempts,
            1,
            MessagingDefaults.Constraints.MaxAttemptsMax);
        int baseDelay = Math.Max(setting.RetryDelaySeconds, 1);

        return [.. Enumerable
            .Range(0, attempts)
            .Select(attempt => TimeSpan.FromSeconds(baseDelay * Math.Pow(2, attempt)))];
    }
}
