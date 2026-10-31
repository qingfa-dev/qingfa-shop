using Application.BuildingBlocks.Security.Authentication.Contexts;
using Application.BuildingBlocks.Security.Identity.Auth.Abstractions;
using Application.BuildingBlocks.Security.Identity.Auth.Models;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Auth;

/// <summary>Shared mocks and sample identity data for the Auth slice unit tests.</summary>
public sealed class AuthFixture
{
    /// <summary>The authenticated user identifier.</summary>
    public static readonly Guid UserId = new("11111111-1111-1111-1111-111111111111");

    /// <summary>The fixed instant used by token fixtures.</summary>
    public static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Initializes the fixture with an authenticated user context.</summary>
    /// <param name="userId">The raw user identifier claim; defaults to <see cref="UserId"/>.</param>
    public AuthFixture(string? userId = null)
    {
        UserContext = new TestUserContext(userId ?? UserId.ToString());
    }

    /// <summary>Gets the identity service mock.</summary>
    public Mock<IIdentityService> Identity { get; } = new();

    /// <summary>Gets the current-user context.</summary>
    public TestUserContext UserContext { get; }

    /// <summary>Gets a representative identity user.</summary>
    public static IdentityUserModel User => new(
        UserId,
        "admin",
        "admin@example.com",
        "Admin",
        null,
        true,
        false);

    /// <summary>Gets a representative issued token pair.</summary>
    public static AuthTokenResponse Tokens() => new(
        "access-token",
        Now.AddMinutes(15),
        "refresh-token",
        User);

    /// <summary>Gets a representative two-factor state.</summary>
    public static TwoFactorInfo TwoFactor => new(
        true,
        "shared-key",
        "otpauth://totp/qingfa",
        ["recovery-1", "recovery-2"]);

    /// <summary>Gets a representative service failure.</summary>
    public static Error Failure => Error.BadRequest(
        "Identity.Test.Failed",
        "The identity operation failed.");

    /// <summary>A mutable user context used to exercise the current-user guard.</summary>
    public sealed class TestUserContext(string? userId) : IUserContext
    {
        /// <inheritdoc />
        public string? UserId { get; } = userId;

        /// <inheritdoc />
        public string? UserName => "admin";

        /// <inheritdoc />
        public string? Email => "admin@example.com";

        /// <inheritdoc />
        public bool IsAuthenticated => UserId is not null;

        /// <inheritdoc />
        public string? IpAddress => null;

        /// <inheritdoc />
        public string? Device => null;
    }
}
