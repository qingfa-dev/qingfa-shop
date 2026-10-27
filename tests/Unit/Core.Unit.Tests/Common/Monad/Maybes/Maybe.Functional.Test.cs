using Core.Common.Monad.Maybes;

namespace Core.Unit.Tests.Common.Monad.Maybes;

public class MaybeFunctionalTest
{
    #region GetValueOrDefault

    [Fact]
    public void GetValueOrDefault_WhenHasValue_ShouldReturnValue()
    {
        var maybe = Maybe<int>.From(42);

        maybe.GetValueOrDefault(0).Should().Be(42);
    }

    [Fact]
    public void GetValueOrDefault_WhenEmpty_ShouldReturnDefault()
    {
        var maybe = Maybe<int>.Empty();

        maybe.GetValueOrDefault(0).Should().Be(0);
    }

    #endregion

    #region GetValueOrThrow

    [Fact]
    public void GetValueOrThrow_WhenHasValue_ShouldReturnValue()
    {
        var maybe = Maybe<int>.From(42);

        maybe.GetValueOrThrow(new InvalidOperationException("no value")).Should().Be(42);
    }

    [Fact]
    public void GetValueOrThrow_WhenEmpty_ShouldThrow()
    {
        var maybe = Maybe<int>.Empty();

        Action act = () => maybe.GetValueOrThrow(new InvalidOperationException("no value"));
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Map

    [Fact]
    public void Map_WhenHasValue_ShouldTransform()
    {
        var maybe = Maybe<int>.From(42);

        var result = maybe.Map(x => x * 2);

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(84);
    }

    [Fact]
    public void Map_WhenEmpty_ShouldReturnEmpty()
    {
        var maybe = Maybe<int>.Empty();

        var result = maybe.Map(x => x * 2);

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Map_NullMapper_ShouldReturnEmpty()
    {
        var maybe = Maybe<int>.From(42);

        var result = maybe.Map<int>(null!);

        result.HasValue.Should().BeFalse();
    }

    #endregion

    #region Bind

    [Fact]
    public void Bind_WhenHasValue_ShouldChain()
    {
        var maybe = Maybe<int>.From(42);

        var result = maybe.Bind(x => Maybe<string>.From(x.ToString()));

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be("42");
    }

    [Fact]
    public void Bind_WhenEmpty_ShouldReturnEmpty()
    {
        var maybe = Maybe<int>.Empty();

        var result = maybe.Bind(x => Maybe<string>.From(x.ToString()));

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Bind_NullMapper_ShouldReturnEmpty()
    {
        var maybe = Maybe<int>.From(42);

        var result = maybe.Bind<string>(null!);

        result.HasValue.Should().BeFalse();
    }

    #endregion

    #region Tap

    [Fact]
    public void Tap_WhenHasValue_ShouldExecuteAction()
    {
        var maybe = Maybe<int>.From(42);
        int captured = 0;

        var result = maybe.Tap(x => captured = x);

        captured.Should().Be(42);
        result.Should().Be(maybe);
    }

    [Fact]
    public void Tap_WhenEmpty_ShouldNotExecuteAction()
    {
        var maybe = Maybe<int>.Empty();
        bool executed = false;

        var result = maybe.Tap(x => executed = true);

        executed.Should().BeFalse();
        result.Should().Be(maybe);
    }

    #endregion

    #region TapNoValue

    [Fact]
    public void TapNoValue_WhenEmpty_ShouldExecuteAction()
    {
        var maybe = Maybe<int>.Empty();
        bool executed = false;

        var result = maybe.TapNoValue(() => executed = true);

        executed.Should().BeTrue();
        result.Should().Be(maybe);
    }

    [Fact]
    public void TapNoValue_WhenHasValue_ShouldNotExecuteAction()
    {
        var maybe = Maybe<int>.From(42);
        bool executed = false;

        var result = maybe.TapNoValue(() => executed = true);

        executed.Should().BeFalse();
        result.Should().Be(maybe);
    }

    #endregion

    #region Match

    [Fact]
    public void Match_WhenHasValue_ShouldCallSome()
    {
        var maybe = Maybe<int>.From(42);

        var result = maybe.Match(x => $"Value: {x}", () => "None");

        result.Should().Be("Value: 42");
    }

    [Fact]
    public void Match_WhenEmpty_ShouldCallNone()
    {
        var maybe = Maybe<int>.Empty();

        var result = maybe.Match(x => $"Value: {x}", () => "None");

        result.Should().Be("None");
    }

    #endregion

    #region Ensure

    [Fact]
    public void Ensure_PredicateTrue_ShouldReturnSame()
    {
        var maybe = Maybe<int>.From(42);

        var result = maybe.Ensure(x => x > 0, -1);

        result.Should().Be(maybe);
    }

    [Fact]
    public void Ensure_PredicateFalse_ShouldReturnErrorValue()
    {
        var maybe = Maybe<int>.From(42);

        var result = maybe.Ensure(x => x > 100, -1);

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(-1);
    }

    [Fact]
    public void Ensure_NullPredicate_ShouldReturnSame()
    {
        var maybe = Maybe<int>.From(42);

        var result = maybe.Ensure(null!, -1);

        result.Should().BeSameAs(maybe);
    }

    #endregion
}