using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

/// <summary>Maps <see cref="Snapshot"/> to the snapshot table.</summary>
public sealed class EventStoreSnapshotConfiguration : IEntityTypeConfiguration<Snapshot>
{
    public void Configure(EntityTypeBuilder<Snapshot> builder)
    {
        builder.ToTable("event_store_snapshots", PersistenceConstant.Schema.Messaging);
        builder.HasKey(snapshot => snapshot.StreamId);
        builder.Property(snapshot => snapshot.State).HasColumnType("jsonb");
    }
}
