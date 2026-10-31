namespace Application.BuildingBlocks.Security.Identity.External.Models;

#region Request

/// <summary>Selects the external provider to challenge.</summary>
public record ExternalChallengeRequestModel
{
    /// <summary>Gets the provider route segment or scheme name.</summary>
    public string Provider { get; init; } = string.Empty;
}

/// <summary>The normalized identity returned by an external provider callback.</summary>
public record ExternalCallbackRequestModel
{
    /// <summary>Gets the configured authentication scheme name.</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>Gets the provider's stable subject identifier.</summary>
    public string ProviderKey { get; init; } = string.Empty;

    /// <summary>Gets the optional display name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the optional email address.</summary>
    public string? Email { get; init; }
}

/// <summary>Exchanges a one-time external sign-in code for the issued token pair.</summary>
/// <param name="Code">The code delivered to the client redirect URL.</param>
public sealed record ExternalExchangeRequest(string Code);

#endregion

#region Response

/// <summary>The provider challenge the client must follow.</summary>
/// <param name="Provider">The configured authentication scheme name.</param>
/// <param name="RedirectUri">The local callback path the authentication handler returns to.</param>
public sealed record ExternalChallengeModel(string Provider, string RedirectUri);

/// <summary>The client redirect produced after an external callback.</summary>
/// <param name="RedirectUrl">The URL carrying the one-time exchange code.</param>
public sealed record ExternalCallbackModel(string RedirectUrl);

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

#endregion
