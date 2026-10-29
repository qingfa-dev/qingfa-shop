using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Options;

using Infrastructure.Security.Authentication.Tokens.Services;

using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Unit.Tests.Security.Authentication.Tokens.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class JwtTokenServiceTest
{
    private const string SigningKey = "qingfa-scaffold-only-signing-key-change-me-32+";
    private static readonly DateTimeOffset Now = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static JwtTokenOption ValidOption() => new()
    {
        Key = SigningKey,
        Issuer = "QingFa.Shop",
        Audience = "QingFa.Shop.Clients",
        AccessTokenMinutes = 15
    };

    private static JwtTokenService CreateService(JwtTokenOption? option = null) =>
        new(option ?? ValidOption(), new FixedTimeProvider(Now));

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "CreateAccessToken should issue a signed token with every supplied claim")]
    public void CreateAccessToken_ShouldIssueSignedToken()
    {
        JwtTokenService service = CreateService();
        var request = new AccessTokenRequest(
            "user-1",
            UserName: "Alice",
            Email: "alice@example.com",
            TenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Roles: ["admin", "seller"],
            Claims: new Dictionary<string, string> { ["department"] = "sales" });

        Result<AccessToken> result = service.CreateAccessToken(request);

        result.IsSuccess.Should().BeTrue();
        AccessToken accessToken = result.Value;
        accessToken.Token.Should().NotBeNullOrWhiteSpace();
        accessToken.TokenType.Should().Be("Bearer");
        accessToken.IssuedAtUtc.Should().Be(Now);
        accessToken.ExpiresAtUtc.Should().Be(Now.AddMinutes(15));
        accessToken.Jti.Should().HaveLength(32);

        JwtSecurityToken parsed = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
        parsed.Issuer.Should().Be("QingFa.Shop");
        parsed.Audiences.Should().ContainSingle().Which.Should().Be("QingFa.Shop.Clients");
        parsed.ValidTo.Should().Be(accessToken.ExpiresAtUtc.UtcDateTime);
        parsed.Claims.Should().Contain(claim => claim.Type == "sub" && claim.Value == "user-1");
        parsed.Claims.Should().Contain(claim => claim.Type == "jti" && claim.Value == accessToken.Jti);
        parsed.Claims.Should().Contain(claim => claim.Type == "name" && claim.Value == "Alice");
        parsed.Claims.Should().Contain(claim => claim.Type == "email" && claim.Value == "alice@example.com");
        parsed.Claims.Should().Contain(claim =>
            claim.Type == "tenant_id" && claim.Value == "11111111-1111-1111-1111-111111111111");
        parsed.Claims.Should().Contain(claim =>
            claim.Type == "department" && claim.Value == "sales");
        parsed.Claims.Should().Contain(claim => claim.Value == "admin");
        parsed.Claims.Should().Contain(claim => claim.Value == "seller");
        parsed.Claims.Should().Contain(claim => claim.Type == "iat");
    }

    [Fact(DisplayName = "CreateAccessToken should produce a verifiable signature")]
    public void CreateAccessToken_ShouldProduceVerifiableSignature()
    {
        JwtTokenService service = CreateService();
        AccessToken accessToken = service.CreateAccessToken(new AccessTokenRequest("user-1")).Value;
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "QingFa.Shop",
            ValidateAudience = true,
            ValidAudience = "QingFa.Shop.Clients",
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey))
        };
        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();

        ClaimsPrincipal principal = handler.ValidateToken(
            accessToken.Token,
            parameters,
            out SecurityToken validatedToken);

        validatedToken.Should().BeOfType<JwtSecurityToken>();
        principal.FindFirst("sub")!.Value.Should().Be("user-1");
        principal.FindFirst("jti")!.Value.Should().Be(accessToken.Jti);
    }

    [Fact(DisplayName = "CreateAccessToken should omit optional claims when they are absent")]
    public void CreateAccessToken_ShouldOmitOptionalClaims()
    {
        JwtTokenService service = CreateService();
        AccessToken accessToken = service
            .CreateAccessToken(new AccessTokenRequest(" user-1 "))
            .Value;

        JwtSecurityToken parsed = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);

        parsed.Claims.Should().Contain(claim => claim.Type == "sub" && claim.Value == "user-1");
        parsed.Claims.Should().NotContain(claim => claim.Type == "name");
        parsed.Claims.Should().NotContain(claim => claim.Type == "email");
        parsed.Claims.Should().NotContain(claim => claim.Type == "tenant_id");
    }

    [Fact(DisplayName = "CreateAccessToken should reject a missing user identifier")]
    public void CreateAccessToken_ShouldRejectMissingUserId()
    {
        JwtTokenService service = CreateService();

        ShouldFailWith(
            service.CreateAccessToken(new AccessTokenRequest("   ")),
            "Token.UserId.Required");
    }

    [Fact(DisplayName = "CreateAccessToken should reject an overlong custom claim key")]
    public void CreateAccessToken_ShouldRejectInvalidClaimKey()
    {
        JwtTokenService service = CreateService();
        var claims = new Dictionary<string, string>
        {
            [new string('a', TokenConstant.Limits.MaxClaimKeyLength + 1)] = "value"
        };

        ShouldFailWith(
            service.CreateAccessToken(new AccessTokenRequest("user-1", Claims: claims)),
            "Token.Claim.KeyTooLong");
    }

    [Fact(DisplayName = "CreateAccessToken should reject a null request")]
    public void CreateAccessToken_ShouldRejectNullRequest()
    {
        JwtTokenService service = CreateService();
        AccessTokenRequest? request = null;

        Assert.Throws<ArgumentNullException>(() => service.CreateAccessToken(request!));
    }

    [Fact(DisplayName = "The constructor should reject invalid options")]
    public void Constructor_ShouldRejectInvalidOptions()
    {
        JwtTokenOption option = ValidOption();
        option.Key = "too-short";

        Assert.Throws<InvalidOperationException>(() => CreateService(option));
    }

    [Fact(DisplayName = "The constructor should reject null dependencies")]
    public void Constructor_ShouldRejectNullDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new JwtTokenService(null!, TimeProvider.System));
        Assert.Throws<ArgumentNullException>(() => new JwtTokenService(ValidOption(), null!));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
