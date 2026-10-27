using Shared.Foundations.Maybes;

namespace Core.Unit.Tests.Common.Monad.Maybes;

public class MaybeInterfaceTest
{
    [Fact]
    public void IMaybe_Implicts_ShouldBeAssignable()
    {
        var withValue = Maybe<int>.From(42);
        var empty = Maybe<int>.Empty();

        withValue.Should().BeAssignableTo<IMaybe<int>>();
        empty.Should().BeAssignableTo<IMaybe<int>>();
    }

    [Fact]
    public void IMaybe_ThroughInterface_ShouldExposeHasValue()
    {
        var withValue = Maybe<int>.From(42);
        var empty = Maybe<int>.Empty();

        ((IMaybe<int>)withValue).HasValue.Should().BeTrue();
        ((IMaybe<int>)empty).HasValue.Should().BeFalse();
    }

    [Fact]
    public void IMaybe_ThroughInterface_ShouldExposeHasNoValue()
    {
        var withValue = Maybe<int>.From(42);
        var empty = Maybe<int>.Empty();

        ((IMaybe<int>)withValue).HasNoValue.Should().BeFalse();
        ((IMaybe<int>)empty).HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void IMaybe_ThroughInterface_ShouldExposeValue()
    {
        var withValue = Maybe<int>.From(42);

        ((IMaybe<int>)withValue).Value.Should().Be(42);
    }
}