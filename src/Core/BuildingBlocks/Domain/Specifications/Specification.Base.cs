using System.Linq.Expressions;

namespace Core.BuildingBlocks.Domain.Specifications;

/// <summary>Base class for composable specifications.</summary>
/// <typeparam name="T">The candidate type.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <inheritdoc />
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <inheritdoc />
    public bool IsSatisfiedBy(T candidate) => ToExpression().Compile()(candidate);

    /// <summary>Combines this specification with another using logical AND.</summary>
    public Specification<T> And(ISpecification<T> other)
        => new AndSpecification<T>(this, Require(other));

    /// <summary>Combines this specification with another using logical OR.</summary>
    public Specification<T> Or(ISpecification<T> other)
        => new OrSpecification<T>(this, Require(other));

    /// <summary>Negates this specification.</summary>
    public Specification<T> Not() => new NotSpecification<T>(this);

    private static ISpecification<T> Require(ISpecification<T> specification)
        => specification ?? throw new ArgumentNullException(nameof(specification));

    private static Expression<Func<T, bool>> Combine(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, Expression> merge)
    {
        // Merge: inline both bodies on one parameter so EF Core can translate the result.
        var parameter = Expression.Parameter(typeof(T), "candidate");
        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(
            merge(leftBody!, rightBody!),
            parameter);
    }

    private sealed class AndSpecification<TValue>(
        ISpecification<TValue> left,
        ISpecification<TValue> right) : Specification<TValue>
    {
        public override Expression<Func<TValue, bool>> ToExpression()
            => Combine(left.ToExpression(), right.ToExpression(), Expression.AndAlso);
    }

    private sealed class OrSpecification<TValue>(
        ISpecification<TValue> left,
        ISpecification<TValue> right) : Specification<TValue>
    {
        public override Expression<Func<TValue, bool>> ToExpression()
            => Combine(left.ToExpression(), right.ToExpression(), Expression.OrElse);
    }

    private sealed class NotSpecification<TValue>(ISpecification<TValue> inner)
        : Specification<TValue>
    {
        public override Expression<Func<TValue, bool>> ToExpression()
        {
            Expression<Func<TValue, bool>> expression = inner.ToExpression();

            return Expression.Lambda<Func<TValue, bool>>(
                Expression.Not(expression.Body),
                expression.Parameters);
        }
    }

    private sealed class ParameterReplacer(
        ParameterExpression source,
        ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == source ? target : base.VisitParameter(node);
    }
}