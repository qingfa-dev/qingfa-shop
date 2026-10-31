using Application.BuildingBlocks.Security.Authentication.Tokens.Domain;

namespace Application.Unit.Tests.BuildingBlocks.Security.Authentication.Tokens.Domain;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class RefreshTokenValidatorTest
{
    private static readonly Guid TokenId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid FamilyId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static RefreshToken CreateToken(
        Guid? id = null,
        Guid? familyId = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? expiresAt = null,
        string? userId = null)
    {
        Result<RefreshToken> result = RefreshToken.Create(
            id ?? TokenId,
            "hash-value",
            userId ?? "user-1",
            familyId ?? FamilyId,
            createdAt ?? CreatedAt,
            expiresAt ?? CreatedAt.AddDays(7));

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "Validate should reject a missing token")]
    public void Validate_ShouldRejectMissingToken()
    {
        ShouldFailWith(
            RefreshTokenValidator.Validate(null),
            "RefreshToken.Entity.Required");
    }

    [Fact(DisplayName = "Validate should accept a created token")]
    public void Validate_ShouldAcceptCreatedToken()
    {
        RefreshTokenValidator.Validate(CreateToken()).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Validate should accept a revoked token with a reason")]
    public void Validate_ShouldAcceptRevokedToken()
    {
        RefreshToken token = CreateToken();
        Result<RefreshToken>.Success(token).Revoke(CreatedAt, RefreshTokenRevocationReason.UserLogout);

        RefreshTokenValidator.Validate(token).IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateTokenHash should reject blank values")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTokenHash_ShouldRejectBlank(string? tokenHash)
    {
        ShouldFailWith(
            RefreshTokenValidator.ValidateTokenHash(tokenHash),
            "RefreshToken.TokenHash.Required");
    }

    [Fact(DisplayName = "ValidateTokenHash should accept values at the maximum length")]
    public void ValidateTokenHash_ShouldAcceptMaximumLength()
    {
        string tokenHash = new('a', RefreshTokenConstant.Constraints.TokenHash.MaxLength);

        RefreshTokenValidator.ValidateTokenHash(tokenHash).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateTokenHash should reject values beyond the maximum length")]
    public void ValidateTokenHash_ShouldRejectTooLong()
    {
        string tokenHash = new('a', RefreshTokenConstant.Constraints.TokenHash.MaxLength + 1);

        ShouldFailWith(
            RefreshTokenValidator.ValidateTokenHash(tokenHash),
            "RefreshToken.TokenHash.TooLong");
    }

    [Theory(DisplayName = "ValidateUserId should reject blank values")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUserId_ShouldRejectBlank(string? userId)
    {
        ShouldFailWith(
            RefreshTokenValidator.ValidateUserId(userId),
            "RefreshToken.UserId.Required");
    }

    [Fact(DisplayName = "ValidateUserId should accept values at the maximum length")]
    public void ValidateUserId_ShouldAcceptMaximumLength()
    {
        string userId = new('a', RefreshTokenConstant.Constraints.UserId.MaxLength);

        RefreshTokenValidator.ValidateUserId(userId).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateUserId should reject values beyond the maximum length")]
    public void ValidateUserId_ShouldRejectTooLong()
    {
        string userId = new('a', RefreshTokenConstant.Constraints.UserId.MaxLength + 1);

        ShouldFailWith(
            RefreshTokenValidator.ValidateUserId(userId),
            "RefreshToken.UserId.TooLong");
    }

    [Fact(DisplayName = "ValidateTokenFamilyId should reject the empty identifier")]
    public void ValidateTokenFamilyId_ShouldRejectEmpty()
    {
        ShouldFailWith(
            RefreshTokenValidator.ValidateTokenFamilyId(Guid.Empty),
            "RefreshToken.TokenFamilyId.Required");
    }

    [Fact(DisplayName = "ValidateTokenFamilyId should accept a non-empty identifier")]
    public void ValidateTokenFamilyId_ShouldAcceptNonEmpty()
    {
        RefreshTokenValidator.ValidateTokenFamilyId(FamilyId).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateCreatedAt should reject the default timestamp")]
    public void ValidateCreatedAt_ShouldRejectDefault()
    {
        ShouldFailWith(
            RefreshTokenValidator.ValidateCreatedAt(default),
            "RefreshToken.CreatedAt.Required");
    }

    [Fact(DisplayName = "ValidateCreatedAt should accept a real timestamp")]
    public void ValidateCreatedAt_ShouldAcceptRealTimestamp()
    {
        RefreshTokenValidator.ValidateCreatedAt(CreatedAt).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateExpiresAt should reject the default timestamp")]
    public void ValidateExpiresAt_ShouldRejectDefault()
    {
        ShouldFailWith(
            RefreshTokenValidator.ValidateExpiresAt(default, CreatedAt),
            "RefreshToken.ExpiresAt.Required");
    }

    [Theory(DisplayName = "ValidateExpiresAt should reject an expiry at or before creation")]
    [MemberData(nameof(InvalidExpiryDates))]
    public void ValidateExpiresAt_ShouldRejectInvalidExpiry(DateTimeOffset expiresAt)
    {
        ShouldFailWith(
            RefreshTokenValidator.ValidateExpiresAt(expiresAt, CreatedAt),
            "RefreshToken.ExpiresAt.BeforeCreated");
    }

    public static TheoryData<DateTimeOffset> InvalidExpiryDates => new()
    {
        CreatedAt.AddDays(-1),
        CreatedAt
    };

    [Fact(DisplayName = "ValidateExpiresAt should accept a later expiry")]
    public void ValidateExpiresAt_ShouldAcceptLaterExpiry()
    {
        RefreshTokenValidator.ValidateExpiresAt(CreatedAt.AddDays(1), CreatedAt)
            .IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateRevokedAt should accept a missing timestamp")]
    public void ValidateRevokedAt_ShouldAcceptNull()
    {
        RefreshTokenValidator.ValidateRevokedAt(null, CreatedAt).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateRevokedAt should reject a timestamp before creation")]
    public void ValidateRevokedAt_ShouldRejectBeforeCreation()
    {
        ShouldFailWith(
            RefreshTokenValidator.ValidateRevokedAt(CreatedAt.AddDays(-1), CreatedAt),
            "RefreshToken.RevokedAt.BeforeCreated");
    }

    [Fact(DisplayName = "ValidateRevokedAt should skip the comparison when creation is unset")]
    public void ValidateRevokedAt_ShouldSkipComparison_WhenCreatedAtIsDefault()
    {
        RefreshTokenValidator.ValidateRevokedAt(CreatedAt, default).IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ValidateRevokedAt should accept a timestamp at or after creation")]
    [MemberData(nameof(ValidRevocationDates))]
    public void ValidateRevokedAt_ShouldAcceptValidTimestamp(DateTimeOffset revokedAt)
    {
        RefreshTokenValidator.ValidateRevokedAt(revokedAt, CreatedAt).IsSuccess.Should().BeTrue();
    }

    public static TheoryData<DateTimeOffset> ValidRevocationDates => new()
    {
        CreatedAt,
        CreatedAt.AddDays(1)
    };

    [Fact(DisplayName = "ValidateRevocationReason should accept no revocation state")]
    public void ValidateRevocationReason_ShouldAcceptNoRevocation()
    {
        RefreshTokenValidator.ValidateRevocationReason(null, null).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateRevocationReason should require a reason when revoked")]
    public void ValidateRevocationReason_ShouldRequireReason()
    {
        ShouldFailWith(
            RefreshTokenValidator.ValidateRevocationReason(null, CreatedAt),
            "RefreshToken.RevocationReason.Required");
    }

    [Fact(DisplayName = "ValidateRevocationReason should reject an undefined reason")]
    public void ValidateRevocationReason_ShouldRejectUndefinedReason()
    {
        ShouldFailWith(
            RefreshTokenValidator.ValidateRevocationReason(
                (RefreshTokenRevocationReason)99,
                CreatedAt),
            "RefreshToken.RevocationReason.Invalid");
    }

    [Fact(DisplayName = "ValidateRevocationReason should reject a reason without a date")]
    public void ValidateRevocationReason_ShouldRejectReasonWithoutDate()
    {
        ShouldFailWith(
            RefreshTokenValidator.ValidateRevocationReason(
                RefreshTokenRevocationReason.UserLogout,
                null),
            "RefreshToken.RevocationReason.WithoutRevocation");
    }

    [Fact(DisplayName = "ValidateRevocationReason should accept a defined reason with a date")]
    public void ValidateRevocationReason_ShouldAcceptDefinedReason()
    {
        RefreshTokenValidator.ValidateRevocationReason(
            RefreshTokenRevocationReason.UserLogout,
            CreatedAt).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateReplacement should accept a missing replacement")]
    public void ValidateReplacement_ShouldAcceptNull()
    {
        RefreshTokenValidator.ValidateReplacement(null, TokenId, "user-1", FamilyId)
            .IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ValidateReplacement should reject the token itself")]
    public void ValidateReplacement_ShouldRejectSelf()
    {
        RefreshToken token = CreateToken();

        ShouldFailWith(
            RefreshTokenValidator.ValidateReplacement(token, token.Id, token.UserId, token.TokenFamilyId),
            "RefreshToken.Replacement.SameToken");
    }

    [Fact(DisplayName = "ValidateReplacement should reject another token family")]
    public void ValidateReplacement_ShouldRejectFamilyMismatch()
    {
        RefreshToken replacement = CreateToken(
            id: Guid.NewGuid(),
            familyId: Guid.NewGuid());

        ShouldFailWith(
            RefreshTokenValidator.ValidateReplacement(replacement, TokenId, "user-1", FamilyId),
            "RefreshToken.Replacement.FamilyMismatch");
    }

    [Fact(DisplayName = "ValidateReplacement should reject another user")]
    public void ValidateReplacement_ShouldRejectUserMismatch()
    {
        RefreshToken replacement = CreateToken(
            id: Guid.NewGuid(),
            userId: "user-2");

        ShouldFailWith(
            RefreshTokenValidator.ValidateReplacement(replacement, TokenId, "user-1", FamilyId),
            "RefreshToken.Replacement.UserMismatch");
    }

    [Fact(DisplayName = "ValidateReplacement should accept a token in the same family")]
    public void ValidateReplacement_ShouldAcceptSameFamily()
    {
        RefreshToken replacement = CreateToken(id: Guid.NewGuid());

        RefreshTokenValidator.ValidateReplacement(replacement, TokenId, "user-1", FamilyId)
            .IsSuccess.Should().BeTrue();
    }
}
