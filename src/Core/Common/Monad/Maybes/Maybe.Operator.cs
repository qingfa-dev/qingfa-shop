namespace Shared.Foundations.Maybes;

public partial record Maybe<T> : IMaybe<T>
{
    #region Implicit Operators

    /// <summary>
    /// Implicitly converts a value to a Maybe.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator Maybe<T>(T? value)
    {
        return value is null ? Empty() : From(value);
    }

    /// <summary>
    /// Implicitly converts a Maybe to its contained value.
    /// </summary>
    /// <param name="maybe">The Maybe to convert.</param>
    public static implicit operator T?(Maybe<T> maybe)
    {
        return maybe.Value;
    }

    #endregion
}