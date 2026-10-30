using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.External.Models;

namespace Application.Features.Identity.External.Abstractions;

/// <summary>Application-level external OAuth/OIDC login operations.</summary>
public interface IExternalAuthenticationService
{
    /// <summary>Returns the external providers configured for this host.</summary>
    IReadOnlyCollection<ExternalProviderModel> GetProviders();

    /// <summary>Finds or creates a local account for external identity data and issues tokens.</summary>
    Task<Result<AuthTokenResponse>> LoginAsync(
        ExternalUserInfo user,
        CancellationToken cancellationToken = default);
}
