using FluentValidation;

namespace Application.BuildingBlocks.Security.Identity.Auth.Options;

/// <summary>Validates the identity API options used to build user-facing links.</summary>
public sealed class IdentityOptionValidator : AbstractValidator<IdentityApiOption>
{
    /// <summary>Creates the validator with the identity option rules.</summary>
    public IdentityOptionValidator()
    {
        RuleFor(options => options.DefaultRole)
            .NotEmpty()
            .WithErrorCode(IdentityOptionOutcome.Failure.DefaultRoleRequired.Code)
            .WithMessage(IdentityOptionOutcome.Failure.DefaultRoleRequired.Message);

        RuleFor(options => options.PublicBaseUrl)
            .Must(BeAbsoluteUrl)
            .WithErrorCode(IdentityOptionOutcome.Failure.PublicBaseUrlInvalid.Code)
            .WithMessage(IdentityOptionOutcome.Failure.PublicBaseUrlInvalid.Message);

        RuleFor(options => options.ConfirmationPath)
            .NotEmpty()
            .WithErrorCode(IdentityOptionOutcome.Failure.ConfirmationPathRequired.Code)
            .WithMessage(IdentityOptionOutcome.Failure.ConfirmationPathRequired.Message);

        RuleFor(options => options.ResetPasswordPath)
            .NotEmpty()
            .WithErrorCode(IdentityOptionOutcome.Failure.ResetPasswordPathRequired.Code)
            .WithMessage(IdentityOptionOutcome.Failure.ResetPasswordPathRequired.Message);
    }

    private static bool BeAbsoluteUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
