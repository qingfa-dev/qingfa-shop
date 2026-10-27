using Infrastructure.Persistence.EntityFramework.Comparers;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Comparers;

/// <summary>Unit tests for <see cref="MetadataValueComparer"/>.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class MetadataValueComparerTest
{
    [Fact(DisplayName = "Equals should treat the same instance as equal")]
    public void Equals_WhenSameInstance_ShouldReturnTrue()
    {
        // Arrange
        var metadata = new DefaultMetadata { ["key"] = "value" };

        // Act
        bool equal = MetadataValueComparer.Instance.Equals(metadata, metadata);

        // Assert
        equal.Should().BeTrue();
    }

    [Fact(DisplayName = "Equals should treat two nulls as equal")]
    public void Equals_WhenBothNull_ShouldReturnTrue()
    {
        // Act
        bool equal = MetadataValueComparer.Instance.Equals(null, null);

        // Assert
        equal.Should().BeTrue();
    }

    [Fact(DisplayName = "Equals should fail when only one side is null")]
    public void Equals_WhenOneSideNull_ShouldReturnFalse()
    {
        // Arrange
        var metadata = new DefaultMetadata();

        // Act & Assert
        MetadataValueComparer.Instance.Equals(metadata, null).Should().BeFalse();
        MetadataValueComparer.Instance.Equals(null, metadata).Should().BeFalse();
    }

    [Fact(DisplayName = "Equals should fail when the entry counts differ")]
    public void Equals_WhenCountsDiffer_ShouldReturnFalse()
    {
        // Arrange
        var left = new DefaultMetadata { ["a"] = 1 };
        var right = new DefaultMetadata { ["a"] = 1, ["b"] = 2 };

        // Act
        bool equal = MetadataValueComparer.Instance.Equals(left, right);

        // Assert
        equal.Should().BeFalse();
    }

    [Fact(DisplayName = "Equals should fail when values differ")]
    public void Equals_WhenValuesDiffer_ShouldReturnFalse()
    {
        // Arrange
        var left = new DefaultMetadata { ["a"] = 1 };
        var right = new DefaultMetadata { ["a"] = 2 };

        // Act
        bool equal = MetadataValueComparer.Instance.Equals(left, right);

        // Assert
        equal.Should().BeFalse();
    }

    [Fact(DisplayName = "Equals should compare keys case-insensitively and values by equality")]
    public void Equals_WhenEquivalent_ShouldReturnTrue()
    {
        // Arrange
        var left = new DefaultMetadata { ["Key"] = "value", ["other"] = null! };
        var right = new DefaultMetadata { ["key"] = "value", ["OTHER"] = null! };

        // Act
        bool equal = MetadataValueComparer.Instance.Equals(left, right);

        // Assert
        equal.Should().BeTrue();
    }

    [Fact(DisplayName = "Equals should fail when a key is missing on the right")]
    public void Equals_WhenKeyMissing_ShouldReturnFalse()
    {
        // Arrange
        var left = new DefaultMetadata { ["a"] = 1 };
        var right = new DefaultMetadata { ["b"] = 1 };

        // Act
        bool equal = MetadataValueComparer.Instance.Equals(left, right);

        // Assert
        equal.Should().BeFalse();
    }

    [Fact(DisplayName = "GetHashCode should be stable for equivalent metadata")]
    public void GetHashCode_WhenEquivalent_ShouldMatch()
    {
        // Arrange
        var left = new DefaultMetadata { ["Key"] = "value", ["other"] = null! };
        var right = new DefaultMetadata { ["key"] = "value", ["OTHER"] = null! };

        // Act
        int leftHash = MetadataValueComparer.Instance.GetHashCode(left);
        int rightHash = MetadataValueComparer.Instance.GetHashCode(right);

        // Assert
        leftHash.Should().Be(rightHash);
    }

    [Fact(DisplayName = "GetHashCode should change when a value changes")]
    public void GetHashCode_WhenValueChanges_ShouldDiffer()
    {
        // Arrange
        var metadata = new DefaultMetadata { ["key"] = "value" };
        int before = MetadataValueComparer.Instance.GetHashCode(metadata);

        // Act
        metadata["key"] = "other";
        int after = MetadataValueComparer.Instance.GetHashCode(metadata);

        // Assert
        after.Should().NotBe(before);
    }

    [Fact(DisplayName = "Snapshot should return an independent copy")]
    public void Snapshot_ShouldReturnIndependentCopy()
    {
        // Arrange
        var metadata = new DefaultMetadata { ["key"] = "value" };

        // Act
        DefaultMetadata snapshot = MetadataValueComparer.Instance.Snapshot(metadata);
        metadata["key"] = "changed";

        // Assert
        snapshot.Should().NotBeSameAs(metadata);
        snapshot["key"].Should().Be("value");
    }
}
