using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

/// <summary>Maps <see cref="ProjectionCheckpoint"/> to the checkpoint table.</summary>
public sealed class ProjectionCheckpointConfiguration : IEntityTypeConfiguration<ProjectionCheckpoint>
{
    public void Configure(EntityTypeBuilder<ProjectionCheckpoint> builder)
    {
        builder.ToTable("projection_checkpoints", PersistenceConstant.Schema.Messaging);
        builder.HasKey(checkpoint => checkpoint.Projection);
    }
}
