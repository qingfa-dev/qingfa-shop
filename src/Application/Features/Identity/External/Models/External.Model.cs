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
