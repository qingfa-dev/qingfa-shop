using Core.BuildingBlocks.Functional.Maybes;

namespace Core.Unit.Tests.BuildingBlocks.Functional.Maybes;

public class MaybeFactoryTest
{
    #region From

    [Fact]
    public void From_ShouldCreateMaybeWithValue()
    {
        var maybe = Maybe<int>.From(42);

        maybe.HasValue.Should().BeTrue();
        maybe.Value.Should().Be(42);
    }

    [Fact]
    public void From_String_ShouldCreateMaybeWithValue()
    {
        var maybe = Maybe<string>.From("hello");

        maybe.HasValue.Should().BeTrue();
        maybe.Value.Should().Be("hello");
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldCreateMaybeWithoutValue()
    {
        var maybe = Maybe<int>.Empty();

        maybe.HasValue.Should().BeFalse();
        maybe.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Empty_Value_ShouldThrow()
    {
        var maybe = Maybe<int>.Empty();

        Action act = () => _ = maybe.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion
}