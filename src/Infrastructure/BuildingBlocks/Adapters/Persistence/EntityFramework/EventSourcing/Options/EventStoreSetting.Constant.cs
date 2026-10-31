namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing.Options;

/// <summary>Defaults for <see cref="EventStoreSetting"/>.</summary>
public static class EventStoreSettingConstant
{
    public static class Defaults
    {
        public const bool Enabled = true;
        public const string Provider = "Postgres";
        public const int BatchSize = 500;
        public const int SnapshotInterval = 50;
        public const int PollingIntervalSeconds = 2;
    }
}
