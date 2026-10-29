using Core.Common.Monad.Metadata;

using FluentValidation;
using FluentValidation.Results;

using ValidationSeverity = FluentValidation.Severity;

namespace Application.Common.Validation.FluentValidation;

/// <summary>
/// Maps the repository's monadic <see cref="Error"/> into FluentValidation failures.
/// </summary>
public static class ErrorMapper
{
    #region Failure Conversion

    /// <summary>
    /// Converts one monadic <see cref="Error"/> into a FluentValidation failure.
    /// </summary>
    /// <param name="error">The error to translate.</param>
    /// <param name="propertyName">
    /// The property the failure belongs to. When omitted, the error's
    /// <see cref="ErrorConstant.Metadata.Field"/> metadata is used, then an empty name.
    /// </param>
    /// <returns>The translated failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <c>null</c>.</exception>
    public static ValidationFailure ToValidationFailure(
        this Error error,
        string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ValidationFailure(
            propertyName ?? ResolvePropertyName(error),
            error.Message)
        {
            // Preserve: The machine-readable code survives the translation.
            ErrorCode = error.Code,

            // Preserve: The operational severity survives the translation.
            Severity = MapSeverity(error.Severity),

            // Preserve: Metadata, HTTP status, type, and instance stay available
            // to the validation pipeline through the typed custom state.
            CustomState = new ValidationErrorState(error)
        };
    }

    #endregion

    #region Helpers

    private static string ResolvePropertyName(Error error)
    {
        string? field = error.Metadata.GetValueOrDefault<string>(
            ErrorConstant.Metadata.Field);

        return string.IsNullOrWhiteSpace(field)
            ? string.Empty
            : field;
    }

    private static ValidationSeverity MapSeverity(ErrorSeverity severity)
        => severity switch
        {
            ErrorSeverity.Info => ValidationSeverity.Info,
            ErrorSeverity.Warning => ValidationSeverity.Warning,
            // Map: Critical and Error are both validation errors. Critical keeps
            // its original value inside ValidationErrorState.
            _ => ValidationSeverity.Error
        };

    #endregion
}