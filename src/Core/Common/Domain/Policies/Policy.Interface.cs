namespace Core.Common.Domain.Policies;

/// <summary>Named business decision that spans entities or aggregates.</summary>
/// <typeparam name="TContext">The inputs the decision needs.</typeparam>
/// <typeparam name="TResult">The value produced on success.</typeparam>
public interface IDomainPolicy<in TContext, TResult>
{
    /// <summary>Evaluates the policy without side effects.</summary>
    Result<TResult> Evaluate(TContext context);
}