using Core.BuildingBlocks.Modeling.Metadata;

using FluentValidation.Results;

namespace Application.BuildingBlocks.UseCases.Validation.FluentValidation;

/// <summary>
/// The original monadic error carried on <see cref="ValidationFailure.CustomState"/>.
/// </summary>
/// <param name="Error">The error the failure was translated from.</param>
/// <remarks>
/// FluentValidation has no native HTTP-status field, so the whole <see cref="Error"/>
/// is preserved. The validation behavior can therefore restore the original status,
/// severity, metadata, type, and instance instead of flattening everything to 422.
/// </remarks>
public sealed record ValidationErrorState(Error Error)
{
    /// <summary>Gets the original HTTP status, when the error declared one.</summary>
    public int? Status => Error.Status;

    /// <summary>Gets the original operational severity.</summary>
    public ErrorSeverity Severity => Error.Severity;

    /// <summary>Gets the original metadata.</summary>
    public DefaultMetadata Metadata => Error.Metadata;
}