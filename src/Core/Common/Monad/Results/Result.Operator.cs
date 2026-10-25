namespace Core.Common.Monad.Results;

public partial record Result
{
    #region Implicit Conversions

    public static implicit operator Result(Error error)
        => Fail(error);

    public static implicit operator Result(List<Error> errors)
        => Fail(errors);

    public static implicit operator Result(Error[] errors)
        => Fail(errors);

    #endregion
}