using Application.BuildingBlocks.Security.Identity.Auth;
using Application.BuildingBlocks.Security.Identity.Auth.Models;
using Application.BuildingBlocks.Security.Identity.External.Abstractions;
using Application.BuildingBlocks.Security.Identity.External.Models;
using Application.BuildingBlocks.Security.Identity.External.Options;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.External;

/// <summary>Shared sample data for the external sign-in slice unit tests.</summary>
public sealed class ExternalFixture
{
    /// <summary>Gets the external authentication service mock.</summary>
    public Mock<IExternalAuthenticationService> External { get; } = new();

    /// <summary>Gets the one-time-code store mock.</summary>
    public Mock<IExternalCodeStore> Codes { get; } = new();

    /// <summary>Gets the external authentication settings.</summary>
    public ExternalAuthOption Option { get; } = new()
    {
        SuccessRedirectUrl = "https://client.example/callback",
        CodeLifetimeSeconds = 60,
    };

    /// <summary>Gets the configured providers.</summary>
    public static IReadOnlyCollection<ExternalProviderModel> Providers =>
    [
        new(IdentityConstant.Providers.Google, "Google"),
        new(IdentityConstant.Providers.Microsoft, "Microsoft"),
        new(IdentityConstant.Providers.OpenIdConnect, "OpenID Connect"),
    ];

    /// <summary>Gets a representative issued token pair.</summary>
    public static AuthTokenResponse Token => new(
        "access-token",
        DateTimeOffset.UtcNow.AddMinutes(15),
        "refresh-token",
        new IdentityUserModel(
            Guid.NewGuid(),
            "external-user",
            "external@example.com",
            "External",
            null,
            true,
            false));

    /// <summary>Gets a representative external identity.</summary>
    public static ExternalUserInfo User => new(
        IdentityConstant.Providers.Google,
        "google-user-1",
        "External User",
        "external@example.com");

    /// <summary>Gets a representative service failure.</summary>
    public static Error Failure => Error.NotFound(
        "External.Test.NotFound",
        "The test external record was not found.");
}
