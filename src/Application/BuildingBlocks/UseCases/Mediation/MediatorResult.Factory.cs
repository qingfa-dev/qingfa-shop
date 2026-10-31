using System.Reflection;

namespace Application.BuildingBlocks.UseCases.Mediation;

/// <summary>
/// Bridges the generic pipeline response type back to the Core result factory.
/// </summary>
/// <remarks>
/// Pipeline behaviors are generic over <c>TResponse</c>, while failures must be
/// built through the non-generic or generic <see cref="Result"/> factory. Both
/// expose the same <c>Fail(List&lt;Error&gt;)</c> shape, so the generic branch is
/// resolved once per closed type.
/// </remarks>
internal static class MediatorResult
{
    /// <summary>
    /// Creates a failed <typeparamref name="TResponse"/> carrying the supplied errors.
    /// </summary>
    /// <typeparam name="TResponse">The closed response type; must be a <see cref="Result"/>.</typeparam>
    /// <param name="errors">The errors describing the failure.</param>
    /// <returns>A failed result of the requested response type.</returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TResponse"/> is not a <see cref="Result"/>.
    /// </exception>
    public static TResponse Failure<TResponse>(IReadOnlyList<Error> errors)
        where TResponse : IResult<Error>
    {
        List<Error> failureErrors = [.. errors];

        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Fail(failureErrors);
        }

        // Resolve: Result<T> hides the non-generic factory, so the closed type owns
        // the exact overload that preserves T's value-less failure shape.
        MethodInfo fail = typeof(TResponse).GetMethod(
                nameof(Result.Fail),
                [typeof(List<Error>)])
            ?? throw new InvalidOperationException(
                $"'{typeof(TResponse).FullName}' does not expose a Result failure factory.");

        return (TResponse)fail.Invoke(null, [failureErrors])!;
    }
}