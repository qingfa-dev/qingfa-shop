namespace Application.Features.Identity.Auth.Options;

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
