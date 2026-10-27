namespace Application.Common.Contexts;

/// <summary>Supplies the actor performing the current request.</summary>
/// <remarks>
/// The actor is optional; audit metadata simply stays empty when no actor is set.
/// </remarks>
public interface IActorContext
{
    /// <summary>Gets the current actor, or <c>null</c> when anonymous.</summary>
    string? Actor { get; }
}
