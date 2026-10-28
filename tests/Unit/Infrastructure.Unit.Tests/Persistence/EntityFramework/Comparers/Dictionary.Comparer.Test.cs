using Infrastructure.Persistence.EntityFramework.Conventions;

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Comparers;

/// <summary>Unit tests for the dictionary value comparer used by the JSON convention.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class DictionaryComparerTest
{
    private static ValueComparer<Dictionary<string, object>> Comparer
        => DictionaryAsJsonConvention.DictionaryComparer;

    [Fact(DisplayName = "Equals should treat the same instance as equal")]
    public void Equals_WhenSameInstance_ShouldReturnTrue()
    {
        // Arrange
        var value = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        bool equal = Comparer.Equals(value, value);

        // Assert
        equal.Should().BeTrue();
    }

    [Fact(DisplayName = "Equals should treat two nulls as equal")]
    public void Equals_WhenBothNull_ShouldReturnTrue()
    {
        // Act & Assert
        Comparer.Equals(null, null).Should().BeTrue();
    }

    [Fact(DisplayName = "Equals should fail when only one side is null")]
    public void Equals_WhenOneSideNull_ShouldReturnFalse()
    {
        // Arrange
        var value = new Dictionary<string, object>();

        // Act & Assert
        Comparer.Equals(value, null).Should().BeFalse();
        Comparer.Equals(null, value).Should().BeFalse();
    }

    [Fact(DisplayName = "Equals should compare keys case-insensitively")]
    public void Equals_WhenKeysDifferOnlyByCase_ShouldReturnTrue()
    {
        // Arrange
        var left = new Dictionary<string, object> { ["Key"] = "value" };
        var right = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        bool equal = Comparer.Equals(left, right);

        // Assert
        equal.Should().BeTrue();
    }

    [Fact(DisplayName = "Equals should fail when the entry counts differ")]
    public void Equals_WhenCountsDiffer_ShouldReturnFalse()
    {
        // Arrange
        var left = new Dictionary<string, object> { ["a"] = 1 };
        var right = new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 };

        // Act & Assert
        Comparer.Equals(left, right).Should().BeFalse();
    }

    [Fact(DisplayName = "Equals should fail when a value differs")]
    public void Equals_WhenValuesDiffer_ShouldReturnFalse()
    {
        // Arrange
        var left = new Dictionary<string, object> { ["a"] = 1 };
        var right = new Dictionary<string, object> { ["a"] = 2 };

        // Act & Assert
        Comparer.Equals(left, right).Should().BeFalse();
    }

    [Fact(DisplayName = "GetHashCode should be stable for equivalent dictionaries")]
    public void GetHashCode_WhenEquivalent_ShouldMatch()
    {
        // Arrange
        var left = new Dictionary<string, object> { ["Key"] = "value" };
        var right = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        int leftHash = Comparer.GetHashCode(left);
        int rightHash = Comparer.GetHashCode(right);

        // Assert
        leftHash.Should().Be(rightHash);
    }

    [Fact(DisplayName = "GetHashCode should ignore insertion order")]
    public void GetHashCode_WhenInsertionOrderDiffers_ShouldMatch()
    {
        // Arrange
        var left = new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 };
        var right = new Dictionary<string, object> { ["b"] = 2, ["a"] = 1 };

        // Act
        int leftHash = Comparer.GetHashCode(left);
        int rightHash = Comparer.GetHashCode(right);

        // Assert
        leftHash.Should().Be(rightHash);
    }

    [Fact(DisplayName = "GetHashCode should change when a value changes")]
    public void GetHashCode_WhenValueChanges_ShouldDiffer()
    {
        // Arrange
        var value = new Dictionary<string, object> { ["key"] = "value" };
        int before = Comparer.GetHashCode(value);

        // Act
        value["key"] = "other";

        // Assert
        Comparer.GetHashCode(value).Should().NotBe(before);
    }

    [Fact(DisplayName = "Snapshot should be an independent dictionary with case-insensitive lookup")]
    public void Snapshot_ShouldReturnIndependentCaseInsensitiveCopy()
    {
        // Arrange
        var value = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        Dictionary<string, object> snapshot = Comparer.Snapshot(value);
        value["key"] = "changed";

        // Assert
        snapshot.Should().NotBeSameAs(value);
        snapshot["KEY"].Should().Be("value");
    }
}
