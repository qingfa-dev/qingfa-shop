using FluentValidation;

namespace Application.BuildingBlocks.Security.Identity.External.Options;

/// <summary>Validates external OAuth and OpenID Connect provider options.</summary>
public sealed class ExternalOptionValidator : AbstractValidator<ExternalAuthOption>
{
    /// <summary>Creates the validator with the external authentication option rules.</summary>
    public ExternalOptionValidator()
    {
        RuleFor(options => options.SuccessRedirectUrl)
            .NotEmpty()
            .WithErrorCode(ExternalOptionOutcome.Failure.SuccessRedirectUrlRequired.Code)
            .WithMessage(ExternalOptionOutcome.Failure.SuccessRedirectUrlRequired.Message);

        RuleFor(options => options.CodeLifetimeSeconds)
            .InclusiveBetween(
                ExternalAuthConstant.Constraints.CodeLifetimeSecondsMin,
                ExternalAuthConstant.Constraints.CodeLifetimeSecondsMax)
            .WithErrorCode(ExternalOptionOutcome.Failure.CodeLifetimeSecondsOutOfRange.Code)
            .WithMessage(ExternalOptionOutcome.Failure.CodeLifetimeSecondsOutOfRange.Message);

        RuleFor(options => options.Google.ClientId)
            .NotEmpty()
            .WithErrorCode(ExternalOptionOutcome.Failure.ClientIdRequired("Google").Code)
            .WithMessage(ExternalOptionOutcome.Failure.ClientIdRequired("Google").Message)
            .When(options => options.Google.Enabled);

        RuleFor(options => options.Google.ClientSecret)
            .NotEmpty()
            .WithErrorCode(ExternalOptionOutcome.Failure.ClientSecretRequired("Google").Code)
            .WithMessage(ExternalOptionOutcome.Failure.ClientSecretRequired("Google").Message)
            .When(options => options.Google.Enabled);

        RuleFor(options => options.Microsoft.ClientId)
            .NotEmpty()
            .WithErrorCode(ExternalOptionOutcome.Failure.ClientIdRequired("Microsoft").Code)
            .WithMessage(ExternalOptionOutcome.Failure.ClientIdRequired("Microsoft").Message)
            .When(options => options.Microsoft.Enabled);

        RuleFor(options => options.Microsoft.ClientSecret)
            .NotEmpty()
            .WithErrorCode(ExternalOptionOutcome.Failure.ClientSecretRequired("Microsoft").Code)
            .WithMessage(ExternalOptionOutcome.Failure.ClientSecretRequired("Microsoft").Message)
            .When(options => options.Microsoft.Enabled);

        RuleFor(options => options.OpenIdConnect.ClientId)
            .NotEmpty()
            .WithErrorCode(ExternalOptionOutcome.Failure.ClientIdRequired("OpenIdConnect").Code)
            .WithMessage(ExternalOptionOutcome.Failure.ClientIdRequired("OpenIdConnect").Message)
            .When(options => options.OpenIdConnect.Enabled);

        RuleFor(options => options.OpenIdConnect.ClientSecret)
            .NotEmpty()
            .WithErrorCode(ExternalOptionOutcome.Failure.ClientSecretRequired("OpenIdConnect").Code)
            .WithMessage(ExternalOptionOutcome.Failure.ClientSecretRequired("OpenIdConnect").Message)
            .When(options => options.OpenIdConnect.Enabled);

        RuleFor(options => options.OpenIdConnect.Authority)
            .Must(BeAbsoluteUrl)
            .WithErrorCode(ExternalOptionOutcome.Failure.OpenIdConnectAuthorityInvalid.Code)
            .WithMessage(ExternalOptionOutcome.Failure.OpenIdConnectAuthorityInvalid.Message)
            .When(options => options.OpenIdConnect.Enabled);

        RuleFor(options => options.OpenIdConnect.Scopes)
            .Must(HaveAtLeastOneScope)
            .WithErrorCode(ExternalOptionOutcome.Failure.OpenIdConnectScopesRequired.Code)
            .WithMessage(ExternalOptionOutcome.Failure.OpenIdConnectScopesRequired.Message)
            .When(options => options.OpenIdConnect.Enabled);

        RuleFor(options => options.OpenIdConnect.CallbackPath)
            .NotEmpty()
            .WithErrorCode(ExternalOptionOutcome.Failure.OpenIdConnectCallbackPathRequired.Code)
            .WithMessage(ExternalOptionOutcome.Failure.OpenIdConnectCallbackPathRequired.Message)
            .When(options => options.OpenIdConnect.Enabled);
    }

    private static bool BeAbsoluteUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool HaveAtLeastOneScope(string[]? scopes) =>
        scopes is { Length: > 0 } &&
        scopes.Any(scope => !string.IsNullOrWhiteSpace(scope));
}
