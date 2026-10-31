using Core.BuildingBlocks.Modeling.Concerns.Foundation.Versionable;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Conventions;

/// <summary>Tests for the Version concurrency-token convention.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class VersionableConfigurationTest
{
    private sealed class VersionedEntity : IVersionable
    {
        public Guid Id { get; set; }
        public long Version { get; set; }
    }

    private sealed class UnversionedEntity
    {
        public Guid Id { get; set; }
        public long Version { get; set; }
    }

    [Fact]
    public void Apply_WhenEntityIsVersionable_MarksVersionAsConcurrencyToken()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<VersionedEntity>(builder =>
        {
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Id).ValueGeneratedNever();
            builder.Property(entity => entity.Version);
        });

        // Act
        VersionableConfiguration.Apply(modelBuilder);

        // Assert
        var property = modelBuilder.Model
            .FindEntityType(typeof(VersionedEntity))!
            .FindProperty(nameof(IVersionable.Version))!;
        property.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void Apply_WhenEntityIsNotVersionable_LeavesVersionAlone()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<UnversionedEntity>(builder =>
        {
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Id).ValueGeneratedNever();
            builder.Property(entity => entity.Version);
        });

        // Act
        VersionableConfiguration.Apply(modelBuilder);

        // Assert
        var property = modelBuilder.Model
            .FindEntityType(typeof(UnversionedEntity))!
            .FindProperty(nameof(UnversionedEntity.Version))!;
        property.IsConcurrencyToken.Should().BeFalse();
    }
}