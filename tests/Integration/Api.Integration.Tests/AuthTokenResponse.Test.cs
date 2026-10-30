using System.Text.Json;

using Application.Common.Security.Authentication.Identity;
using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Domain;

using Core.Common.Monad.Results;

using Infrastructure.Integrations.Serialization;

using Web.Api.Endpoints.Identity;

namespace Api.Integration.Tests;

/// <summary>Locks the public token projection and its serialized shape.</summary>
[Trait("Category", "Unit")]
public sealed class AuthTokenResponseTest
{
    [Fact(DisplayName = "AuthResponse should map to the public token strings and user")]
    public void FromAuthResponse_ShouldProjectTokenStringsAndUser()
    {
        DateTimeOffset now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        var user = new IdentityUserModel(
            Guid.NewGuid(),
            "admin",
            "admin@example.com",
            "Admin",
            null,
            true,
            false);
        var response = new AuthResponse(
            new AccessToken("access-token", "Bearer", now, now.AddMinutes(15), "jti-1"),
            new RefreshTokenIssue("refresh-token", CreateRefreshToken(now)),
            user);

        AuthTokenResponse mapped = AuthTokenResponse.FromAuthResponse(response);

        mapped.AccessToken.Should().Be("access-token");
        mapped.ExpiresAtUtc.Should().Be(now.AddMinutes(15));
        mapped.RefreshToken.Should().Be("refresh-token");
        mapped.User.Should().Be(user);
    }

    [Fact(DisplayName = "Serialization should expose only the public token and user fields")]
    public void Serialization_ShouldExposeOnlyPublicFields()
    {
        DateTimeOffset now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        Guid userId = Guid.NewGuid();
        var user = new IdentityUserModel(
            userId,
            "admin",
            "admin@example.com",
            "Admin",
            null,
            true,
            false);
        var response = new AuthResponse(
            new AccessToken("access-token", "Bearer", now, now.AddMinutes(15), "jti-1"),
            new RefreshTokenIssue("refresh-token", CreateRefreshToken(now)),
            user);

        string json = JsonSerializer.Serialize(
            AuthTokenResponse.FromAuthResponse(response),
            ApiJson.Options);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        root.EnumerateObject()
            .Select(property => property.Name)
            .Should()
            .BeEquivalentTo("accessToken", "expiresAtUtc", "refreshToken", "user");
        root.GetProperty("accessToken").GetString().Should().Be("access-token");
        root.GetProperty("refreshToken").GetString().Should().Be("refresh-token");

        JsonElement serializedUser = root.GetProperty("user");
        serializedUser.EnumerateObject()
            .Select(property => property.Name)
            .Should()
            .BeEquivalentTo(
                "id",
                "userName",
                "email",
                "firstName",
                "lastName",
                "emailConfirmed",
                "twoFactorEnabled");
        serializedUser.GetProperty("id").GetGuid().Should().Be(userId);
        serializedUser.GetProperty("userName").GetString().Should().Be("admin");
        serializedUser.GetProperty("email").GetString().Should().Be("admin@example.com");
        serializedUser.GetProperty("firstName").GetString().Should().Be("Admin");
        serializedUser.GetProperty("lastName").ValueKind.Should().Be(JsonValueKind.Null);
        serializedUser.GetProperty("emailConfirmed").GetBoolean().Should().BeTrue();
        serializedUser.GetProperty("twoFactorEnabled").GetBoolean().Should().BeFalse();
    }

    private static RefreshToken CreateRefreshToken(DateTimeOffset now)
    {
        Result<RefreshToken> created = RefreshToken.Create(
            Guid.NewGuid(),
            "token-hash",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            now,
            now.AddDays(7));

        created.IsSuccess.Should().BeTrue();

        return created.Value;
    }
}
