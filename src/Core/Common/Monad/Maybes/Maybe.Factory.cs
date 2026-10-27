namespace Core.Common.Monad.Maybes;

public partial record Maybe<T> : IMaybe<T>
{
    #region Factory Methods

    /// <summary>
    /// Creates a Maybe with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A Maybe containing the value.</returns>
    public static Maybe<T> From(T value)
    {
        return new Maybe<T>(value, hasValue: true);
    }

    /// <summary>
    /// Creates an empty Maybe.
    /// </summary>
    /// <returns>An empty Maybe.</returns>
    public static Maybe<T> Empty()
    {
        return new Maybe<T>(default, hasValue: false);
    }

    #endregion
}