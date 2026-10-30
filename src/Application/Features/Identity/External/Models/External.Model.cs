namespace Application.Features.Identity.External.Models;

/// <summary>The provider challenge the client must follow.</summary>
/// <param name="Provider">The configured authentication scheme name.</param>
/// <param name="RedirectUri">The provider authorization URL to redirect to.</param>
public sealed record ExternalChallengeModel(string Provider, string RedirectUri);

/// <summary>The client redirect produced after an external callback.</summary>
/// <param name="RedirectUrl">The URL carrying the one-time exchange code.</param>
public sealed record ExternalCallbackModel(string RedirectUrl);

/// <summary>Exchanges a one-time external sign-in code for the issued token pair.</summary>
/// <param name="Code">The code delivered to the client redirect URL.</param>
public sealed record ExternalExchangeRequest(string Code);

/// <summary>A configured external authentication provider.</summary>
/// <param name="Name">The configured authentication scheme name.</param>
/// <param name="DisplayName">The user-facing provider name.</param>
public sealed record ExternalProviderModel(string Name, string DisplayName);

/// <summary>Normalized external provider identity data.</summary>
/// <param name="Provider">The configured authentication scheme name.</param>
/// <param name="ProviderKey">The provider's stable subject identifier.</param>
/// <param name="DisplayName">The optional display name.</param>
/// <param name="Email">The optional email address.</param>
public sealed record ExternalUserInfo(
    string Provider,
    string ProviderKey,
    string? DisplayName,
    string? Email);
