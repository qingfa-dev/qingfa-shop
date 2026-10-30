using System.Text.Json;

using Application.Features.Identity.Auth.Models;

using Infrastructure.Integrations.Serialization;

namespace Api.Integration.Tests;

/// <summary>Locks the public token projection and its serialized shape.</summary>
[Trait("Category", "Unit")]
public sealed class AuthTokenResponseTest
{
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
        var response = new AuthTokenResponse(
            "access-token",
            now.AddMinutes(15),
            "refresh-token",
            user);

        string json = JsonSerializer.Serialize(response, ApiJson.Options);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        root.EnumerateObject()
            .Select(property => property.Name)
            .Should()
            .BeEquivalentTo("accessToken", "expiresAtUtc", "refreshToken", "user");
        root.GetProperty("accessToken").GetString().Should().Be("access-token");
        root.GetProperty("refreshToken").GetString().Should().Be("refresh-token");
        root.GetProperty("expiresAtUtc").GetDateTimeOffset()
            .Should().Be(now.AddMinutes(15));

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
}
