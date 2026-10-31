using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.UseCases.Validation.FluentValidation;

using FluentValidation;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Projections;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

/// <summary>Registers the event store, snapshots and projection checkpoints.</summary>
public static class EventStoreComposition
{
    public static WebApplicationBuilder AddEventStore(
        this WebApplicationBuilder builder,
        IConfiguration configuration,
        IEnumerable<IEventStoreModule> modules)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(modules);

        builder.Services.AddSingleton<IValidator<EventStoreSetting>, EventStoreSettingValidator>();
        builder.Services
            .AddOptions<EventStoreSetting>()
            .Bind(configuration.GetSection(EventStoreSetting.SectionName))
            .ValidateFluentValidation()
            .ValidateOnStart();

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddSingleton(sp =>
            sp.GetRequiredService<IOptions<EventStoreSetting>>().Value);
        builder.Services.TryAddSingleton<ISnapshotPolicy>(sp =>
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

        foreach (IEventStoreModule module in modules)
            module.Configure(builder.Services);

        return builder;
    }
}
