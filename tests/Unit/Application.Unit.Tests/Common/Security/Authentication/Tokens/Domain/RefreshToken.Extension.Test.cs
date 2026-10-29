using Application.Common.Security.Authentication.Tokens.Domain;

namespace Application.Unit.Tests.Common.Security.Authentication.Tokens.Domain;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class RefreshTokenExtensionTest
{
    private static readonly Guid TokenId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid FamilyId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset CreatedAt = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static RefreshToken CreateToken(
        Guid? id = null,
        Guid? familyId = null,
        DateTimeOffset? expiresAt = null,
        string? userId = null)
    {
        Result<RefreshToken> result = RefreshToken.Create(
            id ?? TokenId,
            "hash-value",
            userId ?? "user-1",
            familyId ?? FamilyId,
            CreatedAt,
            expiresAt ?? CreatedAt.AddDays(7));

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "EnsureActive should accept an active token")]
    public void EnsureActive_ShouldAcceptActiveToken()
    {
        RefreshToken token = CreateToken();

        Result<RefreshToken> result = Result<RefreshToken>.Success(token)
            .EnsureActive(CreatedAt);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "EnsureActive should reject an expired token")]
    public void EnsureActive_ShouldRejectExpiredToken()
    {
        RefreshToken token = CreateToken(expiresAt: CreatedAt.AddDays(1));

        ShouldFailWith(
            Result<RefreshToken>.Success(token).EnsureActive(CreatedAt.AddDays(1)),
            "RefreshToken.Expired");
    }

    [Fact(DisplayName = "EnsureActive should reject a revoked token")]
    public void EnsureActive_ShouldRejectRevokedToken()
    {
        RefreshToken token = CreateToken();
        Result<RefreshToken>.Success(token).Revoke(CreatedAt, RefreshTokenRevocationReason.UserLogout);

        ShouldFailWith(
            Result<RefreshToken>.Success(token).EnsureActive(CreatedAt),
            "RefreshToken.Revoked");
    }

    [Fact(DisplayName = "Revoke should stamp the UTC revocation and reason")]
    public void Revoke_ShouldStampRevocation()
    {
        RefreshToken token = CreateToken();
        var revokedAt = new DateTimeOffset(2020, 1, 2, 7, 0, 0, TimeSpan.FromHours(7));

        Result<RefreshToken> result = Result<RefreshToken>.Success(token)
            .Revoke(revokedAt, RefreshTokenRevocationReason.UserLogout);

        result.IsSuccess.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        token.RevokedAtUtc.Should().Be(revokedAt);
        token.RevokedAtUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
        token.RevocationReason.Should().Be(RefreshTokenRevocationReason.UserLogout);
    }

    [Fact(DisplayName = "Revoke should be idempotent and preserve the first reason")]
    public void Revoke_AlreadyRevoked_ShouldPreserveFirstRevocation()
    {
        RefreshToken token = CreateToken();
        Result<RefreshToken>.Success(token)
            .Revoke(CreatedAt, RefreshTokenRevocationReason.UserLogout);

        Result<RefreshToken> result = Result<RefreshToken>.Success(token)
            .Revoke(CreatedAt.AddDays(1), RefreshTokenRevocationReason.UserLogoutAll);

        result.IsSuccess.Should().BeTrue();
        token.RevokedAtUtc.Should().Be(CreatedAt);
        token.RevocationReason.Should().Be(RefreshTokenRevocationReason.UserLogout);
    }

    [Fact(DisplayName = "Revoke should reject an undefined reason without mutation")]
    public void Revoke_InvalidReason_ShouldFailWithoutMutation()
    {
        RefreshToken token = CreateToken();

        ShouldFailWith(
            Result<RefreshToken>.Success(token)
                .Revoke(CreatedAt, (RefreshTokenRevocationReason)99),
            "RefreshToken.RevocationReason.Invalid");

        token.IsRevoked.Should().BeFalse();
    }

    [Fact(DisplayName = "Revoke should reject a timestamp before creation without mutation")]
    public void Revoke_BeforeCreation_ShouldFailWithoutMutation()
    {
        RefreshToken token = CreateToken();

        ShouldFailWith(
            Result<RefreshToken>.Success(token)
                .Revoke(CreatedAt.AddDays(-1), RefreshTokenRevocationReason.UserLogout),
            "RefreshToken.RevokedAt.BeforeCreated");

        token.IsRevoked.Should().BeFalse();
    }

    [Fact(DisplayName = "Revoke should allow an expired token")]
    public void Revoke_ExpiredToken_ShouldSucceed()
    {
        RefreshToken token = CreateToken(expiresAt: CreatedAt.AddDays(1));

        Result<RefreshToken> result = Result<RefreshToken>.Success(token)
            .Revoke(CreatedAt.AddDays(2), RefreshTokenRevocationReason.UserLogoutAll);

        result.IsSuccess.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
    }

    [Fact(DisplayName = "DetectReuse should record the theft reason")]
    public void DetectReuse_ShouldRecordTheftReason()
    {
        RefreshToken token = CreateToken();

        Result<RefreshToken> result = Result<RefreshToken>.Success(token)
            .DetectReuse(CreatedAt.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        token.RevocationReason.Should().Be(RefreshTokenRevocationReason.ReuseDetected);
        token.RevokedAtUtc.Should().Be(CreatedAt.AddDays(1));
    }

    [Fact(DisplayName = "DetectReuse should be idempotent")]
    public void DetectReuse_ShouldBeIdempotent()
    {
        RefreshToken token = CreateToken();
        Result<RefreshToken>.Success(token).DetectReuse(CreatedAt.AddDays(1));

        Result<RefreshToken> result = Result<RefreshToken>.Success(token)
            .DetectReuse(CreatedAt.AddDays(2));

        result.IsSuccess.Should().BeTrue();
        token.RevokedAtUtc.Should().Be(CreatedAt.AddDays(1));
    }

    [Fact(DisplayName = "DetectReuse should override an earlier revocation reason")]
    public void DetectReuse_ShouldOverrideEarlierReason()
    {
        RefreshToken token = CreateToken();
        Result<RefreshToken>.Success(token).Revoke(CreatedAt, RefreshTokenRevocationReason.UserLogout);

        Result<RefreshToken> result = Result<RefreshToken>.Success(token)
            .DetectReuse(CreatedAt.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        token.RevocationReason.Should().Be(RefreshTokenRevocationReason.ReuseDetected);
        token.RevokedAtUtc.Should().Be(CreatedAt.AddDays(1));
    }

    [Fact(DisplayName = "DetectReuse should reject a timestamp before creation")]
    public void DetectReuse_BeforeCreation_ShouldFailWithoutMutation()
    {
        RefreshToken token = CreateToken();

        ShouldFailWith(
            Result<RefreshToken>.Success(token).DetectReuse(CreatedAt.AddDays(-1)),
            "RefreshToken.RevokedAt.BeforeCreated");

        token.IsRevoked.Should().BeFalse();
    }

    [Fact(DisplayName = "ReplaceWith should link the replacement and revoke the token")]
    public void ReplaceWith_ShouldRotateToken()
    {
        RefreshToken token = CreateToken();
        RefreshToken replacement = CreateToken(id: Guid.NewGuid());

        Result<RefreshToken> result = Result<RefreshToken>.Success(token)
            .ReplaceWith(replacement, CreatedAt.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        token.ReplacedByToken.Should().BeSameAs(replacement);
        token.RevokedAtUtc.Should().Be(CreatedAt.AddDays(1));
        token.RevocationReason.Should().Be(RefreshTokenRevocationReason.Replaced);
        token.IsActive(CreatedAt.AddDays(2)).Should().BeFalse();
    }

    [Fact(DisplayName = "ReplaceWith should require a replacement")]
    public void ReplaceWith_MissingReplacement_ShouldFail()
    {
        RefreshToken token = CreateToken();

        ShouldFailWith(
            Result<RefreshToken>.Success(token).ReplaceWith(null, CreatedAt),
            "RefreshToken.Replacement.Required");

        token.ReplacedByToken.Should().BeNull();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact(DisplayName = "ReplaceWith should reject the token itself")]
    public void ReplaceWith_Self_ShouldFail()
    {
        RefreshToken token = CreateToken();

        ShouldFailWith(
            Result<RefreshToken>.Success(token).ReplaceWith(token, CreatedAt),
            "RefreshToken.Replacement.SameToken");

        token.ReplacedByToken.Should().BeNull();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact(DisplayName = "ReplaceWith should reject another token family")]
    public void ReplaceWith_FamilyMismatch_ShouldFail()
    {
        RefreshToken token = CreateToken();
        RefreshToken replacement = CreateToken(id: Guid.NewGuid(), familyId: Guid.NewGuid());

        ShouldFailWith(
            Result<RefreshToken>.Success(token).ReplaceWith(replacement, CreatedAt),
            "RefreshToken.Replacement.FamilyMismatch");

        token.ReplacedByToken.Should().BeNull();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact(DisplayName = "ReplaceWith should reject an expired token")]
    public void ReplaceWith_ExpiredToken_ShouldFail()
    {
        RefreshToken token = CreateToken(expiresAt: CreatedAt.AddDays(1));
        RefreshToken replacement = CreateToken(id: Guid.NewGuid());

        ShouldFailWith(
            Result<RefreshToken>.Success(token).ReplaceWith(replacement, CreatedAt.AddDays(1)),
            "RefreshToken.Expired");

        token.ReplacedByToken.Should().BeNull();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact(DisplayName = "ReplaceWith should reject another user")]
    public void ReplaceWith_UserMismatch_ShouldFail()
    {
        RefreshToken token = CreateToken();
        RefreshToken replacement = CreateToken(id: Guid.NewGuid(), userId: "user-2");

        ShouldFailWith(
            Result<RefreshToken>.Success(token).ReplaceWith(replacement, CreatedAt),
            "RefreshToken.Replacement.UserMismatch");

        token.ReplacedByToken.Should().BeNull();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact(DisplayName = "ReplaceWith should reject an inactive replacement")]
    public void ReplaceWith_InactiveReplacement_ShouldFail()
    {
        RefreshToken token = CreateToken();
        RefreshToken replacement = CreateToken(
            id: Guid.NewGuid(),
            expiresAt: CreatedAt.AddDays(1));

        ShouldFailWith(
            Result<RefreshToken>.Success(token).ReplaceWith(replacement, CreatedAt.AddDays(1)),
            "RefreshToken.Replacement.NotActive");

        token.ReplacedByToken.Should().BeNull();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact(DisplayName = "ReplaceWith should reject a revoked token")]
    public void ReplaceWith_RevokedToken_ShouldFail()
    {
        RefreshToken token = CreateToken();
        Result<RefreshToken>.Success(token).Revoke(CreatedAt, RefreshTokenRevocationReason.UserLogout);
        RefreshToken replacement = CreateToken(id: Guid.NewGuid());

        ShouldFailWith(
            Result<RefreshToken>.Success(token).ReplaceWith(replacement, CreatedAt.AddDays(1)),
            "RefreshToken.Revoked");

        token.ReplacedByToken.Should().BeNull();
    }

    [Fact(DisplayName = "Lifecycle helpers should short-circuit an existing failure")]
    public void LifecycleHelpers_ShouldShortCircuitFailure()
    {
        Result<RefreshToken> failed = Result<RefreshToken>.Fail(RefreshTokenOutcome.Failure.NotFound);

        Result<RefreshToken> revoked = failed.Revoke(CreatedAt, RefreshTokenRevocationReason.UserLogout);
        Result<RefreshToken> replaced = failed.ReplaceWith(null, CreatedAt);

        ShouldFailWith(revoked, "RefreshToken.NotFound");
        ShouldFailWith(replaced, "RefreshToken.NotFound");
    }
}
