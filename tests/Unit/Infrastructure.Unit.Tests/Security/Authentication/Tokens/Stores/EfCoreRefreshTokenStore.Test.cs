using Application.Common.Security.Authentication.Tokens.Domain;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;
using Infrastructure.Security.Authentication.Tokens.Stores;

using Core.Common.Monad.Maybes;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Security.Authentication.Tokens.Stores;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "RefreshTokens")]
public sealed class EfCoreRefreshTokenStoreTest
{
    private static readonly DateTimeOffset Now = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "EfCoreRefreshTokenStore should add, find, and revoke a token")]
    public async Task Store_ShouldAddFindAndRevoke()
    {
        await using ApplicationDbContext db = CreateContext();
        var store = new EfCoreRefreshTokenStore(db, new EfCoreUnitOfWork(db));
        RefreshToken token = CreateToken();

        (await store.AddAsync(token)).IsSuccess.Should().BeTrue();
        Result<Maybe<RefreshToken>> found = await store.FindByHashAsync(token.TokenHash);

        found.IsSuccess.Should().BeTrue();
        found.Value.HasValue.Should().BeTrue();
        found.Value.Value.Id.Should().Be(token.Id);

        Result<RefreshToken> result = Result<RefreshToken>.Success(found.Value.Value)
            .Revoke(Now.AddMinutes(1), RefreshTokenRevocationReason.UserLogout);
        result.IsSuccess.Should().BeTrue();
        (await store.UpdateAsync(found.Value.Value)).IsSuccess.Should().BeTrue();

        RefreshToken persisted = await db.Set<RefreshToken>().SingleAsync();
        persisted.IsRevoked.Should().BeTrue();
        persisted.RevocationReason.Should().Be(RefreshTokenRevocationReason.UserLogout);
    }

    [Fact(DisplayName = "EfCoreRefreshTokenStore should return an empty Maybe for an unknown hash")]
    public async Task Store_ShouldReturnEmptyForUnknownHash()
    {
        await using ApplicationDbContext db = CreateContext();
        var store = new EfCoreRefreshTokenStore(db, new EfCoreUnitOfWork(db));

        Result<Maybe<RefreshToken>> found = await store.FindByHashAsync("missing");

        found.IsSuccess.Should().BeTrue();
        found.Value.HasNoValue.Should().BeTrue();
    }

    [Fact(DisplayName = "EfCoreRefreshTokenStore should rotate and revoke all user tokens")]
    public async Task Store_ShouldRotateAndRevokeAll()
    {
        await using ApplicationDbContext db = CreateContext();
        var store = new EfCoreRefreshTokenStore(db, new EfCoreUnitOfWork(db));
        RefreshToken current = CreateToken();
        RefreshToken replacement = CreateToken(current.TokenFamilyId);
        await store.AddAsync(current);

        Result<RefreshToken> replaced = Result<RefreshToken>.Success(current)
            .ReplaceWith(replacement, Now.AddMinutes(1));
        replaced.IsSuccess.Should().BeTrue();
        (await store.RotateAsync(current, replacement)).IsSuccess.Should().BeTrue();

        db.Set<RefreshToken>().Should().HaveCount(2);
        (await store.RevokeAllAsync("user-1", Now.AddMinutes(2), RefreshTokenRevocationReason.UserLogoutAll)).Value.Should().Be(1);

        RefreshToken active = CreateToken();
        await store.AddAsync(active);
        Result<int> revoked = await store.RevokeAllAsync(
            "user-1",
            Now.AddMinutes(3),
            RefreshTokenRevocationReason.ReuseDetected);

        revoked.IsSuccess.Should().BeTrue();
        revoked.Value.Should().Be(1);
        (await db.Set<RefreshToken>().SingleAsync(token => token.Id == active.Id))
            .RevocationReason.Should().Be(RefreshTokenRevocationReason.ReuseDetected);
    }

    private static RefreshToken CreateToken(Guid? familyId = null) =>
        RefreshToken.Create(
            Guid.NewGuid(),
            Guid.NewGuid().ToString("N"),
            "user-1",
            familyId ?? Guid.NewGuid(),
            Now,
            Now.AddDays(7)).Value;

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"RefreshStore_{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
