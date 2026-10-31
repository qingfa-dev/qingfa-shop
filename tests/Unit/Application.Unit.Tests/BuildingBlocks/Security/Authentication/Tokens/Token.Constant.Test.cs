using Application.BuildingBlocks.Security.Authentication.Tokens;

namespace Application.Unit.Tests.BuildingBlocks.Security.Authentication.Tokens;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class TokenConstantTest
{
    [Fact(DisplayName = "Configuration section names should match the documented keys")]
    public void Sections_ShouldMatchDocumentedKeys()
    {
        TokenConstant.Sections.Jwt.Should().Be("Jwt");
        TokenConstant.Sections.RefreshToken.Should().Be("RefreshToken");
        TokenConstant.Sections.Blacklist.Should().Be("TokenBlacklist");
    }

    [Fact(DisplayName = "Claim names should match the JWT wire format")]
    public void Claims_ShouldMatchJwtWireFormat()
    {
        TokenConstant.Claims.Subject.Should().Be("sub");
        TokenConstant.Claims.Name.Should().Be("name");
        TokenConstant.Claims.Email.Should().Be("email");
        TokenConstant.Claims.TenantId.Should().Be("tenant_id");
        TokenConstant.Claims.Jti.Should().Be("jti");
        TokenConstant.Claims.Role.Should().Be("role");
        TokenConstant.Claims.IssuedAt.Should().Be("iat");
    }

    [Fact(DisplayName = "Token limits should expose the documented bounds")]
    public void Limits_ShouldExposeDocumentedBounds()
    {
        TokenConstant.Limits.MinSigningKeyLength.Should().Be(32);
        TokenConstant.Limits.MinAccessTokenMinutes.Should().Be(1);
        TokenConstant.Limits.MaxAccessTokenMinutes.Should().Be(1440);
        TokenConstant.Limits.MaxClockSkewSeconds.Should().Be(300);
        TokenConstant.Limits.MinRefreshTokenDays.Should().Be(1);
        TokenConstant.Limits.MaxRefreshTokenDays.Should().Be(365);
        TokenConstant.Limits.MinRefreshTokenBytes.Should().Be(16);
        TokenConstant.Limits.MaxRefreshTokenBytes.Should().Be(64);
        TokenConstant.Limits.MaxRefreshTokenLength.Should().Be(512);
        TokenConstant.Limits.MaxJtiLength.Should().Be(128);
        TokenConstant.Limits.MaxUserIdLength.Should().Be(450);
        TokenConstant.Limits.MaxUserNameLength.Should().Be(256);
        TokenConstant.Limits.MaxEmailLength.Should().Be(320);
        TokenConstant.Limits.MaxClaimKeyLength.Should().Be(128);
    }

    [Fact(DisplayName = "Defaults should be usable without configuration")]
    public void Defaults_ShouldBeUsableWithoutConfiguration()
    {
        TokenConstant.Defaults.AccessTokenMinutes.Should().Be(15);
        TokenConstant.Defaults.ClockSkewSeconds.Should().Be(30);
        TokenConstant.Defaults.RefreshTokenDays.Should().Be(7);
        TokenConstant.Defaults.RefreshTokenBytes.Should().Be(32);
        TokenConstant.Defaults.RefreshTokenPrefix.Should().Be("rt_");
        TokenConstant.Defaults.BlacklistKeyPrefix.Should().Be("auth:blacklist:");
    }

    [Fact(DisplayName = "Scheme values should identify bearer HMAC-SHA256 tokens")]
    public void Scheme_ShouldIdentifyBearerHmacSha256()
    {
        TokenConstant.Scheme.Bearer.Should().Be("Bearer");
        TokenConstant.Scheme.HmacSha256.Should().Be("HS256");
    }
}
