using Application.Features.Identity.Auth;
using Application.Features.Identity.External.Models;
using Application.Features.Identity.External.Options;

namespace Application.Features.Identity.External.Mappings;

/// <summary>Maps external provider configuration onto the external sign-in models.</summary>
public static class ExternalMapper
{
    #region Provider

    /// <summary>Finds the configured provider matching a route segment or scheme name.</summary>
    /// <param name="providers">The providers reported by the external authentication service.</param>
    /// <param name="provider">The provider segment or scheme from the request.</param>
    /// <returns>The configured provider, or a 404 failure when it is not enabled.</returns>
    public static Result<ExternalProviderModel> ResolveProvider(
        this IReadOnlyCollection<ExternalProviderModel> providers,
        string? provider)
    {
        ArgumentNullException.ThrowIfNull(providers);

        ExternalProviderModel? match = string.IsNullOrWhiteSpace(provider)
            ? null
            : providers.FirstOrDefault(candidate =>
                candidate.Name.Equals(provider, StringComparison.OrdinalIgnoreCase) ||
                IsOpenIdConnectAlias(candidate.Name, provider));

        return match is null
            ? Result<ExternalProviderModel>.Fail(Error.NotFound(
                "Auth.External.ProviderUnsupported",
                "The requested external provider is not configured."))
            : Result<ExternalProviderModel>.Ok(match);
    }

    /// <summary>Builds the route segment for a provider scheme.</summary>
    /// <param name="scheme">The authentication scheme name.</param>
    /// <returns>The lowercase route segment used by the external routes.</returns>
    public static string ProviderSegment(string scheme)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheme);

        return scheme.Equals(IdentityConstant.Providers.OpenIdConnect, StringComparison.OrdinalIgnoreCase)
            ? "openid"
            : scheme.ToLowerInvariant();
    }

    /// <summary>Builds the provider callback path the challenge must return to.</summary>
    /// <param name="providerSegment">The lowercase provider route segment.</param>
    /// <returns>The absolute callback path.</returns>
    public static string CallbackPath(string providerSegment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSegment);

        return $"{IdentityConstant.Routes.External}/{providerSegment}/callback";
    }

    /// <summary>Resolves the callback URL registered with the provider's authentication handler.</summary>
    /// <param name="provider">The configured provider.</param>
    /// <param name="option">The external authentication settings.</param>
    /// <returns>The callback path the handler expects.</returns>
    public static string CallbackRedirectUri(
        ExternalProviderModel provider,
        ExternalAuthOption option)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(option);

        // Fallback: OpenID Connect accepts a configured callback path; the other schemes
        // use the route this API exposes.
        return provider.Name.Equals(IdentityConstant.Providers.OpenIdConnect, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(option.OpenIdConnect.CallbackPath)
            ? option.OpenIdConnect.CallbackPath
            : CallbackPath(ProviderSegment(provider.Name));
    }

    /// <summary>Builds the challenge model for a configured provider.</summary>
    /// <param name="provider">The configured provider.</param>
    /// <param name="option">The external authentication settings.</param>
    /// <returns>The authentication scheme and the local callback path.</returns>
    public static ExternalChallengeModel ToChallenge(
        this ExternalProviderModel provider,
        ExternalAuthOption option)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(option);

        return new ExternalChallengeModel(
            provider.Name,
            CallbackRedirectUri(provider, option));
    }

    #endregion

    #region Callback

    /// <summary>Maps a normalized callback request onto the external user information.</summary>
    /// <param name="request">The callback identity from the provider principal.</param>
    /// <returns>The user information consumed by the external authentication service.</returns>
    public static ExternalUserInfo ToUserInfo(this ExternalCallbackRequestModel request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ExternalUserInfo(
            request.Provider,
            request.ProviderKey,
            request.DisplayName,
            request.Email);
    }

    /// <summary>Appends the one-time code to the configured client redirect URL.</summary>
    /// <param name="successRedirectUrl">The configured client URL.</param>
    /// <param name="code">The one-time code.</param>
    /// <returns>The redirect URL carrying the code.</returns>
    public static string BuildRedirectUrl(string successRedirectUrl, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        string target = string.IsNullOrWhiteSpace(successRedirectUrl) ? "/" : successRedirectUrl;

        string prefix = target.EndsWith('?') || target.EndsWith('&')
            ? target
            : target.Contains('?', StringComparison.Ordinal)
                ? $"{target}&"
                : $"{target}?";

        return $"{prefix}code={Uri.EscapeDataString(code)}";
    }

    /// <summary>Builds the client callback model for an issued one-time code.</summary>
    /// <param name="successRedirectUrl">The configured client URL.</param>
    /// <param name="code">The one-time code.</param>
    /// <returns>The redirect model handed back to the client.</returns>
    public static ExternalCallbackModel ToCallback(
        string successRedirectUrl,
        string code)
        => new(BuildRedirectUrl(successRedirectUrl, code));

    #endregion

    private static bool IsOpenIdConnectAlias(string scheme, string provider) =>
        scheme.Equals(IdentityConstant.Providers.OpenIdConnect, StringComparison.OrdinalIgnoreCase) &&
        (provider.Equals("openid", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("openidconnect", StringComparison.OrdinalIgnoreCase));
}
