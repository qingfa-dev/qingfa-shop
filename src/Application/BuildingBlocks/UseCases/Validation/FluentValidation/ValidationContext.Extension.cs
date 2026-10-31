using FluentValidation;

namespace Application.BuildingBlocks.UseCases.Validation.FluentValidation;

/// <summary>
/// Adds the repository's monadic <see cref="Result"/> failures to a
/// FluentValidation context without re-implementing validation rules.
/// </summary>
/// <remarks>
/// This type lives in the Application layer, not Infrastructure: validators live
/// in Application, and Application must never reference Infrastructure. The
/// extension only needs Core plus FluentValidation, so the dependency direction
/// stays correct.
/// </remarks>
public static class ValidationContextExtension
{
    #region Result Conversion

    /// <summary>
    /// Converts every error in a failed <paramref name="result"/> into a
    /// FluentValidation failure. Successful results add nothing.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="context">The FluentValidation context receiving the failures.</param>
    /// <param name="result">The domain or application result to translate.</param>
    /// <param name="propertyName">
    /// The property the failures belong to. When omitted, the error's
    /// <see cref="ErrorConstant.Metadata.Field"/> metadata is used, then an empty name.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="result"/> is <c>null</c>.
    /// </exception>
    public static void AddResultErrors<T>(
        this ValidationContext<T> context,
        Result result,
        string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return;
        }

        foreach (Error error in result.Errors)
        {
            context.AddFailure(error.ToValidationFailure(propertyName));
        }
    }

    /// <summary>
    /// Converts every error in a failed <see cref="Result{TValue}"/> into a
    /// FluentValidation failure.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The result's success value type.</typeparam>
    /// <param name="context">The FluentValidation context receiving the failures.</param>
    /// <param name="result">The typed result to translate.</param>
    /// <param name="propertyName">The optional property the failures belong to.</param>
    /// <remarks>
    /// <see cref="Result{TValue}"/> derives from <see cref="Result"/>, so the
    /// non-generic overload already accepts it. This overload exists so callers
    /// can keep a typed result at the call site without casting.
    /// </remarks>
    public static void AddResultErrors<T, TValue>(
        this ValidationContext<T> context,
        Result<TValue> result,
        string? propertyName = null)
        => AddResultErrors(context, (Result)result, propertyName);

    #endregion
}