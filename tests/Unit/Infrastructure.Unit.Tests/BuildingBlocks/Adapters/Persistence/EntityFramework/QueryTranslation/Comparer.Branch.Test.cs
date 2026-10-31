using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

/// <summary>Branch tests for the composite grouping-key comparer.</summary>
[Trait("Category", "Unit")]
public sealed class ComparerBranchTest
{
    private static readonly ObjectArrayComparer Comparer = ObjectArrayComparer.Instance;

    [Fact(DisplayName = "Equals should treat the same reference as equal")]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        object?[] values = ["a", 1];

        Comparer.Equals(values, values).Should().BeTrue();
    }

    [Fact(DisplayName = "Equals should treat two nulls as equal")]
    public void Equals_WithBothNull_ShouldReturnTrue()
    {
        Comparer.Equals(null, null).Should().BeTrue();
    }

    [Theory(DisplayName = "Equals should return false for a null side")]
    [InlineData(true)]
    [InlineData(false)]
    public void Equals_WithOneNull_ShouldReturnFalse(bool leftNull)
    {
        object?[] values = ["a"];

        bool result = leftNull
            ? Comparer.Equals(null, values)
            : Comparer.Equals(values, null);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Equals should return false for different lengths")]
    public void Equals_WithDifferentLengths_ShouldReturnFalse()
    {
        Comparer.Equals(["a"], ["a", "b"]).Should().BeFalse();
    }

    [Fact(DisplayName = "Equals should compare every element")]
    public void Equals_WithEqualElements_ShouldReturnTrue()
    {
        Comparer.Equals(["a", 1, null], ["a", 1, null]).Should().BeTrue();
    }

    [Fact(DisplayName = "Equals should reject a mismatched element")]
    public void Equals_WithMismatchedElement_ShouldReturnFalse()
    {
        Comparer.Equals(["a", 1], ["a", 2]).Should().BeFalse();
    }

    [Fact(DisplayName = "GetHashCode should agree for equal arrays")]
    public void GetHashCode_WithEqualArrays_ShouldMatch()
    {
        object?[] left = ["a", 1, null];
        object?[] right = ["a", 1, null];

        Comparer.GetHashCode(left).Should().Be(Comparer.GetHashCode(right));
    }

    [Fact(DisplayName = "GetHashCode should handle a null element")]
    public void GetHashCode_WithNullElement_ShouldNotThrow()
    {
        Action act = () => Comparer.GetHashCode([null]);

        act.Should().NotThrow();
    }
}
