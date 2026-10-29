using System.Linq.Expressions;

using Core.Common.Domain.Specifications;

namespace Core.Unit.Tests.Common.Domain.Specifications;

/// <summary>Tests for composable specifications.</summary>
[Trait("Category", "Unit")]
public sealed class SpecificationTest
{
    private sealed record Item(int Value, string Name);

    private sealed class GreaterThan(int threshold) : Specification<Item>
    {
        public override Expression<Func<Item, bool>> ToExpression()
            => item => item.Value > threshold;
    }

    private sealed class NameStartsWith(string prefix) : Specification<Item>
    {
        public override Expression<Func<Item, bool>> ToExpression()
            => item => item.Name.StartsWith(prefix);
    }

    [Fact]
    public void IsSatisfiedBy_WhenCandidateMatches_ReturnsTrue()
    {
        // Arrange
        var specification = new GreaterThan(5);

        // Act
        bool result = specification.IsSatisfiedBy(new Item(6, "alpha"));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WhenCandidateDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var specification = new GreaterThan(5);

        // Act
        bool result = specification.IsSatisfiedBy(new Item(5, "alpha"));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void And_RequiresBothSpecifications()
    {
        // Arrange
        var specification = new GreaterThan(5).And(new NameStartsWith("a"));
        var items = new[]
        {
            new Item(6, "alpha"),
            new Item(6, "beta"),
            new Item(4, "alpha"),
        }.AsQueryable();

        // Act
        var result = specification.IsSatisfiedBy(new Item(6, "alpha"))
            && !specification.IsSatisfiedBy(new Item(6, "beta"))
            && !specification.IsSatisfiedBy(new Item(4, "alpha"));

        // Assert
        result.Should().BeTrue();
        items.Where(specification).Should().ContainSingle()
            .Which.Should().Be(new Item(6, "alpha"));
    }

    [Fact]
    public void Or_RequiresEitherSpecification()
    {
        // Arrange
        var specification = new GreaterThan(5).Or(new NameStartsWith("a"));
        var items = new[]
        {
            new Item(6, "beta"),
            new Item(4, "alpha"),
            new Item(4, "beta"),
        }.AsQueryable();

        // Act
        var result = items.Where(specification).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(new Item(6, "beta"));
        result.Should().Contain(new Item(4, "alpha"));
    }

    [Fact]
    public void Not_NegatesTheSpecification()
    {
        // Arrange
        var specification = new GreaterThan(5).Not();

        // Act
        bool matches = specification.IsSatisfiedBy(new Item(4, "alpha"));

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void Any_WhenMatchingItemExists_ReturnsTrue()
    {
        // Arrange
        var items = new[] { new Item(1, "a"), new Item(9, "b") }.AsQueryable();

        // Act
        bool result = items.Any(new GreaterThan(5));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void And_WithNullSpecification_Throws()
    {
        // Arrange
        var specification = new GreaterThan(1);

        // Act
        Action act = () => specification.And(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Composition_CanBeChained()
    {
        // Arrange
        var specification = new GreaterThan(5)
            .And(new NameStartsWith("a"))
            .Or(new NameStartsWith("b"))
            .Not();

        // Act
        bool matches = specification.IsSatisfiedBy(new Item(6, "beta"));

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void Any_WhenNothingMatches_ReturnsFalse()
    {
        // Arrange
        var items = new[] { new Item(1, "a"), new Item(2, "b") }.AsQueryable();

        // Act
        bool result = items.Any(new GreaterThan(5));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Not_ThroughQueryable_FiltersMatches()
    {
        // Arrange
        var items = new[] { new Item(1, "a"), new Item(9, "b") }.AsQueryable();

        // Act
        var result = items.Where(new GreaterThan(5).Not()).ToList();

        // Assert
        result.Should().ContainSingle().Which.Should().Be(new Item(1, "a"));
    }

    [Fact]
    public void Where_WithNullQuery_Throws()
    {
        // Arrange
        IQueryable<Item> query = null!;

        // Act
        Action act = () => query.Where(new GreaterThan(1));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Where_WithNullSpecification_Throws()
    {
        // Arrange
        var query = Array.Empty<Item>().AsQueryable();

        // Act
        Action act = () => query.Where((ISpecification<Item>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Any_WithNullSpecification_Throws()
    {
        // Arrange
        var query = Array.Empty<Item>().AsQueryable();

        // Act
        Action act = () => query.Any((ISpecification<Item>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}