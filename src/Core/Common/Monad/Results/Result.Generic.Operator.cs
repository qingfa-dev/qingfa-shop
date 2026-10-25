namespace Core.Common.Monad.Results;

public partial record Result<TValue>
{
    #region Implicit Conversions

    public static implicit operator Result<TValue>(TValue value)
        => Ok(value);

    public static implicit operator Result<TValue>(Error error)
        => Fail(error);

    public static implicit operator Result<TValue>(Error[] errors)
        => Fail(errors);

    public static implicit operator Result<TValue>(List<Error> errors)
        => Fail(errors);

    #endregion
}