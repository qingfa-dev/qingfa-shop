namespace Web.Api.Endpoints.Identity.Models;

/// <summary>The logout body; only the refresh token may come from the client.</summary>
/// <param name="RefreshToken">The optional refresh token to revoke.</param>
/// <remarks>
/// The access-token identifier and expiry are always read from the authenticated
/// principal, never from this body, so a caller cannot blacklist an arbitrary token.
/// </remarks>
public sealed record LogoutBody(string? RefreshToken = null);
