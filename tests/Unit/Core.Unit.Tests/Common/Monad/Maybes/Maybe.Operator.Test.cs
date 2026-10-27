using Shared.Foundations.Maybes;

namespace Core.Unit.Tests.Common.Monad.Maybes;

public class MaybeOperatorTest
{
    [Fact]
    public void ImplicitConversion_Value_ShouldCreateMaybe()
    {
        Maybe<int> maybe = 42;

        maybe.HasValue.Should().BeTrue();
        maybe.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitConversion_NullValue_ShouldCreateEmpty()
    {
        Maybe<int?>? maybe = null;

        maybe.Should().BeNull();
    }

    [Fact]
    public void ImplicitConversion_MaybeWithValue_ShouldReturnValue()
    {
        var maybe = Maybe<int>.From(42);

        int value = maybe;

        value.Should().Be(42);
    }

    [Fact]
    public void ImplicitConversion_EmptyMaybe_ShouldThrow()
    {
        var maybe = Maybe<int>.Empty();

        Action act = () => { int value = maybe; };
        act.Should().Throw<InvalidOperationException>();
    }
}