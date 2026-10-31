namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;

/// <summary>Configuration for the event store and projections.</summary>
public sealed class EventStoreSetting
{
    public const string SectionName = "EventStore";

    public bool Enabled { get; set; } = EventStoreSettingConstant.Defaults.Enabled;
    public string Provider { get; set; } = EventStoreSettingConstant.Defaults.Provider;
    public int BatchSize { get; set; } = EventStoreSettingConstant.Defaults.BatchSize;
    public int SnapshotInterval { get; set; } = EventStoreSettingConstant.Defaults.SnapshotInterval;
    public int PollingIntervalSeconds { get; set; } = EventStoreSettingConstant.Defaults.PollingIntervalSeconds;
}
