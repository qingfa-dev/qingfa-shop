using Application.Common.Security.Authentication.Tokens.Domain;

namespace Application.Unit.Tests.Common.Security.Authentication.Tokens.Domain;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class RefreshTokenTest
{
    private static readonly Guid TokenId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid FamilyId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static RefreshToken CreateToken(
        DateTimeOffset? expiresAt = null,
        string? tokenHash = "hash-value",
        string? userId = "user-1",
        Guid? familyId = null,
        Guid? id = null,
        DateTimeOffset? createdAt = null)
    {
        Result<RefreshToken> result = RefreshToken.Create(
            id ?? TokenId,
            tokenHash,
            userId,
            familyId ?? FamilyId,
            createdAt ?? CreatedAt,
            expiresAt ?? CreatedAt.AddDays(7));

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "Create should build an active token with canonical state")]
    public void Create_ShouldBuildActiveToken()
    {
        RefreshToken token = CreateToken();

        token.Id.Should().Be(TokenId);
        token.TokenHash.Should().Be("hash-value");
        token.UserId.Should().Be("user-1");
        token.TokenFamilyId.Should().Be(FamilyId);
        token.CreatedAtUtc.Should().Be(CreatedAt);
        token.ExpiresAtUtc.Should().Be(CreatedAt.AddDays(7));
        token.RevokedAtUtc.Should().BeNull();
        token.RevocationReason.Should().BeNull();
        token.ReplacedByToken.Should().BeNull();
        token.IsRevoked.Should().BeFalse();
        token.IsExpired(CreatedAt).Should().BeFalse();
        token.IsActive(CreatedAt).Should().BeTrue();
        token.IsTransient.Should().BeFalse();
    }

    [Fact(DisplayName = "Create should trim the hash and user identifier")]
    public void Create_ShouldTrimTextValues()
    {
        RefreshToken token = CreateToken(tokenHash: "  hash-value  ", userId: "  user-1  ");

        token.TokenHash.Should().Be("hash-value");
        token.UserId.Should().Be("user-1");
    }

    [Fact(DisplayName = "Create should normalize timestamps to UTC")]
    public void Create_ShouldNormalizeTimestampsToUtc()
    {
        var createdAt = new DateTimeOffset(2020, 1, 1, 7, 0, 0, TimeSpan.FromHours(7));
        var expiresAt = new DateTimeOffset(2020, 1, 8, 7, 0, 0, TimeSpan.FromHours(7));

        RefreshToken token = CreateToken(createdAt: createdAt, expiresAt: expiresAt);

        token.CreatedAtUtc.Offset.Should().Be(TimeSpan.Zero);
        token.ExpiresAtUtc.Offset.Should().Be(TimeSpan.Zero);
        token.CreatedAtUtc.Should().Be(createdAt);
        token.ExpiresAtUtc.Should().Be(expiresAt);
    }

    [Fact(DisplayName = "Create should reject an empty identifier")]
    public void Create_ShouldRejectEmptyIdentifier()
    {
        ShouldFailWith(
            RefreshToken.Create(Guid.Empty, "hash", "user", FamilyId, CreatedAt, CreatedAt.AddDays(1)),
            "Identifiable.Id.Required");
    }

    [Theory(DisplayName = "Create should reject a missing token hash")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldRejectMissingTokenHash(string? tokenHash)
    {
        ShouldFailWith(
            RefreshToken.Create(TokenId, tokenHash, "user", FamilyId, CreatedAt, CreatedAt.AddDays(1)),
            "RefreshToken.TokenHash.Required");
    }

    [Fact(DisplayName = "Create should reject a token hash beyond the maximum length")]
    public void Create_ShouldRejectLongTokenHash()
    {
        string tokenHash = new('a', RefreshTokenConstant.Constraints.TokenHash.MaxLength + 1);

        ShouldFailWith(
            RefreshToken.Create(TokenId, tokenHash, "user", FamilyId, CreatedAt, CreatedAt.AddDays(1)),
            "RefreshToken.TokenHash.TooLong");
    }

    [Theory(DisplayName = "Create should reject a missing user identifier")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldRejectMissingUserId(string? userId)
    {
        ShouldFailWith(
            RefreshToken.Create(TokenId, "hash", userId, FamilyId, CreatedAt, CreatedAt.AddDays(1)),
            "RefreshToken.UserId.Required");
    }

    [Fact(DisplayName = "Create should reject a user identifier beyond the maximum length")]
    public void Create_ShouldRejectLongUserId()
    {
        string userId = new('a', RefreshTokenConstant.Constraints.UserId.MaxLength + 1);

        ShouldFailWith(
            RefreshToken.Create(TokenId, "hash", userId, FamilyId, CreatedAt, CreatedAt.AddDays(1)),
            "RefreshToken.UserId.TooLong");
    }

    [Fact(DisplayName = "Create should reject an empty token family identifier")]
    public void Create_ShouldRejectEmptyFamilyId()
    {
        ShouldFailWith(
            RefreshToken.Create(TokenId, "hash", "user", Guid.Empty, CreatedAt, CreatedAt.AddDays(1)),
            "RefreshToken.TokenFamilyId.Required");
    }

    [Fact(DisplayName = "Create should reject a missing creation date")]
    public void Create_ShouldRejectMissingCreatedAt()
    {
        ShouldFailWith(
            RefreshToken.Create(TokenId, "hash", "user", FamilyId, default, CreatedAt.AddDays(1)),
            "RefreshToken.CreatedAt.Required");
    }

    [Fact(DisplayName = "Create should reject a missing expiry date")]
    public void Create_ShouldRejectMissingExpiresAt()
    {
        ShouldFailWith(
            RefreshToken.Create(TokenId, "hash", "user", FamilyId, CreatedAt, default),
            "RefreshToken.ExpiresAt.Required");
    }

    [Theory(DisplayName = "Create should reject an expiry that does not follow creation")]
    [MemberData(nameof(InvalidExpiryDates))]
    public void Create_ShouldRejectInvalidExpiry(DateTimeOffset expiresAt)
    {
        ShouldFailWith(
            RefreshToken.Create(TokenId, "hash", "user", FamilyId, CreatedAt, expiresAt),
            "RefreshToken.ExpiresAt.BeforeCreated");
    }

    public static TheoryData<DateTimeOffset> InvalidExpiryDates => new()
    {
        CreatedAt.AddDays(-1),
        CreatedAt
    };

    [Fact(DisplayName = "IsExpired should compare against the expiry instant")]
    public void IsExpired_ShouldCompareAgainstExpiry()
    {
        RefreshToken token = CreateToken();

        token.IsExpired(CreatedAt.AddDays(7).AddTicks(-1)).Should().BeFalse();
        token.IsExpired(CreatedAt.AddDays(7)).Should().BeTrue();
        token.IsExpired(CreatedAt.AddDays(8)).Should().BeTrue();
    }

    [Fact(DisplayName = "IsActive should be false for a revoked token")]
    public void IsActive_ShouldBeFalse_ForRevokedToken()
    {
        RefreshToken token = CreateToken();

        Result<RefreshToken> revoked = Result<RefreshToken>.Success(token)
            .Revoke(CreatedAt, RefreshTokenRevocationReason.UserLogout);

        revoked.IsSuccess.Should().BeTrue();
        token.IsActive(CreatedAt).Should().BeFalse();
    }

    [Fact(DisplayName = "The persistence constructor should leave an empty transient token")]
    public void PersistenceConstructor_ShouldLeaveEmptyTransientToken()
    {
        var token = (RefreshToken)Activator.CreateInstance(typeof(RefreshToken), nonPublic: true)!;

        token.TokenHash.Should().BeEmpty();
        token.UserId.Should().BeEmpty();
        token.TokenFamilyId.Should().Be(Guid.Empty);
        token.CreatedAtUtc.Should().Be(default);
        token.ExpiresAtUtc.Should().Be(default);
        token.RevokedAtUtc.Should().BeNull();
        token.RevocationReason.Should().BeNull();
        token.ReplacedByToken.Should().BeNull();
        token.IsRevoked.Should().BeFalse();
        token.IsTransient.Should().BeTrue();
    }

    [Fact(DisplayName = "Create should not return a value when validation fails")]
    public void Create_Invalid_ShouldNotReturnValue()
    {
        Result<RefreshToken> result = RefreshToken.Create(
            TokenId,
            "   ",
            "user",
            FamilyId,
            CreatedAt,
            CreatedAt.AddDays(1));

        result.IsFailure.Should().BeTrue();
        result.TryGetValue(out _).Should().BeFalse();
    }
}
