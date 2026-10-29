using System.Linq.Expressions;
using Application.Common.Querying;

namespace Application.Unit.Tests.Common.Querying;

public sealed class QueryResultTest
{
    private sealed record Item(string Name);

    [Fact(DisplayName = "Typed query should carry the source query")]
    public void TypedQuery_ShouldCarrySource()
    {
        var source = new Query<Item>();

        var typed = new Query<Item, string>(source);

        typed.Source.Should().BeSameAs(source);
    }

    [Fact(DisplayName = "Projected query should be assignable to the typed query")]
    public void ProjectedQuery_ShouldBeAssignableToTypedQuery()
    {
        var source = new Query<Item>();
        Expression<Func<Item, string>> projection = item => item.Name;

        var projected = new ProjectedQuery<Item, string>(source, projection);

        projected.Should().BeAssignableTo<Query<Item, string>>();
        projected.Projection.Should().BeSameAs(projection);
    }
}
