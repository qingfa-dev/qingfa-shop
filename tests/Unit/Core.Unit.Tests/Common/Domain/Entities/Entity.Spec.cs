using Core.Common.Domain.Entities;

namespace Core.Unit.Tests.Common.Domain.Entities;

/// <summary>
/// Tests for <see cref="Entity{TKey}"/> identity and transient state.
/// </summary>
[Trait("Category", "Unit")]
public class EntitySpec
{
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity()
        {
        }

        public TestEntity(Guid id)
            : base(id)
        {
        }

        public void SetId(Guid id) => Id = id;
    }

    [Fact]
    public void ParameterlessConstructor_ShouldCreateTransientEntity()
    {
        // Arrange
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().Be(Guid.Empty);
        entity.IsTransient.Should().BeTrue();
    }

    [Fact]
    public void IdConstructor_ShouldSetIdAndBePersistent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestEntity(id);

        // Assert
        entity.Id.Should().Be(id);
        entity.IsTransient.Should().BeFalse();
    }

    [Fact]
    public void SetId_ToDefault_ShouldBecomeTransientAgain()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act
        entity.SetId(Guid.Empty);

        // Assert
        entity.IsTransient.Should().BeTrue();
    }

    [Fact]
    public void Entity_ShouldImplementIdentifiableContract()
    {
        // Arrange
        var id = Guid.NewGuid();
        IEntity<Guid> entity = new TestEntity(id);

        // Act
        var identifiedId = entity.Id;

        // Assert
        identifiedId.Should().Be(id);
    }
}