using Application.Common.EventSourcing;
using Application.Common.Validation.FluentValidation;

using FluentValidation;

using Infrastructure.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.Persistence.EntityFramework.Projections;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.EntityFramework.EventSourcing;

/// <summary>Registers the event store, snapshots and projection checkpoints.</summary>
public static class EventStoreComposition
{
    public static WebApplicationBuilder AddEventStore(
        this WebApplicationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.AddSingleton<IValidator<EventStoreSetting>, EventStoreSettingValidator>();
        builder.Services
            .AddOptions<EventStoreSetting>()
            .Bind(configuration.GetSection(EventStoreSetting.SectionName))
            .ValidateFluentValidation()
            .ValidateOnStart();

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddSingleton(sp =>
        {
            EventStoreSetting setting = sp.GetRequiredService<IOptions<EventStoreSetting>>().Value;
            return new IntervalSnapshotPolicy(setting.SnapshotInterval);
        });

        builder.Services.TryAddScoped<IEventStore, PostgresEventStore>();
        builder.Services.TryAddScoped<ISnapshotStore, EfCoreSnapshotStore>();
        builder.Services.TryAddScoped<IProjectionCheckpointStore, EfCoreProjectionCheckpointStore>();
        builder.Services.TryAddScoped(typeof(IEventSourcedRepository<,>), typeof(EventSourcedRepository<,>));
        builder.Services.TryAddScoped<ProjectionRunner>();
        builder.Services.AddHostedService<ProjectionWorker>();

        return builder;
    }
}
