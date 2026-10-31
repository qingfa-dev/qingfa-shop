namespace Core.BuildingBlocks.Functional.Maybes;

public partial record Maybe<T> : IMaybe<T>
{
    #region Functional Methods

    /// <summary>
    /// Returns the contained value or a default value.
    /// </summary>
    /// <param name="defaultValue">The default value to return if no value is present.</param>
    /// <returns>The contained value or the default.</returns>
    public T GetValueOrDefault(T defaultValue)
    {
        return HasValue ? Value! : defaultValue;
    }

    /// <summary>
    /// Returns the contained value or throws an exception.
    /// </summary>
    /// <param name="exception">The exception to throw if no value is present.</param>
    /// <returns>The contained value.</returns>
    /// <exception cref="Exception">Thrown when there is no value.</exception>
    public T GetValueOrThrow(Exception exception)
    {
        return HasValue ? Value! : throw exception;
    }

    /// <summary>
    /// Maps the contained value to a new value.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new Maybe with the mapped value.</returns>
    public Maybe<TResult> Map<TResult>(Func<T, TResult>? mapper)
    {
        if (mapper is null)
        {
            return Maybe<TResult>.Empty();
        }

        return HasValue ? Maybe<TResult>.From(mapper(Value!)) : Maybe<TResult>.Empty();
    }

    /// <summary>
    /// Chains operations that return a Maybe.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>The result of the mapping function.</returns>
    public Maybe<TResult> Bind<TResult>(Func<T, Maybe<TResult>>? mapper)
    {
        if (mapper is null)
        {
            return Maybe<TResult>.Empty();
        }

        return HasValue ? mapper(Value!) : Maybe<TResult>.Empty();
    }

    /// <summary>
    /// Executes an action if a value is present.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original Maybe.</returns>
    public Maybe<T> Tap(Action<T>? action)
    {
        if (action is not null && HasValue)
        {
            action(Value!);
        }

        return this;
    }

    /// <summary>
    /// Executes an action if there is no value.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original Maybe.</returns>
    public Maybe<T> TapNoValue(Action? action)
    {
        if (action is not null && HasNoValue)
        {
            action();
        }

        return this;
    }

    /// <summary>
    /// Matches the Maybe to one of two functions.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="some">The function to call when a value is present.</param>
    /// <param name="none">The function to call when no value is present.</param>
    /// <returns>The result of the matched function.</returns>
    public TResult Match<TResult>(Func<T, TResult>? some, Func<TResult>? none)
    {
        if (some is null || none is null)
        {
            return default!;
        }

        return HasValue ? some(Value!) : none();
    }

    /// <summary>
    /// Ensures the predicate returns true for the contained value.
    /// Returns an empty Maybe if the predicate returns false.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>The original Maybe if the predicate returns true, otherwise an empty Maybe.</returns>
    public Maybe<T> Ensure(Func<T, bool>? predicate)
    {
        if (predicate is null)
        {
            return this;
        }

        if (HasNoValue)
        {
            return this;
        }

        return predicate(Value!) ? this : Empty();
    }

    /// <summary>
    /// Ensures the predicate returns true for the contained value.
    /// Returns a failure Maybe with the specified error if the predicate returns false.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="error">The error to return when the predicate returns false.</param>
    /// <returns>The original Maybe if the predicate returns true, otherwise a failure Maybe.</returns>
    public Maybe<T> Ensure(Func<T, bool>? predicate, T error)
    {
        if (predicate is null)
        {
            return this;
        }

        if (HasNoValue)
        {
            return this;
        }

        return predicate(Value!) ? this : From(error!);
    }

    #endregion
}
