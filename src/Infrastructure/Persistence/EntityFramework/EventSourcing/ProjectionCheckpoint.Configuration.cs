using Application.Common.EventSourcing;
using Application.Common.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityFramework.EventSourcing;

/// <summary>Maps <see cref="ProjectionCheckpoint"/> to the checkpoint table.</summary>
public sealed class ProjectionCheckpointConfiguration : IEntityTypeConfiguration<ProjectionCheckpoint>
{
    public void Configure(EntityTypeBuilder<ProjectionCheckpoint> builder)
    {
        builder.ToTable("projection_checkpoints", PersistenceConstant.Schema.Messaging);
        builder.HasKey(checkpoint => checkpoint.Projection);
    }
}
