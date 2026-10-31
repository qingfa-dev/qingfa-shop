using Application.BuildingBlocks.Security.Authentication.Tokens;
using Application.BuildingBlocks.Security.Authentication.Tokens.Options;

namespace Application.Unit.Tests.BuildingBlocks.Security.Authentication.Tokens;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class TokenOptionValidatorTest
{
    private const string SigningKey = "qingfa-scaffold-only-signing-key-change-me-32+";

    private static JwtTokenOption ValidJwtOption() => new()
    {
        Key = SigningKey,
        Issuer = "QingFa.Shop",
        Audience = "QingFa.Shop.Clients"
    };

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "JWT options should accept a valid configuration")]
    public void JwtOptions_ShouldAcceptValidConfiguration()
    {
        TokenOptionValidator.Validate(ValidJwtOption()).IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "JWT options should require a signing key")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void JwtOptions_ShouldRequireSigningKey(string? key)
    {
        JwtTokenOption option = ValidJwtOption();
        option.Key = key!;

        ShouldFailWith(
            TokenOptionValidator.Validate(option),
            "Token.SigningKey.Required");
    }

    [Fact(DisplayName = "JWT options should require a signing key of at least 32 characters")]
    public void JwtOptions_ShouldRequireLongSigningKey()
    {
        JwtTokenOption option = ValidJwtOption();
        option.Key = new string('a', TokenConstant.Limits.MinSigningKeyLength - 1);

        ShouldFailWith(
            TokenOptionValidator.Validate(option),
            "Token.SigningKey.TooShort");
    }

    [Fact(DisplayName = "JWT options should accept a signing key at the minimum length")]
    public void JwtOptions_ShouldAcceptMinimumSigningKey()
    {
        JwtTokenOption option = ValidJwtOption();
        option.Key = new string('a', TokenConstant.Limits.MinSigningKeyLength);

        TokenOptionValidator.Validate(option).IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "JWT options should require issuer and audience")]
    [InlineData("", "QingFa.Shop.Clients", "Token.Issuer.Required")]
    [InlineData("QingFa.Shop", "", "Token.Audience.Required")]
    public void JwtOptions_ShouldRequireIssuerAndAudience(
        string issuer,
        string audience,
        string expectedCode)
    {
        JwtTokenOption option = ValidJwtOption();
        option.Issuer = issuer;
        option.Audience = audience;

        ShouldFailWith(TokenOptionValidator.Validate(option), expectedCode);
    }

    [Theory(DisplayName = "JWT options should bound the access-token lifetime")]
    [InlineData(0)]
    [InlineData(1441)]
    public void JwtOptions_ShouldBoundAccessTokenLifetime(int minutes)
    {
        JwtTokenOption option = ValidJwtOption();
        option.AccessTokenMinutes = minutes;

        ShouldFailWith(
            TokenOptionValidator.Validate(option),
            "Token.AccessToken.LifetimeInvalid");
    }

    [Theory(DisplayName = "JWT options should bound the clock skew")]
    [InlineData(-1)]
    [InlineData(301)]
    public void JwtOptions_ShouldBoundClockSkew(int seconds)
    {
        JwtTokenOption option = ValidJwtOption();
        option.ClockSkewSeconds = seconds;

        ShouldFailWith(
            TokenOptionValidator.Validate(option),
            "Token.ClockSkew.Invalid");
    }

    [Fact(DisplayName = "JWT options should reject null")]
    public void JwtOptions_ShouldRejectNull()
    {
        JwtTokenOption? option = null;

        Assert.Throws<ArgumentNullException>(() => TokenOptionValidator.Validate(option!));
    }

    [Fact(DisplayName = "Refresh options should accept a valid configuration")]
    public void RefreshOptions_ShouldAcceptValidConfiguration()
    {
        TokenOptionValidator.Validate(new RefreshTokenOption()).IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "Refresh options should bound the lifetime")]
    [InlineData(0)]
    [InlineData(366)]
    public void RefreshOptions_ShouldBoundLifetime(int days)
    {
        var option = new RefreshTokenOption { LifetimeDays = days };

        ShouldFailWith(
            TokenOptionValidator.Validate(option),
            "Token.RefreshToken.LifetimeInvalid");
    }

    [Theory(DisplayName = "Refresh options should bound the token length")]
    [InlineData(15)]
    [InlineData(65)]
    public void RefreshOptions_ShouldBoundTokenLength(int bytes)
    {
        var option = new RefreshTokenOption { TokenByteLength = bytes };

        ShouldFailWith(
            TokenOptionValidator.Validate(option),
            "Token.RefreshToken.LengthInvalid");
    }

    [Theory(DisplayName = "Refresh options should require a prefix")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RefreshOptions_ShouldRequirePrefix(string? prefix)
    {
        var option = new RefreshTokenOption { TokenPrefix = prefix! };

        ShouldFailWith(
            TokenOptionValidator.Validate(option),
            "Token.RefreshToken.PrefixRequired");
    }

    [Fact(DisplayName = "Refresh options should reject null")]
    public void RefreshOptions_ShouldRejectNull()
    {
        RefreshTokenOption? option = null;

        Assert.Throws<ArgumentNullException>(() => TokenOptionValidator.Validate(option!));
    }

    [Fact(DisplayName = "Blacklist options should accept a valid configuration")]
    public void BlacklistOptions_ShouldAcceptValidConfiguration()
    {
        TokenOptionValidator.Validate(new TokenBlacklistOption()).IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "Blacklist options should require a key prefix")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlacklistOptions_ShouldRequirePrefix(string? prefix)
    {
        var option = new TokenBlacklistOption { KeyPrefix = prefix! };

        ShouldFailWith(
            TokenOptionValidator.Validate(option),
            "Token.Blacklist.PrefixRequired");
    }

    [Fact(DisplayName = "Blacklist options should reject null")]
    public void BlacklistOptions_ShouldRejectNull()
    {
        TokenBlacklistOption? option = null;

        Assert.Throws<ArgumentNullException>(() => TokenOptionValidator.Validate(option!));
    }

    [Fact(DisplayName = "Access-token requests should accept minimal valid input")]
    public void AccessTokenRequest_ShouldAcceptMinimalInput()
    {
        TokenOptionValidator.Validate(new AccessTokenRequest("user-1")).IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "Access-token requests should require a user identifier")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AccessTokenRequest_ShouldRequireUserId(string? userId)
    {
        ShouldFailWith(
            TokenOptionValidator.Validate(new AccessTokenRequest(userId!)),
            "Token.UserId.Required");
    }

    [Fact(DisplayName = "Access-token requests should bound the user identifier")]
    public void AccessTokenRequest_ShouldBoundUserId()
    {
        string userId = new('a', TokenConstant.Limits.MaxUserIdLength + 1);

        ShouldFailWith(
            TokenOptionValidator.Validate(new AccessTokenRequest(userId)),
            "Token.UserId.TooLong");
    }

    [Fact(DisplayName = "Access-token requests should bound the display name")]
    public void AccessTokenRequest_ShouldBoundUserName()
    {
        string name = new('a', TokenConstant.Limits.MaxUserNameLength + 1);

        ShouldFailWith(
            TokenOptionValidator.Validate(new AccessTokenRequest("user-1", UserName: name)),
            "Token.UserName.TooLong");
    }

    [Fact(DisplayName = "Access-token requests should bound the email")]
    public void AccessTokenRequest_ShouldBoundEmail()
    {
        string email = new('a', TokenConstant.Limits.MaxEmailLength + 1);

        ShouldFailWith(
            TokenOptionValidator.Validate(new AccessTokenRequest("user-1", Email: email)),
            "Token.Email.TooLong");
    }

    [Fact(DisplayName = "Access-token requests should require non-empty custom claim keys")]
    public void AccessTokenRequest_ShouldRequireClaimKeys()
    {
        var claims = new Dictionary<string, string> { [" "] = "value" };

        ShouldFailWith(
            TokenOptionValidator.Validate(new AccessTokenRequest("user-1", Claims: claims)),
            "Token.Claim.KeyRequired");
    }

    [Fact(DisplayName = "Access-token requests should bound custom claim keys")]
    public void AccessTokenRequest_ShouldBoundClaimKeys()
    {
        string key = new('a', TokenConstant.Limits.MaxClaimKeyLength + 1);
        var claims = new Dictionary<string, string> { [key] = "value" };

        ShouldFailWith(
            TokenOptionValidator.Validate(new AccessTokenRequest("user-1", Claims: claims)),
            "Token.Claim.KeyTooLong");
    }

    [Fact(DisplayName = "Access-token requests should reject reserved custom claim keys")]
    public void AccessTokenRequest_ShouldRejectReservedClaimKeys()
    {
        var claims = new Dictionary<string, string> { ["sub"] = "other-user" };

        ShouldFailWith(
            TokenOptionValidator.Validate(new AccessTokenRequest("user-1", Claims: claims)),
            "Token.Claim.KeyReserved");
    }

    [Fact(DisplayName = "Access-token requests should reject null")]
    public void AccessTokenRequest_ShouldRejectNull()
    {
        AccessTokenRequest? request = null;

        Assert.Throws<ArgumentNullException>(() => TokenOptionValidator.Validate(request!));
    }

    [Theory(DisplayName = "Refresh-token values should be required and bounded")]
    [InlineData(null, "Token.RefreshToken.Required")]
    [InlineData("", "Token.RefreshToken.Required")]
    [InlineData("   ", "Token.RefreshToken.Required")]
    public void RefreshTokenValue_ShouldBeRequired(string? token, string expectedCode)
    {
        ShouldFailWith(TokenOptionValidator.ValidateRefreshToken(token), expectedCode);
    }

    [Fact(DisplayName = "Refresh-token values should be bounded")]
    public void RefreshTokenValue_ShouldBeBounded()
    {
        string token = new('a', TokenConstant.Limits.MaxRefreshTokenLength + 1);

        ShouldFailWith(
            TokenOptionValidator.ValidateRefreshToken(token),
            "Token.RefreshToken.TooLong");
    }

    [Fact(DisplayName = "Refresh-token values should accept a valid token")]
    public void RefreshTokenValue_ShouldAcceptValidToken()
    {
        TokenOptionValidator.ValidateRefreshToken("rt_value").IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "Token identifiers should be required and bounded")]
    [InlineData(null, "Token.Jti.Required")]
    [InlineData("", "Token.Jti.Required")]
    [InlineData("   ", "Token.Jti.Required")]
    public void Jti_ShouldBeRequired(string? jti, string expectedCode)
    {
        ShouldFailWith(TokenOptionValidator.ValidateJti(jti), expectedCode);
    }

    [Fact(DisplayName = "Token identifiers should be bounded")]
    public void Jti_ShouldBeBounded()
    {
        string jti = new('a', TokenConstant.Limits.MaxJtiLength + 1);

        ShouldFailWith(TokenOptionValidator.ValidateJti(jti), "Token.Jti.TooLong");
    }

    [Fact(DisplayName = "Token identifiers should accept a valid value")]
    public void Jti_ShouldAcceptValidValue()
    {
        TokenOptionValidator.ValidateJti("jti-1").IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "User identifiers should be required and bounded")]
    [InlineData(null, "Token.UserId.Required")]
    [InlineData("", "Token.UserId.Required")]
    [InlineData("   ", "Token.UserId.Required")]
    public void UserId_ShouldBeRequired(string? userId, string expectedCode)
    {
        ShouldFailWith(TokenOptionValidator.ValidateUserId(userId), expectedCode);
    }

    [Fact(DisplayName = "User identifiers should be bounded")]
    public void UserId_ShouldBeBounded()
    {
        string userId = new('a', TokenConstant.Limits.MaxUserIdLength + 1);

        ShouldFailWith(TokenOptionValidator.ValidateUserId(userId), "Token.UserId.TooLong");
    }

    [Fact(DisplayName = "User identifiers should accept a valid value")]
    public void UserId_ShouldAcceptValidValue()
    {
        TokenOptionValidator.ValidateUserId("user-1").IsSuccess.Should().BeTrue();
    }
}
