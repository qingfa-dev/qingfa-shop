using Application.Features.Identity.Auth;

namespace Application.Common.Security.Authentication.Identity;

/// <summary>Configuration for the identity API behavior.</summary>
public sealed class IdentityApiOption
{
    /// <summary>Gets or sets a value indicating whether email confirmation is required to sign in.</summary>
    public bool RequireConfirmedEmail { get; set; }

    /// <summary>Gets or sets a value indicating whether a confirmed account is required to sign in.</summary>
    public bool RequireConfirmedAccount { get; set; }

    /// <summary>Gets or sets the role assigned to new local users.</summary>
    public string DefaultRole { get; set; } = IdentityConstant.Defaults.DefaultRole;

    /// <summary>Gets or sets the public base URL used to build email links.</summary>
    public string PublicBaseUrl { get; set; } = "https://localhost";

    /// <summary>Gets or sets the email confirmation path.</summary>
    public string ConfirmationPath { get; set; } = IdentityConstant.Routes.ConfirmEmail;

    /// <summary>Gets or sets the password reset path.</summary>
    public string ResetPasswordPath { get; set; } = IdentityConstant.Routes.ResetPassword;

    /// <summary>Gets or sets a value indicating whether two-factor setup is enabled.</summary>
    public bool EnableTwoFactor { get; set; } = true;
}

/// <summary>Configuration for external OAuth and OpenID Connect providers.</summary>
public sealed class ExternalAuthOption
{
    /// <summary>Gets or sets the client URL that receives the one-time sign-in code.</summary>
    public string SuccessRedirectUrl { get; set; } = "/";

    /// <summary>Gets or sets how long a one-time sign-in code remains valid, in seconds.</summary>
    public int CodeLifetimeSeconds { get; set; } = 60;

    /// <summary>Gets or sets Google OAuth settings.</summary>
    public GoogleAuthOption Google { get; set; } = new();

    /// <summary>Gets or sets Microsoft account settings.</summary>
    public MicrosoftAuthOption Microsoft { get; set; } = new();

    /// <summary>Gets or sets generic OpenID Connect settings.</summary>
    public OidcAuthOption OpenIdConnect { get; set; } = new();
}

/// <summary>Google OAuth client settings.</summary>
public sealed class GoogleAuthOption
{
    /// <summary>Gets or sets a value indicating whether Google sign-in is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the OAuth client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the OAuth client secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>Microsoft account OAuth settings.</summary>
public sealed class MicrosoftAuthOption
{
    /// <summary>Gets or sets a value indicating whether Microsoft sign-in is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the OAuth client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the OAuth client secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional tenant identifier.</summary>
    public string? TenantId { get; set; }
}

/// <summary>Generic OpenID Connect provider settings.</summary>
public sealed class OidcAuthOption
{
    /// <summary>Gets or sets a value indicating whether generic OIDC is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the authority/metadata URL.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Gets or sets the client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the client secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Gets or sets the callback path.</summary>
    public string CallbackPath { get; set; } = IdentityConstant.Routes.ExternalOpenIdConnectCallback;

    /// <summary>Gets or sets the requested scopes.</summary>
    public string[] Scopes { get; set; } = IdentityConstant.Defaults.OidcScopes;
}
