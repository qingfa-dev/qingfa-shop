namespace Application.BuildingBlocks.Security.Identity.External.Options;

/// <summary>Validation failures and stable codes for <see cref="ExternalAuthOption"/>.</summary>
public static class ExternalOptionOutcome
{
    /// <summary>Failure errors raised when the external authentication options are invalid.</summary>
    public static class Failure
    {
        /// <summary>The post-sign-in redirect target is missing.</summary>
        public static Error SuccessRedirectUrlRequired => Error.Validation(
            code: Codes.SuccessRedirectUrlRequired,
            message: "ExternalAuth:SuccessRedirectUrl is required.");

        /// <summary>The one-time code lifetime is outside the allowed range.</summary>
        public static Error CodeLifetimeSecondsOutOfRange => Error.Validation(
            code: Codes.CodeLifetimeSecondsOutOfRange,
            message:
            $"ExternalAuth:CodeLifetimeSeconds must be between {ExternalAuthConstant.Constraints.CodeLifetimeSecondsMin} and {ExternalAuthConstant.Constraints.CodeLifetimeSecondsMax}.");

        /// <summary>An enabled provider is missing its client identifier.</summary>
        public static Error ClientIdRequired(string provider) => Error.Validation(
            code: Codes.ClientIdRequired,
            message: $"ExternalAuth:{provider}:ClientId is required when the provider is enabled.");

        /// <summary>An enabled provider is missing its client secret.</summary>
        public static Error ClientSecretRequired(string provider) => Error.Validation(
            code: Codes.ClientSecretRequired,
            message: $"ExternalAuth:{provider}:ClientSecret is required when the provider is enabled.");

        /// <summary>The OpenID Connect authority is not an absolute URL.</summary>
        public static Error OpenIdConnectAuthorityInvalid => Error.Validation(
            code: Codes.OpenIdConnectAuthorityInvalid,
            message: "ExternalAuth:OpenIdConnect:Authority must be an absolute URL.");

        /// <summary>The OpenID Connect scope list is empty.</summary>
        public static Error OpenIdConnectScopesRequired => Error.Validation(
            code: Codes.OpenIdConnectScopesRequired,
            message: "ExternalAuth:OpenIdConnect:Scopes must contain at least one scope.");

        /// <summary>The OpenID Connect callback path is missing.</summary>
        public static Error OpenIdConnectCallbackPathRequired => Error.Validation(
            code: Codes.OpenIdConnectCallbackPathRequired,
            message: "ExternalAuth:OpenIdConnect:CallbackPath is required.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string SuccessRedirectUrlRequired = "External.SuccessRedirectUrl.Required";
        public const string CodeLifetimeSecondsOutOfRange = "External.CodeLifetimeSeconds.OutOfRange";
        public const string ClientIdRequired = "External.Provider.ClientId.Required";
        public const string ClientSecretRequired = "External.Provider.ClientSecret.Required";
        public const string OpenIdConnectAuthorityInvalid = "External.OpenIdConnect.Authority.Invalid";
        public const string OpenIdConnectScopesRequired = "External.OpenIdConnect.Scopes.Required";
        public const string OpenIdConnectCallbackPathRequired = "External.OpenIdConnect.CallbackPath.Required";
    }
}
