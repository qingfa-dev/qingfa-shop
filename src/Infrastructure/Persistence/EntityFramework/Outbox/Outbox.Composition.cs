using Application.Common.Messaging.Bus;
using Application.Common.Messaging.Inbox;
using Application.Common.Messaging.Outbox;
using Application.Common.Validation.FluentValidation;

using FluentValidation;

using Infrastructure.Events;
using Infrastructure.Persistence.EntityFramework.Inbox;
using Infrastructure.Persistence.EntityFramework.Outbox.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.EntityFramework.Outbox;

/// <summary>Registers the outbox dispatcher, worker, inbox, and health check.</summary>
public static class OutboxComposition
{
    /// <summary>Adds the reliable-event pipeline to the host.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configuration">Configuration holding the <c>Outbox</c> section.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddOutbox(
        this WebApplicationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.AddSingleton<IValidator<OutboxSetting>, OutboxSettingValidator>();
        builder.Services
            .AddOptions<OutboxSetting>()
            .Bind(configuration.GetSection(OutboxSetting.SectionName))
            .ValidateFluentValidation()
            .ValidateOnStart();

        builder.Services.TryAddSingleton(sp =>
            sp.GetRequiredService<IOptions<OutboxSetting>>().Value);
        builder.Services.TryAddSingleton(TimeProvider.System);

        builder.Services.TryAddScoped<IOutboxStore, EfCoreOutboxStore>();
        builder.Services.TryAddScoped<IInboxStore, EfCoreInboxStore>();
        builder.Services.TryAddScoped<IOutboxDispatcher, OutboxDispatcher>();
        builder.Services.TryAddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
        builder.Services.TryAddSingleton<IEventBus, NoOpEventBus>();

        builder.Services.AddHostedService<OutboxWorker>();
        builder.Services.AddHealthChecks()
            .AddCheck<OutboxHealthCheck>("outbox", tags: ["ready"]);

        return builder;
    }
}
