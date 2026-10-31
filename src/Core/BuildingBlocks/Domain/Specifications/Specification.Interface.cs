using System.Linq.Expressions;

namespace Core.BuildingBlocks.Domain.Specifications;

/// <summary>Composable predicate over a candidate type.</summary>
/// <typeparam name="T">The candidate type.</typeparam>
public interface ISpecification<T>
{
    /// <summary>Returns the predicate as a translatable expression tree.</summary>
    Expression<Func<T, bool>> ToExpression();

    /// <summary>Evaluates the predicate in memory.</summary>
    bool IsSatisfiedBy(T candidate);
}