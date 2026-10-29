namespace Core.Common.Domain.Policies;

/// <summary>Base class for Result-returning domain policies.</summary>
public abstract class DomainPolicy<TContext, TResult>
    : IDomainPolicy<TContext, TResult>
{
    /// <inheritdoc />
    public abstract Result<TResult> Evaluate(TContext context);
}