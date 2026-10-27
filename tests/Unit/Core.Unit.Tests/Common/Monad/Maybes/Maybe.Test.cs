using Core.Common.Monad.Maybes;

namespace Core.Unit.Tests.Common.Monad.Maybes;

public class MaybeTest
{
    #region Properties

    [Fact]
    public void HasValue_WhenHasValue_ShouldBeTrue()
    {
        var maybe = Maybe<int>.From(42);

        maybe.HasValue.Should().BeTrue();
        maybe.HasNoValue.Should().BeFalse();
    }

    [Fact]
    public void HasValue_WhenEmpty_ShouldBeFalse()
    {
        var maybe = Maybe<int>.Empty();

        maybe.HasValue.Should().BeFalse();
        maybe.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Value_WhenHasValue_ShouldReturnValue()
    {
        var maybe = Maybe<int>.From(42);

        maybe.Value.Should().Be(42);
    }

    [Fact]
    public void Value_WhenEmpty_ShouldThrowInvalidOperationException()
    {
        var maybe = Maybe<int>.Empty();

        Action act = () => _ = maybe.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_WhenHasValue_ShouldReturnValueFormat()
    {
        var maybe = Maybe<int>.From(42);

        maybe.ToString().Should().Be("Maybe(42)");
    }

    [Fact]
    public void ToString_WhenEmpty_ShouldReturnNoneFormat()
    {
        var maybe = Maybe<int>.Empty();

        maybe.ToString().Should().Be("Maybe(None)");
    }

    #endregion
}