using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Services;
using Application.Common.Security.Authentication.Tokens.Domain;

namespace Application.Unit.Tests.Common.Security.Authentication.Tokens;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class TokenRotationServiceTest
{
    private static readonly DateTimeOffset Now = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const string PlainToken = "rt_current";

    private static RefreshToken CreateToken(
        Guid? familyId = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? expiresAt = null)
    {
        Result<RefreshToken> created = RefreshToken.Create(
            Guid.NewGuid(),
            Guid.NewGuid().ToString("N"),
            "user-1",
            familyId ?? Guid.NewGuid(),
            createdAt ?? Now,
            expiresAt ?? Now.AddDays(7));

        created.IsSuccess.Should().BeTrue();
        return created.Value;
    }

    private static TokenRotationService CreateService(Mock<IRefreshTokenService> refresh) =>
        new(refresh.Object);

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "RotateAsync should rotate an active token")]
    public async Task RotateAsync_ShouldRotateActiveToken()
    {
        RefreshToken current = CreateToken();
        RefreshToken replacement = CreateToken(familyId: current.TokenFamilyId);
        var issue = new RefreshTokenIssue("rt_replacement", replacement);
        var refresh = new Mock<IRefreshTokenService>();
        refresh
            .Setup(service => service.FindAsync(PlainToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshToken>.Ok(current));
        refresh
            .Setup(service => service.RotateAsync(current, Now.AddMinutes(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshTokenIssue>.Ok(issue));

        Result<RefreshTokenIssue> result = await CreateService(refresh)
            .RotateAsync(PlainToken, Now.AddMinutes(1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(issue);
        refresh.Verify(
            service => service.RotateAsync(current, Now.AddMinutes(1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "RotateAsync should return the lookup failure")]
    public async Task RotateAsync_ShouldReturnLookupFailure()
    {
        var refresh = new Mock<IRefreshTokenService>();
        refresh
            .Setup(service => service.FindAsync(PlainToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshToken>.Fail(RefreshTokenOutcome.Failure.NotFound));

        Result<RefreshTokenIssue> result = await CreateService(refresh)
            .RotateAsync(PlainToken, Now);

        ShouldFailWith(result, "RefreshToken.NotFound");
        refresh.Verify(
            service => service.RotateAsync(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "RotateAsync should mark reuse of a replaced token as theft")]
    public async Task RotateAsync_ShouldDetectReuse()
    {
        RefreshToken current = CreateToken();
        RefreshToken replacement = CreateToken(familyId: current.TokenFamilyId);
        Result<RefreshToken>.Success(current).ReplaceWith(replacement, Now.AddMinutes(1));
        var refresh = new Mock<IRefreshTokenService>();
        refresh
            .Setup(service => service.FindAsync(PlainToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshToken>.Ok(current));
        refresh
            .Setup(service => service.MarkReuseAsync(current, Now.AddMinutes(2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        refresh
            .Setup(service => service.RevokeAllAsync(
                "user-1",
                Now.AddMinutes(2),
                RefreshTokenRevocationReason.ReuseDetected,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(1));

        Result<RefreshTokenIssue> result = await CreateService(refresh)
            .RotateAsync(PlainToken, Now.AddMinutes(2));

        ShouldFailWith(result, "RefreshToken.TheftDetected");
        refresh.Verify(
            service => service.MarkReuseAsync(current, Now.AddMinutes(2), It.IsAny<CancellationToken>()),
            Times.Once);
        refresh.Verify(
            service => service.RevokeAllAsync(
                "user-1",
                Now.AddMinutes(2),
                RefreshTokenRevocationReason.ReuseDetected,
                It.IsAny<CancellationToken>()),
            Times.Once);
        refresh.Verify(
            service => service.RotateAsync(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "RotateAsync should return a family revocation failure")]
    public async Task RotateAsync_ShouldReturnFamilyRevocationFailure()
    {
        RefreshToken current = CreateToken();
        RefreshToken replacement = CreateToken(familyId: current.TokenFamilyId);
        Result<RefreshToken>.Success(current).ReplaceWith(replacement, Now.AddMinutes(1));
        var error = Error.Unexpected("Test.RevokeAll", "Revoke all failed.");
        var refresh = new Mock<IRefreshTokenService>();
        refresh
            .Setup(service => service.FindAsync(PlainToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshToken>.Ok(current));
        refresh
            .Setup(service => service.MarkReuseAsync(current, Now.AddMinutes(2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        refresh
            .Setup(service => service.RevokeAllAsync(
                "user-1",
                Now.AddMinutes(2),
                RefreshTokenRevocationReason.ReuseDetected,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Fail(error));

        Result<RefreshTokenIssue> result = await CreateService(refresh)
            .RotateAsync(PlainToken, Now.AddMinutes(2));

        ShouldFailWith(result, "Test.RevokeAll");
    }

    [Fact(DisplayName = "RotateAsync should return a reuse persistence failure")]
    public async Task RotateAsync_ShouldReturnReuseFailure()
    {
        RefreshToken current = CreateToken();
        RefreshToken replacement = CreateToken(familyId: current.TokenFamilyId);
        Result<RefreshToken>.Success(current).ReplaceWith(replacement, Now.AddMinutes(1));
        var error = Error.Unexpected("Test.MarkReuse", "Mark reuse failed.");
        var refresh = new Mock<IRefreshTokenService>();
        refresh
            .Setup(service => service.FindAsync(PlainToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshToken>.Ok(current));
        refresh
            .Setup(service => service.MarkReuseAsync(current, Now.AddMinutes(2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(error));

        Result<RefreshTokenIssue> result = await CreateService(refresh)
            .RotateAsync(PlainToken, Now.AddMinutes(2));

        ShouldFailWith(result, "Test.MarkReuse");
    }

    [Fact(DisplayName = "RotateAsync should reject a revoked token that was not replaced")]
    public async Task RotateAsync_ShouldRejectRevokedToken()
    {
        RefreshToken current = CreateToken();
        Result<RefreshToken>.Success(current).Revoke(Now, RefreshTokenRevocationReason.UserLogout);
        var refresh = new Mock<IRefreshTokenService>();
        refresh
            .Setup(service => service.FindAsync(PlainToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshToken>.Ok(current));

        Result<RefreshTokenIssue> result = await CreateService(refresh)
            .RotateAsync(PlainToken, Now.AddMinutes(1));

        ShouldFailWith(result, "RefreshToken.Revoked");
        refresh.Verify(
            service => service.MarkReuseAsync(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "RotateAsync should reject an expired token")]
    public async Task RotateAsync_ShouldRejectExpiredToken()
    {
        RefreshToken current = CreateToken(expiresAt: Now.AddDays(1));
        var refresh = new Mock<IRefreshTokenService>();
        refresh
            .Setup(service => service.FindAsync(PlainToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshToken>.Ok(current));

        Result<RefreshTokenIssue> result = await CreateService(refresh)
            .RotateAsync(PlainToken, Now.AddDays(2));

        ShouldFailWith(result, "RefreshToken.Expired");
    }

    [Fact(DisplayName = "RotateAsync should return a rotation failure")]
    public async Task RotateAsync_ShouldReturnRotationFailure()
    {
        RefreshToken current = CreateToken();
        var refresh = new Mock<IRefreshTokenService>();
        refresh
            .Setup(service => service.FindAsync(PlainToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshToken>.Ok(current));
        refresh
            .Setup(service => service.RotateAsync(current, Now.AddMinutes(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshTokenIssue>.Fail(TokenOutcome.Failure.RotationFailed));

        Result<RefreshTokenIssue> result = await CreateService(refresh)
            .RotateAsync(PlainToken, Now.AddMinutes(1));

        ShouldFailWith(result, "Token.Rotation.Failed");
    }

    [Fact(DisplayName = "The constructor should reject a null refresh-token service")]
    public void Constructor_ShouldRejectNullService()
    {
        Assert.Throws<ArgumentNullException>(() => new TokenRotationService(null!));
    }
}
