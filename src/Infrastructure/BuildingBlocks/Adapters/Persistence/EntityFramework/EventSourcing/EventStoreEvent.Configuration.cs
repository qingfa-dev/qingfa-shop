using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Persistence;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

/// <summary>Maps <see cref="StoredEvent"/> to the event-store table.</summary>
public sealed class EventStoreEventConfiguration : IEntityTypeConfiguration<StoredEvent>
{
    public void Configure(EntityTypeBuilder<StoredEvent> builder)
    {
        builder.ToTable("event_store_events", PersistenceConstant.Schema.Messaging);
        builder.HasKey(stored => new { stored.StreamId, stored.Version });
        builder.Property(stored => stored.Position).UseIdentityByDefaultColumn();
        builder.HasIndex(stored => stored.Position).IsUnique();
        builder.HasIndex(stored => stored.EventId).IsUnique();
        builder.Property(stored => stored.Payload).HasColumnType("jsonb");
        builder.Property(stored => stored.Metadata).HasMetadataConversion();
    }
}
