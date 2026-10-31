namespace Core.BuildingBlocks.Domain.Policies;

/// <summary>Base class for Result-returning domain policies.</summary>
/// <typeparam name="TContext">The inputs the decision needs.</typeparam>
/// <typeparam name="TResult">The value produced on success.</typeparam>
public abstract class DomainPolicy<TContext, TResult>
    : IDomainPolicy<TContext, TResult>
{
    /// <summary>Evaluates the policy without side effects.</summary>
    /// <param name="context">The inputs the decision needs.</param>
    /// <returns>
    /// A success carrying the decision value, or a failure with a stable code for an
    /// expected business outcome.
    /// </returns>
    /// <remarks>
    /// Implementations must reject a <see langword="null"/> context with
    /// <see cref="ArgumentNullException"/>: a missing context is a programming error,
    /// never a business failure.
    /// </remarks>
    public abstract Result<TResult> Evaluate(TContext context);
}